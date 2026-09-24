using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Patronato.Caja.Models;

namespace Patronato.Caja.Services
{
    public class ApiService
    {
        private static readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions;

        static ApiService()
        {
            var handler = new HttpClientHandler
            {
                // Permite certificados autofirmados de desarrollo local
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private string GetBaseUrl() => SessionManager.BaseApiUrl.TrimEnd('/');

        // ==========================================
        // TEST DE CONEXIÓN
        // ==========================================
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{GetBaseUrl()}/api/inventario");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // Probar alternativa HTTPS si la HTTP falla
                if (GetBaseUrl().StartsWith("http://localhost:57141"))
                {
                    try
                    {
                        var altResponse = await _httpClient.GetAsync("https://localhost:57140/api/inventario");
                        if (altResponse.IsSuccessStatusCode)
                        {
                            SessionManager.BaseApiUrl = "https://localhost:57140";
                            return true;
                        }
                    }
                    catch { }
                }
                return false;
            }
        }

        // ==========================================
        // AUTENTICACIÓN
        // ==========================================
        public async Task<(bool Success, string Message, UsuarioSession? Session)> LoginAsync(string username, string password)
        {
            try
            {
                var url = $"{GetBaseUrl()}/api/Usuarios/login?nombreUsuario={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(error) ? "Credenciales inválidas." : error.Replace("\"", ""), null);
                }

                var data = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                if (data == null)
                    return (false, "Error al procesar la respuesta del servidor.", null);

                // Permitir Cajero, Administrador o Recepcionista
                if (data.Rol != "Cajero" && data.Rol != "Administrador" && data.Rol != "Recepcionista")
                {
                    return (false, $"El usuario tiene el rol '{data.Rol}'. Solo los roles 'Recepcionista', 'Cajero' o 'Administrador' pueden acceder a esta aplicación.", null);
                }

                string sucursalNombre = data.SucursalId switch
                {
                    1 => "Sede Central Santo Domingo",
                    2 => "Regional Norte Santiago",
                    3 => "Regional Sur Barahona",
                    _ => "Sede Institucional"
                };

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
            catch (Exception ex)
            {
                return (false, $"No se pudo conectar con el servidor: {ex.Message}", null);
            }
        }

        // ==========================================
        // 1. VENTA DE PRODUCTOS (INVENTARIO Y POS)
        // ==========================================
        public async Task<List<ProductoItem>> GetProductosInventarioAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ProductoItem>>($"{GetBaseUrl()}/api/inventario", _jsonOptions);
                return response ?? new List<ProductoItem>();
            }
            catch
            {
                return new List<ProductoItem>();
            }
        }

        public async Task<(bool Success, string Message, TransaccionCajaDto? Recibo)> VenderCarritoAsync(
            List<CarritoItem> items, string metodoPago, decimal montoRecibido, string cliente, int? ordenId = null)
        {
            try
            {
                var payload = new VentaCarritoPayload
                {
                    MetodoPago = metodoPago,
                    MontoRecibido = montoRecibido,
                    Cajero = SessionManager.CurrentSession?.Nombre ?? "Caja Principal",
                    Sucursal = SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central",
                    Cliente = string.IsNullOrWhiteSpace(cliente) ? "Cliente de Contado" : cliente,
                    OrdenId = ordenId
                };

                foreach (var it in items)
                {
                    payload.Items.Add(new VentaCarritoItemPayload
                    {
                        ProductoId = it.ProductoId,
                        Cantidad = it.Cantidad
                    });
                }

                var response = await _httpClient.PostAsJsonAsync($"{GetBaseUrl()}/api/caja/vender-carrito", payload, _jsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Error al procesar la venta." : err.Replace("\"", ""), null);
                }

                var doc = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                TransaccionCajaDto? recibo = null;
                if (doc.TryGetProperty("recibo", out var reciboEl))
                {
                    recibo = JsonSerializer.Deserialize<TransaccionCajaDto>(reciboEl.GetRawText(), _jsonOptions);
                }

                string msg = doc.TryGetProperty("mensaje", out var msgEl) ? msgEl.GetString() ?? "Venta completada." : "Venta completada.";
                return (true, msg, recibo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al comunicar con Caja: {ex.Message}", null);
            }
        }

        // ==========================================
        // 2. MASAJES TACTO
        // ==========================================
        public async Task<List<SesionMasajeItem>> GetMasajesPendientesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<SesionMasajeItem>>($"{GetBaseUrl()}/api/caja/masajes-pendientes", _jsonOptions);
                return response ?? new List<SesionMasajeItem>();
            }
            catch
            {
                return new List<SesionMasajeItem>();
            }
        }

        public async Task<(bool Success, string Message, TransaccionCajaDto? Recibo)> CobrarMasajeAsync(
            int id, string metodoPago, decimal montoRecibido)
        {
            try
            {
                var cajero = Uri.EscapeDataString(SessionManager.CurrentSession?.Nombre ?? "Caja Principal");
                var sucursal = Uri.EscapeDataString(SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central");
                var url = $"{GetBaseUrl()}/api/caja/cobrar-masaje/{id}?metodoPago={Uri.EscapeDataString(metodoPago)}&montoRecibido={montoRecibido}&cajero={cajero}&sucursal={sucursal}";

                var response = await _httpClient.PostAsync(url, null);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Error al cobrar el masaje." : err.Replace("\"", ""), null);
                }

                var doc = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                TransaccionCajaDto? recibo = null;
                if (doc.TryGetProperty("recibo", out var reciboEl))
                {
                    recibo = JsonSerializer.Deserialize<TransaccionCajaDto>(reciboEl.GetRawText(), _jsonOptions);
                }

                string msg = doc.TryGetProperty("mensaje", out var msgEl) ? msgEl.GetString() ?? "Masaje cobrado." : "Masaje cobrado.";
                return (true, msg, recibo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al comunicar con Caja: {ex.Message}", null);
            }
        }

        // ==========================================
        // 3. PRÉSTAMOS DE EMPRENDIMIENTO
        // ==========================================
        public async Task<List<PrestamoItem>> GetPrestamosActivosAsync(string? filtro = null)
        {
            try
            {
                var url = $"{GetBaseUrl()}/api/caja/prestamos-activos";
                if (!string.IsNullOrWhiteSpace(filtro))
                    url += $"?filtro={Uri.EscapeDataString(filtro)}";

                var response = await _httpClient.GetFromJsonAsync<List<PrestamoItem>>(url, _jsonOptions);
                return response ?? new List<PrestamoItem>();
            }
            catch
            {
                return new List<PrestamoItem>();
            }
        }

        public async Task<(bool Success, string Message, decimal NuevoSaldo, TransaccionCajaDto? Recibo)> AbonarPrestamoAsync(
            int prestamoId, decimal monto, string metodoPago, decimal montoRecibido)
        {
            try
            {
                var cajero = Uri.EscapeDataString(SessionManager.CurrentSession?.Nombre ?? "Caja Principal");
                var sucursal = Uri.EscapeDataString(SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central");
                var url = $"{GetBaseUrl()}/api/caja/abonar-prestamo?prestamoId={prestamoId}&monto={monto}&metodoPago={Uri.EscapeDataString(metodoPago)}&montoRecibido={montoRecibido}&cajero={cajero}&sucursal={sucursal}";

                var response = await _httpClient.PostAsync(url, null);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Error al registrar el abono." : err.Replace("\"", ""), 0, null);
                }

                var doc = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                decimal nuevoSaldo = doc.TryGetProperty("nuevoSaldo", out var saldoEl) ? saldoEl.GetDecimal() : 0;

                TransaccionCajaDto? recibo = null;
                if (doc.TryGetProperty("recibo", out var reciboEl))
                {
                    recibo = JsonSerializer.Deserialize<TransaccionCajaDto>(reciboEl.GetRawText(), _jsonOptions);
                }

                string msg = doc.TryGetProperty("mensaje", out var msgEl) ? msgEl.GetString() ?? "Abono aplicado." : "Abono aplicado.";
                return (true, msg, nuevoSaldo, recibo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al comunicar con Caja: {ex.Message}", 0, null);
            }
        }

        // ==========================================
        // 4. DONACIONES EN VENTANILLA
        // ==========================================
        public async Task<(bool Success, string Message, TransaccionCajaDto? Recibo)> RegistrarDonacionAsync(DonacionRequest donacion)
        {
            try
            {
                var cajero = Uri.EscapeDataString(SessionManager.CurrentSession?.Nombre ?? "Caja Principal");
                var sucursal = Uri.EscapeDataString(SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central");
                var url = $"{GetBaseUrl()}/api/caja/donacion?cajero={cajero}&sucursal={sucursal}";

                var response = await _httpClient.PostAsJsonAsync(url, donacion, _jsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Error al registrar la donación." : err.Replace("\"", ""), null);
                }

                var doc = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                TransaccionCajaDto? recibo = null;
                if (doc.TryGetProperty("recibo", out var reciboEl))
                {
                    recibo = JsonSerializer.Deserialize<TransaccionCajaDto>(reciboEl.GetRawText(), _jsonOptions);
                }

                string msg = doc.TryGetProperty("mensaje", out var msgEl) ? msgEl.GetString() ?? "Donación registrada." : "Donación registrada.";
                return (true, msg, recibo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al comunicar con Caja: {ex.Message}", null);
            }
        }

        // ==========================================
        // 5. CUADRE DE CAJA Y TRANSACCIONES
        // ==========================================
        public async Task<CuadreCajaDto?> GetCuadreCajaAsync(DateTime? fecha = null, string? cajero = null)
        {
            try
            {
                var targetDate = fecha ?? DateTime.Today;
                var url = $"{GetBaseUrl()}/api/caja/cuadre-caja?fecha={targetDate:yyyy-MM-dd}";
                if (!string.IsNullOrWhiteSpace(cajero))
                    url += $"&cajero={Uri.EscapeDataString(cajero)}";

                var result = await _httpClient.GetFromJsonAsync<CuadreCajaDto>(url, _jsonOptions);
                return result;
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
                var targetDate = fecha ?? DateTime.Today;
                var url = $"{GetBaseUrl()}/api/caja/transacciones?fecha={targetDate:yyyy-MM-dd}";
                if (!string.IsNullOrWhiteSpace(cajero))
                    url += $"&cajero={Uri.EscapeDataString(cajero)}";

                var list = await _httpClient.GetFromJsonAsync<List<TransaccionCajaDto>>(url, _jsonOptions);
                return list ?? new List<TransaccionCajaDto>();
            }
            catch
            {
                return new List<TransaccionCajaDto>();
            }
        }

        // ==========================================
        // 6. GESTIÓN DE USUARIOS / CAJEROS (ADMIN)
        // ==========================================
        public async Task<List<UsuarioDto>> GetUsuariosAsync()
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<UsuarioDto>>($"{GetBaseUrl()}/api/Usuarios", _jsonOptions);
                return list ?? new List<UsuarioDto>();
            }
            catch
            {
                return new List<UsuarioDto>();
            }
        }

        public async Task<(bool Success, string Message, UsuarioDto? Usuario)> CrearUsuarioAsync(
            string nombreCompleto, string nombreUsuario, string correo, string password, int rol, int sucursalId)
        {
            try
            {
                var payload = new
                {
                    nombreCompleto,
                    nombreUsuario,
                    correo,
                    passwordHash = password,
                    rol,
                    sucursalId,
                    activo = true
                };

                var response = await _httpClient.PostAsJsonAsync($"{GetBaseUrl()}/api/Usuarios", payload, _jsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Error al crear el empleado." : err.Replace("\"", ""), null);
                }

                var usuarioCreado = await response.Content.ReadFromJsonAsync<UsuarioDto>(_jsonOptions);
                return (true, "Empleado registrado con éxito.", usuarioCreado);
            }
            catch (Exception ex)
            {
                return (false, $"Error al conectar con el servidor: {ex.Message}", null);
            }
        }

        // ==========================================
        // 7. CLIENTES FRECUENTES, CITAS Y PRÉSTAMOS
        // ==========================================
        public async Task<List<ClienteFrecuenteDto>> GetClientesFrecuentesAsync()
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<ClienteFrecuenteDto>>($"{GetBaseUrl()}/api/caja/clientes-frecuentes", _jsonOptions);
                return list ?? new List<ClienteFrecuenteDto>();
            }
            catch
            {
                return new List<ClienteFrecuenteDto>();
            }
        }

        public async Task<List<BeneficiarioInfo>> GetBeneficiariosAsync()
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<BeneficiarioInfo>>($"{GetBaseUrl()}/api/caja/beneficiarios", _jsonOptions);
                return list ?? new List<BeneficiarioInfo>();
            }
            catch
            {
                return new List<BeneficiarioInfo>();
            }
        }

        public async Task<(bool Success, string Message)> CrearMasajeAsync(CrearMasajeDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{GetBaseUrl()}/api/caja/crear-masaje", dto, _jsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Error al agendar la sesión." : err.Replace("\"", ""));
                }
                return (true, "Cita de masaje TACTO agendada exitosamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al conectar con Caja: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CrearPrestamoAsync(CrearPrestamoDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{GetBaseUrl()}/api/caja/crear-prestamo", dto, _jsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Error al otorgar el préstamo." : err.Replace("\"", ""));
                }
                return (true, "Préstamo de emprendimiento otorgado exitosamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al conectar con Caja: {ex.Message}");
            }
        }

        // ==========================================
        // 8. ÓRDENES DE PEDIDO Y COTIZACIONES
        // ==========================================
        public async Task<List<OrdenPedidoCajaDto>> GetOrdenesPendientesAsync()
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<OrdenPedidoCajaDto>>($"{GetBaseUrl()}/api/caja/ordenes-pendientes", _jsonOptions);
                return list ?? new List<OrdenPedidoCajaDto>();
            }
            catch
            {
                return new List<OrdenPedidoCajaDto>();
            }
        }

        public async Task<(bool Success, string Message, OrdenPedidoCajaDto? Orden)> CrearOrdenAsync(CrearOrdenDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{GetBaseUrl()}/api/caja/crear-orden", dto, _jsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Error al generar la orden de pedido." : err.Replace("\"", ""), null);
                }

                var doc = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                OrdenPedidoCajaDto? orden = null;
                if (doc.TryGetProperty("orden", out var ordenEl))
                {
                    orden = JsonSerializer.Deserialize<OrdenPedidoCajaDto>(ordenEl.GetRawText(), _jsonOptions);
                }

                string msg = doc.TryGetProperty("mensaje", out var msgEl) ? msgEl.GetString() ?? "Orden generada exitosamente." : "Orden generada exitosamente.";
                return (true, msg, orden);
            }
            catch (Exception ex)
            {
                return (false, $"Error al conectar con el servidor: {ex.Message}", null);
            }
        }
    }
}
