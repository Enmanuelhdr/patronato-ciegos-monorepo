using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Patronato.Caja.Models;

namespace Patronato.Caja.Services
{
    // La Caja nunca habla HTTP con nadie: toda operación (consulta o cobro) viaja por el
    // socket del Gateway como un código de transacción, igual que un cajero automático
    // habla con su switch por una sola línea en vez de abrir una API por cada botón.
    //
    // Resiliencia: si el Gateway no responde, los cobros no se pierden -> se registran
    // localmente con un recibo PROVISIONAL y quedan en OfflineQueueService para reenviarse
    // solos en cuanto vuelva la conexión (ver SincronizarPendientesAsync). Las consultas, si
    // no hay conexión, devuelven el último dato bueno que se tenga en memoria.
    public class ApiService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly GatewaySocketClient _gateway = new();

        // Último dato bueno de cada consulta, para seguir mostrando algo útil sin conexión.
        private static List<ProductoItem>? _cacheProductos;
        private static List<SesionMasajeItem>? _cacheMasajes;
        private static List<PrestamoItem>? _cachePrestamos;
        private static List<ClienteFrecuenteDto>? _cacheClientesFrecuentes;
        private static List<OrdenPedidoCajaDto>? _cacheOrdenes;

        // Unidades vendidas sin conexión que el catálogo en caché todavía no refleja (el Core
        // es quien de verdad descuenta stock; esto es solo para no "vender de más" en pantalla
        // mientras la venta sigue encolada). Se limpia en cuanto vuelve a llegar un catálogo real.
        private static readonly Dictionary<int, int> _ajustesStockOffline = new();

        // True mientras la última operación con el Gateway falló por falta de conexión.
        // MainWindow lo consulta para avisar en la barra de estado.
        public static bool SinConexionGateway { get; private set; }

        // Traduce una TransaccionResponse del Gateway al contrato (Success, Message, Recibo)
        // que ya usan las vistas. "01" (encolada por el propio Gateway porque el Core está
        // caído) se trata como éxito: la operación quedó aceptada, solo pendiente de confirmar.
        private static (bool Success, string Message, TransaccionCajaDto? Recibo) TraducirRespuestaGateway(TransaccionResponse respuesta)
        {
            bool success = respuesta.CodigoRespuesta is "00" or "01";
            TransaccionCajaDto? recibo = null;

            if (respuesta.Datos is JsonElement datosEl && datosEl.ValueKind == JsonValueKind.Object &&
                datosEl.TryGetProperty("recibo", out var reciboEl))
            {
                recibo = JsonSerializer.Deserialize<TransaccionCajaDto>(reciboEl.GetRawText(), _jsonOptions);
            }

            return (success, respuesta.Mensaje, recibo);
        }

        // Toda consulta de catálogo/listado viene como un array en Datos.
        private static List<T> DeserializarLista<T>(TransaccionResponse respuesta)
        {
            if (respuesta.CodigoRespuesta == "00" && respuesta.Datos is JsonElement datosEl && datosEl.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<T>>(datosEl.GetRawText(), _jsonOptions) ?? new List<T>();
            }
            return new List<T>();
        }

        // ==========================================
        // TEST DE CONEXIÓN (línea viva con el Gateway, sin tocar el Core)
        // ==========================================
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var respuesta = await _gateway.EnviarAsync("PING", new { });
                SinConexionGateway = false;
                return respuesta.CodigoRespuesta == "00";
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;
                return false;
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // AUTENTICACIÓN (no se puede encolar: hace falta validar contra el Core sí o sí)
        // ==========================================
        public async Task<(bool Success, string Message, UsuarioSession? Session)> LoginAsync(string username, string password)
        {
            try
            {
                var respuesta = await _gateway.EnviarAsync("LOGIN", new { nombreUsuario = username, password });
                SinConexionGateway = false;

                if (respuesta.CodigoRespuesta != "00")
                {
                    return (false, string.IsNullOrWhiteSpace(respuesta.Mensaje) ? "Credenciales inválidas." : respuesta.Mensaje, null);
                }

                if (respuesta.Datos is not JsonElement datosEl || datosEl.ValueKind != JsonValueKind.Object)
                {
                    return (false, "Error al procesar la respuesta del Gateway.", null);
                }

                var data = JsonSerializer.Deserialize<LoginResponse>(datosEl.GetRawText(), _jsonOptions);
                if (data == null)
                    return (false, "Error al procesar la respuesta del Gateway.", null);

                // Esta terminal es exclusiva de cobro: solo Cajero o Administrador la usan.
                // Agendar citas, otorgar préstamos y gestionar empleados son tareas de Recepción/Web.
                if (data.Rol != "Cajero" && data.Rol != "Administrador")
                {
                    return (false, $"El usuario tiene el rol '{data.Rol}'. Solo 'Cajero' o 'Administrador' pueden acceder a esta terminal de Caja.", null);
                }

                string sucursalNombre = await ObtenerNombreSucursalAsync(data.SucursalId);

                var session = new UsuarioSession
                {
                    UsuarioId = data.UsuarioId,
                    Nombre = data.Nombre,
                    Rol = data.Rol,
                    SucursalId = data.SucursalId,
                    SucursalNombre = sucursalNombre,
                    HoraInicioTurno = DateTime.Now
                };

                return (true, "Inicio de sesión exitoso.", session);
            }
            catch (GatewayNoDisponibleException ex)
            {
                SinConexionGateway = true;
                return (false, $"No se pudo conectar con el Gateway: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"No se pudo conectar con el Gateway: {ex.Message}", null);
            }
        }

        // El nombre de la sucursal viene del Core (SUCURSALES_CONSULTAR), nunca hardcodeado en la Caja.
        private async Task<string> ObtenerNombreSucursalAsync(int sucursalId)
        {
            try
            {
                var respuesta = await _gateway.EnviarAsync("SUCURSALES_CONSULTAR", new { });
                var sucursales = DeserializarLista<SucursalDto>(respuesta);
                var match = sucursales.FirstOrDefault(s => s.Id == sucursalId);
                return match?.Nombre ?? "Sede Institucional";
            }
            catch
            {
                return "Sede Institucional";
            }
        }

        // ==========================================
        // 1. VENTA DE PRODUCTOS (INVENTARIO Y POS)
        // ==========================================
        public async Task<List<ProductoItem>> GetProductosInventarioAsync()
        {
            try
            {
                var respuesta = await _gateway.EnviarAsync("INVENTARIO_CONSULTAR", new { });
                var lista = DeserializarLista<ProductoItem>(respuesta);
                _cacheProductos = lista;
                _ajustesStockOffline.Clear(); // el Core vuelve a ser la fuente de verdad del stock
                SinConexionGateway = false;
                return lista;
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;
                return AplicarAjustesStockOffline(_cacheProductos);
            }
            catch
            {
                return AplicarAjustesStockOffline(_cacheProductos);
            }
        }

        private static List<ProductoItem> AplicarAjustesStockOffline(List<ProductoItem>? catalogoBase)
        {
            if (catalogoBase == null) return new List<ProductoItem>();
            if (_ajustesStockOffline.Count == 0) return new List<ProductoItem>(catalogoBase);

            return catalogoBase.Select(p => new ProductoItem
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                Categoria = p.Categoria,
                PrecioVenta = p.PrecioVenta,
                StockDisponible = _ajustesStockOffline.TryGetValue(p.Id, out var vendido)
                    ? Math.Max(0, p.StockDisponible - vendido)
                    : p.StockDisponible
            }).ToList();
        }

        public async Task<(bool Success, string Message, TransaccionCajaDto? Recibo)> VenderCarritoAsync(
            List<CarritoItem> items, string metodoPago, decimal montoRecibido, string cliente, int? ordenId = null)
        {
            var cajero = SessionManager.CurrentSession?.Nombre ?? "Caja Principal";
            var sucursal = SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central";
            var clienteFinal = string.IsNullOrWhiteSpace(cliente) ? "Cliente de Contado" : cliente;

            var datos = new
            {
                items = items.Select(it => new { productoId = it.ProductoId, cantidad = it.Cantidad }).ToList(),
                metodoPago,
                montoRecibido,
                cajero,
                sucursal,
                cliente = clienteFinal,
                ordenId
            };

            try
            {
                // Cobro de dinero real: código VENTA_CARRITO por el socket del Gateway.
                var respuesta = await _gateway.EnviarAsync("VENTA_CARRITO", datos);
                SinConexionGateway = false;
                return TraducirRespuestaGateway(respuesta);
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;

                decimal total = items.Sum(i => i.Subtotal);
                bool esEfectivo = string.Equals(metodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
                decimal recibido = esEfectivo && montoRecibido > 0 ? montoRecibido : total;
                decimal devuelta = esEfectivo ? Math.Max(0, recibido - total) : 0;

                var recibo = new TransaccionCajaDto
                {
                    NumeroRecibo = OfflineQueueService.GenerarNumeroProvisional("POS"),
                    FechaHora = DateTime.Now,
                    TipoTransaccion = "VentaProductos",
                    Concepto = $"Venta POS sin conexión ({items.Sum(i => i.Cantidad)} artículo(s)) - PENDIENTE DE SINCRONIZAR",
                    MontoTotal = total,
                    MetodoPago = metodoPago,
                    MontoRecibido = recibido,
                    Devuelta = devuelta,
                    Cajero = cajero,
                    Sucursal = sucursal,
                    ClienteOBeneficiario = clienteFinal,
                    DetalleLineas = string.Join("\n", items.Select(i => $"{i.Cantidad}x {i.Nombre} (RD$ {i.PrecioUnitario:N2}) = RD$ {i.Subtotal:N2}"))
                };

                foreach (var it in items)
                    _ajustesStockOffline[it.ProductoId] = (_ajustesStockOffline.TryGetValue(it.ProductoId, out var v) ? v : 0) + it.Cantidad;

                OfflineQueueService.Encolar("VENTA_CARRITO", datos, recibo.NumeroRecibo);

                return (true, "Sin conexión con el Gateway: venta registrada localmente. Se sincronizará automáticamente.", recibo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al comunicar con el Gateway: {ex.Message}", null);
            }
        }

        // ==========================================
        // 2. MASAJES TACTO
        // ==========================================
        public async Task<List<SesionMasajeItem>> GetMasajesPendientesAsync()
        {
            try
            {
                var respuesta = await _gateway.EnviarAsync("MASAJES_PENDIENTES_CONSULTAR", new { });
                _cacheMasajes = DeserializarLista<SesionMasajeItem>(respuesta);
                SinConexionGateway = false;
                return _cacheMasajes;
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;
                return _cacheMasajes ?? new List<SesionMasajeItem>();
            }
            catch
            {
                return _cacheMasajes ?? new List<SesionMasajeItem>();
            }
        }

        public async Task<(bool Success, string Message, TransaccionCajaDto? Recibo)> CobrarMasajeAsync(
            SesionMasajeItem sesion, string metodoPago, decimal montoRecibido)
        {
            var cajero = SessionManager.CurrentSession?.Nombre ?? "Caja Principal";
            var sucursal = SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central";

            var datos = new
            {
                id = sesion.Id,
                metodoPago,
                montoRecibido,
                cajero,
                sucursal
            };

            try
            {
                // Código MASAJE_COBRAR: cobro de dinero real, debe ir por el Gateway.
                var respuesta = await _gateway.EnviarAsync("MASAJE_COBRAR", datos);
                SinConexionGateway = false;
                return TraducirRespuestaGateway(respuesta);
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;

                bool esEfectivo = string.Equals(metodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
                decimal recibido = esEfectivo && montoRecibido > 0 ? montoRecibido : sesion.PrecioTarifa;
                decimal devuelta = esEfectivo ? Math.Max(0, recibido - sesion.PrecioTarifa) : 0;

                var recibo = new TransaccionCajaDto
                {
                    NumeroRecibo = OfflineQueueService.GenerarNumeroProvisional("MAS"),
                    FechaHora = DateTime.Now,
                    TipoTransaccion = "MasajeTacto",
                    Concepto = $"Masaje TACTO ({sesion.DuracionMinutos} min) - PENDIENTE DE SINCRONIZAR",
                    MontoTotal = sesion.PrecioTarifa,
                    MetodoPago = metodoPago,
                    MontoRecibido = recibido,
                    Devuelta = devuelta,
                    Cajero = cajero,
                    Sucursal = sucursal,
                    ClienteOBeneficiario = sesion.NombreCliente,
                    IdentificacionCliente = sesion.TelefonoCliente,
                    DetalleLineas = $"1x Sesión de masaje terapéutico TACTO ({sesion.DuracionMinutos} mins) - RD$ {sesion.PrecioTarifa:N2}",
                    ReferenciaId = sesion.Id
                };

                OfflineQueueService.Encolar("MASAJE_COBRAR", datos, recibo.NumeroRecibo);

                return (true, "Sin conexión con el Gateway: masaje cobrado localmente. Se sincronizará automáticamente.", recibo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al comunicar con el Gateway: {ex.Message}", null);
            }
        }

        // ==========================================
        // 3. PRÉSTAMOS DE EMPRENDIMIENTO
        // ==========================================
        public async Task<List<PrestamoItem>> GetPrestamosActivosAsync(string? filtro = null)
        {
            try
            {
                var respuesta = await _gateway.EnviarAsync("PRESTAMOS_ACTIVOS_CONSULTAR", new { filtro = filtro ?? "" });
                _cachePrestamos = DeserializarLista<PrestamoItem>(respuesta);
                SinConexionGateway = false;
                return _cachePrestamos;
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;
                return FiltrarCachePrestamos(filtro);
            }
            catch
            {
                return FiltrarCachePrestamos(filtro);
            }
        }

        private static List<PrestamoItem> FiltrarCachePrestamos(string? filtro)
        {
            if (_cachePrestamos == null) return new List<PrestamoItem>();
            if (string.IsNullOrWhiteSpace(filtro)) return new List<PrestamoItem>(_cachePrestamos);

            var f = filtro.Trim().ToLower();
            return _cachePrestamos.Where(p =>
                (p.Beneficiario != null && (
                    p.Beneficiario.Nombres.ToLower().Contains(f) ||
                    p.Beneficiario.Apellidos.ToLower().Contains(f) ||
                    p.Beneficiario.Cedula.Contains(f))) ||
                p.Id.ToString() == f).ToList();
        }

        public async Task<(bool Success, string Message, decimal NuevoSaldo, TransaccionCajaDto? Recibo)> AbonarPrestamoAsync(
            PrestamoItem prestamo, decimal monto, string metodoPago, decimal montoRecibido)
        {
            var cajero = SessionManager.CurrentSession?.Nombre ?? "Caja Principal";
            var sucursal = SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central";

            var datos = new
            {
                prestamoId = prestamo.Id,
                monto,
                metodoPago,
                montoRecibido,
                cajero,
                sucursal
            };

            try
            {
                // Código PRESTAMO_ABONAR: cobro de dinero real, debe ir por el Gateway.
                var respuesta = await _gateway.EnviarAsync("PRESTAMO_ABONAR", datos);
                SinConexionGateway = false;

                bool success = respuesta.CodigoRespuesta is "00" or "01";
                decimal nuevoSaldo = 0;
                TransaccionCajaDto? recibo = null;

                if (respuesta.Datos is JsonElement datosEl && datosEl.ValueKind == JsonValueKind.Object)
                {
                    if (datosEl.TryGetProperty("nuevoSaldo", out var saldoEl))
                        nuevoSaldo = saldoEl.GetDecimal();
                    if (datosEl.TryGetProperty("recibo", out var reciboEl))
                        recibo = JsonSerializer.Deserialize<TransaccionCajaDto>(reciboEl.GetRawText(), _jsonOptions);
                }

                return (success, respuesta.Mensaje, nuevoSaldo, recibo);
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;

                bool esEfectivo = string.Equals(metodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
                decimal recibido = esEfectivo && montoRecibido > 0 ? montoRecibido : monto;
                decimal devuelta = esEfectivo ? Math.Max(0, recibido - monto) : 0;
                decimal nuevoSaldoEstimado = Math.Max(0, prestamo.SaldoPendiente - monto);
                string nombreBeneficiario = prestamo.Beneficiario?.NombreCompleto ?? "Beneficiario No. " + prestamo.BeneficiarioId;

                var recibo = new TransaccionCajaDto
                {
                    NumeroRecibo = OfflineQueueService.GenerarNumeroProvisional("ABN"),
                    FechaHora = DateTime.Now,
                    TipoTransaccion = "AbonoPrestamo",
                    Concepto = $"Abono a Préstamo #{prestamo.Id} - {nombreBeneficiario} - PENDIENTE DE SINCRONIZAR",
                    MontoTotal = monto,
                    MetodoPago = metodoPago,
                    MontoRecibido = recibido,
                    Devuelta = devuelta,
                    Cajero = cajero,
                    Sucursal = sucursal,
                    ClienteOBeneficiario = nombreBeneficiario,
                    IdentificacionCliente = prestamo.Beneficiario?.Cedula ?? string.Empty,
                    DetalleLineas = $"Abono a capital (estimado sin conexión). Saldo anterior: RD$ {prestamo.SaldoPendiente:N2} | Saldo restante estimado: RD$ {nuevoSaldoEstimado:N2}",
                    ReferenciaId = prestamo.Id
                };

                OfflineQueueService.Encolar("PRESTAMO_ABONAR", datos, recibo.NumeroRecibo);

                return (true, "Sin conexión con el Gateway: abono registrado localmente. Se sincronizará automáticamente.", nuevoSaldoEstimado, recibo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al comunicar con el Gateway: {ex.Message}", 0, null);
            }
        }

        // ==========================================
        // 4. DONACIONES EN VENTANILLA
        // ==========================================
        public async Task<(bool Success, string Message, TransaccionCajaDto? Recibo)> RegistrarDonacionAsync(DonacionRequest donacion)
        {
            var cajero = SessionManager.CurrentSession?.Nombre ?? "Caja Principal";
            var sucursal = SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central";

            var datos = new
            {
                donante = donacion.Donante,
                rncCedula = donacion.RncCedula,
                monto = donacion.Monto,
                metodoPago = donacion.MetodoPago,
                requiereComprobanteFiscal = donacion.RequiereComprobanteFiscal,
                cajero,
                sucursal
            };

            try
            {
                // Código DONACION_REGISTRAR: cobro de dinero real, debe ir por el Gateway.
                var respuesta = await _gateway.EnviarAsync("DONACION_REGISTRAR", datos);
                SinConexionGateway = false;
                return TraducirRespuestaGateway(respuesta);
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;

                var recibo = new TransaccionCajaDto
                {
                    NumeroRecibo = OfflineQueueService.GenerarNumeroProvisional("DON"),
                    FechaHora = DateTime.Now,
                    TipoTransaccion = "Donacion",
                    Concepto = $"Donación Institucional - {donacion.Donante} - PENDIENTE DE SINCRONIZAR",
                    MontoTotal = donacion.Monto,
                    MetodoPago = donacion.MetodoPago,
                    MontoRecibido = donacion.Monto,
                    Devuelta = 0,
                    Cajero = cajero,
                    Sucursal = sucursal,
                    ClienteOBeneficiario = donacion.Donante,
                    IdentificacionCliente = donacion.RncCedula,
                    DetalleLineas = $"Donación recibida sin conexión. Requiere NCF: {(donacion.RequiereComprobanteFiscal ? "SÍ" : "NO")}"
                };

                OfflineQueueService.Encolar("DONACION_REGISTRAR", datos, recibo.NumeroRecibo);

                return (true, "Sin conexión con el Gateway: donación registrada localmente. Se sincronizará automáticamente.", recibo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al comunicar con el Gateway: {ex.Message}", null);
            }
        }

        // ==========================================
        // 5. CUADRE DE CAJA Y TRANSACCIONES (no tiene sentido cachear: es un corte del día)
        // ==========================================
        public async Task<CuadreCajaDto?> GetCuadreCajaAsync(DateTime? fecha = null, string? cajero = null)
        {
            try
            {
                var datos = new { fecha = (fecha ?? DateTime.Today).ToString("yyyy-MM-dd"), cajero = cajero ?? "" };
                var respuesta = await _gateway.EnviarAsync("CUADRE_CAJA_CONSULTAR", datos);
                SinConexionGateway = false;

                if (respuesta.CodigoRespuesta == "00" && respuesta.Datos is JsonElement datosEl && datosEl.ValueKind == JsonValueKind.Object)
                {
                    return JsonSerializer.Deserialize<CuadreCajaDto>(datosEl.GetRawText(), _jsonOptions);
                }
                return null;
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<TransaccionCajaDto>> GetTransaccionesAsync(DateTime? fecha = null, string? cajero = null)
        {
            try
            {
                var datos = new { fecha = (fecha ?? DateTime.Today).ToString("yyyy-MM-dd"), cajero = cajero ?? "" };
                var respuesta = await _gateway.EnviarAsync("TRANSACCIONES_CONSULTAR", datos);
                SinConexionGateway = false;
                return DeserializarLista<TransaccionCajaDto>(respuesta);
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;
                return new List<TransaccionCajaDto>();
            }
            catch
            {
                return new List<TransaccionCajaDto>();
            }
        }

        // ==========================================
        // 6. CLIENTES FRECUENTES (AUTOCOMPLETADO POS Y DONACIONES)
        // ==========================================
        public async Task<List<ClienteFrecuenteDto>> GetClientesFrecuentesAsync()
        {
            try
            {
                var respuesta = await _gateway.EnviarAsync("CLIENTES_FRECUENTES_CONSULTAR", new { });
                _cacheClientesFrecuentes = DeserializarLista<ClienteFrecuenteDto>(respuesta);
                SinConexionGateway = false;
                return _cacheClientesFrecuentes;
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;
                return _cacheClientesFrecuentes ?? new List<ClienteFrecuenteDto>();
            }
            catch
            {
                return _cacheClientesFrecuentes ?? new List<ClienteFrecuenteDto>();
            }
        }

        // ==========================================
        // 7. ÓRDENES DE PEDIDO PENDIENTES DE COBRO
        // ==========================================
        // Las órdenes se generan fuera de esta terminal (Recepción/Web); la Caja solo
        // las carga al carrito para cobrarlas (ver BtnCargarOrden_Click en MainWindow).
        public async Task<List<OrdenPedidoCajaDto>> GetOrdenesPendientesAsync()
        {
            try
            {
                var respuesta = await _gateway.EnviarAsync("ORDENES_PENDIENTES_CONSULTAR", new { });
                _cacheOrdenes = DeserializarLista<OrdenPedidoCajaDto>(respuesta);
                SinConexionGateway = false;
                return _cacheOrdenes;
            }
            catch (GatewayNoDisponibleException)
            {
                SinConexionGateway = true;
                return _cacheOrdenes ?? new List<OrdenPedidoCajaDto>();
            }
            catch
            {
                return _cacheOrdenes ?? new List<OrdenPedidoCajaDto>();
            }
        }

        // ==========================================
        // 8. SINCRONIZACIÓN DE PENDIENTES (Caja -> Gateway)
        // ==========================================
        // Reenvía en orden todo lo que quedó encolado mientras no había conexión con el
        // Gateway. Si el Gateway responde "00" (el Core ya la confirmó) o "01" (el propio
        // Gateway la encoló porque el Core seguía caído), la Caja ya cumplió su parte y la
        // quita de su cola; si sigue sin poder conectar, se detiene y se reintenta en el
        // próximo ciclo (ver MainWindow -> temporizador de sincronización).
        public async Task<int> SincronizarPendientesAsync()
        {
            int exitosas = 0;

            while (true)
            {
                var pendientes = OfflineQueueService.ObtenerTodos();
                if (pendientes.Count == 0) break;

                var item = pendientes[0];
                try
                {
                    var respuesta = await _gateway.EnviarAsync(item.Codigo, item.Datos);
                    SinConexionGateway = false;
                    OfflineQueueService.QuitarPrimero();
                    exitosas++;
                }
                catch (GatewayNoDisponibleException)
                {
                    SinConexionGateway = true;
                    break;
                }
            }

            return exitosas;
        }
    }
}
