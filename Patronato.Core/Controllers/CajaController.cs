using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using System.Text.Json;

namespace Patronato.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CajaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CajaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ENDPOINTS DE CONSULTA PARA LA CAJA
        // ==========================================

        // GET: api/caja/masajes-pendientes
        [HttpGet("masajes-pendientes")]
        public async Task<IActionResult> GetMasajesPendientes()
        {
            var pendientes = await _context.SesionesMasaje
                .Where(s => !s.EstaFacturadoEnCaja)
                .OrderBy(s => s.FechaHora)
                .ToListAsync();

            return Ok(pendientes);
        }

        // GET: api/caja/prestamos-activos
        [HttpGet("prestamos-activos")]
        public async Task<IActionResult> GetPrestamosActivos([FromQuery] string? filtro = null)
        {
            var query = _context.PrestamosEmprendimiento
                .Include(p => p.Beneficiario)
                .Where(p => p.Estado == EstadoPrestamo.Activo && p.SaldoPendiente > 0);

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var f = filtro.Trim().ToLower();
                query = query.Where(p =>
                    (p.Beneficiario != null && (
                        p.Beneficiario.Nombres.ToLower().Contains(f) ||
                        p.Beneficiario.Apellidos.ToLower().Contains(f) ||
                        p.Beneficiario.Cedula.Contains(f))) ||
                    p.Id.ToString() == f);
            }

            var prestamos = await query.ToListAsync();
            return Ok(prestamos);
        }

        // GET: api/caja/transacciones
        [HttpGet("transacciones")]
        public async Task<IActionResult> GetTransacciones([FromQuery] DateTime? fecha = null, [FromQuery] string? cajero = null)
        {
            var targetDate = (fecha ?? DateTime.Today).Date;
            var query = _context.TransaccionesCaja
                .Where(t => t.FechaHora.Date == targetDate);

            if (!string.IsNullOrWhiteSpace(cajero))
            {
                query = query.Where(t => t.Cajero.ToLower() == cajero.Trim().ToLower());
            }

            var list = await query.OrderByDescending(t => t.FechaHora).ToListAsync();
            return Ok(list);
        }

        // GET: api/caja/cuadre-caja (Arqueo de turno/día)
        [HttpGet("cuadre-caja")]
        public async Task<IActionResult> GetCuadreCaja([FromQuery] DateTime? fecha = null, [FromQuery] string? cajero = null)
        {
            var targetDate = (fecha ?? DateTime.Today).Date;
            var query = _context.TransaccionesCaja
                .Where(t => t.FechaHora.Date == targetDate);

            if (!string.IsNullOrWhiteSpace(cajero))
            {
                query = query.Where(t => t.Cajero.ToLower() == cajero.Trim().ToLower());
            }

            var transacciones = await query.ToListAsync();

            var totalGeneral = transacciones.Sum(t => t.MontoTotal);
            var totalEfectivo = transacciones.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.MontoTotal);
            var totalTarjeta = transacciones.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.MontoTotal);
            var totalTransferencia = transacciones.Where(t => t.MetodoPago == "Transferencia").Sum(t => t.MontoTotal);

            var totalVentas = transacciones.Where(t => t.TipoTransaccion == "VentaProductos").Sum(t => t.MontoTotal);
            var totalMasajes = transacciones.Where(t => t.TipoTransaccion == "MasajeTacto").Sum(t => t.MontoTotal);
            var totalPrestamos = transacciones.Where(t => t.TipoTransaccion == "AbonoPrestamo").Sum(t => t.MontoTotal);
            var totalDonaciones = transacciones.Where(t => t.TipoTransaccion == "Donacion").Sum(t => t.MontoTotal);

            return Ok(new
            {
                fecha = targetDate.ToString("yyyy-MM-dd"),
                cajero = cajero ?? "Todos",
                cantidadTransacciones = transacciones.Count,
                totalGeneral,
                porMetodo = new
                {
                    efectivo = totalEfectivo,
                    tarjeta = totalTarjeta,
                    transferencia = totalTransferencia
                },
                porConcepto = new
                {
                    ventasProductos = totalVentas,
                    masajesTacto = totalMasajes,
                    abonosPrestamos = totalPrestamos,
                    donaciones = totalDonaciones
                },
                movimientos = transacciones
            });
        }

        // ==========================================
        // 2. ENDPOINTS TRANSACCIONALES DE COBRO
        // ==========================================

        // 1. POST: api/caja/cobrar-masaje/1 (Cobra una sesión de masajes TACTO)
        [HttpPost("cobrar-masaje/{id}")]
        public async Task<IActionResult> CobrarMasaje(
            int id,
            [FromQuery] string metodoPago = "Efectivo",
            [FromQuery] decimal? montoRecibido = null,
            [FromQuery] string cajero = "Caja Principal",
            [FromQuery] string sucursal = "Sede Central")
        {
            var sesion = await _context.SesionesMasaje.FindAsync(id);
            if (sesion == null)
                return NotFound("Sesión de masaje no encontrada.");

            if (sesion.EstaFacturadoEnCaja)
                return BadRequest("Esta sesión ya fue cobrada previamente.");

            sesion.MarcarComoCobrado();

            bool esEfectivo = string.Equals(metodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
            decimal recibido = esEfectivo && montoRecibido.HasValue && montoRecibido.Value > 0 ? montoRecibido.Value : sesion.PrecioTarifa;
            decimal devuelta = esEfectivo ? Math.Max(0, recibido - sesion.PrecioTarifa) : 0;

            var transaccion = new TransaccionCaja
            {
                NumeroRecibo = GenerarNumeroRecibo("MAS"),
                FechaHora = DateTime.Now,
                TipoTransaccion = "MasajeTacto",
                Concepto = $"Masaje TACTO ({sesion.DuracionMinutos} min) - Masajista: {sesion.NombreMasajista}",
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

            _context.TransaccionesCaja.Add(transaccion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Masaje cobrado con éxito en Caja.",
                monto = sesion.PrecioTarifa,
                recibo = transaccion
            });
        }

        // 2. POST: api/caja/abonar-prestamo (Recibe el pago de cuota de un colmado/paletera)
        [HttpPost("abonar-prestamo")]
        public async Task<IActionResult> AbonarPrestamo(
            [FromQuery] int prestamoId,
            [FromQuery] decimal monto,
            [FromQuery] string metodoPago = "Efectivo",
            [FromQuery] decimal? montoRecibido = null,
            [FromQuery] string cajero = "Caja Principal",
            [FromQuery] string sucursal = "Sede Central")
        {
            var prestamo = await _context.PrestamosEmprendimiento
                .Include(p => p.Beneficiario)
                .FirstOrDefaultAsync(p => p.Id == prestamoId);

            if (prestamo == null)
                return NotFound("Préstamo no encontrado.");

            try
            {
                var saldoAnterior = prestamo.SaldoPendiente;
                prestamo.RegistrarAbono(monto);

                bool esEfectivo = string.Equals(metodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
                decimal recibido = esEfectivo && montoRecibido.HasValue && montoRecibido.Value > 0 ? montoRecibido.Value : monto;
                decimal devuelta = esEfectivo ? Math.Max(0, recibido - monto) : 0;

                string nombreBeneficiario = prestamo.Beneficiario != null
                    ? $"{prestamo.Beneficiario.Nombres} {prestamo.Beneficiario.Apellidos}".Trim()
                    : "Beneficiario No. " + prestamo.BeneficiarioId;

                var transaccion = new TransaccionCaja
                {
                    NumeroRecibo = GenerarNumeroRecibo("ABN"),
                    FechaHora = DateTime.Now,
                    TipoTransaccion = "AbonoPrestamo",
                    Concepto = $"Abono a Préstamo #{prestamo.Id} ({prestamo.Rubro}) - {nombreBeneficiario}",
                    MontoTotal = monto,
                    MetodoPago = metodoPago,
                    MontoRecibido = recibido,
                    Devuelta = devuelta,
                    Cajero = cajero,
                    Sucursal = sucursal,
                    ClienteOBeneficiario = nombreBeneficiario,
                    IdentificacionCliente = prestamo.Beneficiario?.Cedula ?? string.Empty,
                    DetalleLineas = $"Abono a capital. Saldo anterior: RD$ {saldoAnterior:N2} | Saldo restante: RD$ {prestamo.SaldoPendiente:N2}",
                    ReferenciaId = prestamo.Id
                };

                _context.TransaccionesCaja.Add(transaccion);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Abono aplicado con éxito.",
                    nuevoSaldo = prestamo.SaldoPendiente,
                    estado = prestamo.Estado.ToString(),
                    recibo = transaccion
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 3. POST: api/caja/vender-producto (Vende producto individual)
        [HttpPost("vender-producto")]
        public async Task<IActionResult> VenderProducto(
            [FromQuery] int productoId,
            [FromQuery] int cantidad,
            [FromQuery] string metodoPago = "Efectivo",
            [FromQuery] decimal? montoRecibido = null,
            [FromQuery] string cajero = "Caja Principal",
            [FromQuery] string sucursal = "Sede Central",
            [FromQuery] string? cliente = null)
        {
            var producto = await _context.ProductosArticulos.FindAsync(productoId);
            if (producto == null)
                return NotFound("Artículo no encontrado en el inventario.");

            try
            {
                producto.DescontarStock(cantidad);
                decimal totalVenta = producto.PrecioVenta * cantidad;

                bool esEfectivo = string.Equals(metodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
                decimal recibido = esEfectivo && montoRecibido.HasValue && montoRecibido.Value > 0 ? montoRecibido.Value : totalVenta;
                decimal devuelta = esEfectivo ? Math.Max(0, recibido - totalVenta) : 0;

                var transaccion = new TransaccionCaja
                {
                    NumeroRecibo = GenerarNumeroRecibo("VTA"),
                    FechaHora = DateTime.Now,
                    TipoTransaccion = "VentaProductos",
                    Concepto = $"Venta de {cantidad}x {producto.Nombre}",
                    MontoTotal = totalVenta,
                    MetodoPago = metodoPago,
                    MontoRecibido = recibido,
                    Devuelta = devuelta,
                    Cajero = cajero,
                    Sucursal = sucursal,
                    ClienteOBeneficiario = cliente ?? "Cliente de Contado",
                    DetalleLineas = $"{cantidad}x [{producto.Codigo}] {producto.Nombre} @ RD$ {producto.PrecioVenta:N2} = RD$ {totalVenta:N2}",
                    ReferenciaId = producto.Id
                };

                _context.TransaccionesCaja.Add(transaccion);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Venta realizada de {cantidad} unidad(es) de {producto.Nombre}.",
                    totalCobrado = totalVenta,
                    stockRestante = producto.StockDisponible,
                    recibo = transaccion
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 4. POST: api/caja/vender-carrito (Venta POS de múltiples productos en un solo ticket)
        [HttpPost("vender-carrito")]
        public async Task<IActionResult> VenderCarrito([FromBody] VentaCarritoRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest("El carrito de compras está vacío.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal totalVenta = 0;
                var lineasDetalle = new List<string>();

                foreach (var item in request.Items)
                {
                    var producto = await _context.ProductosArticulos.FindAsync(item.ProductoId);
                    if (producto == null)
                        return NotFound($"Producto con ID {item.ProductoId} no existe.");

                    producto.DescontarStock(item.Cantidad);
                    decimal subtotal = producto.PrecioVenta * item.Cantidad;
                    totalVenta += subtotal;

                    lineasDetalle.Add($"{item.Cantidad}x {producto.Nombre} (RD$ {producto.PrecioVenta:N2}) = RD$ {subtotal:N2}");
                }

                bool esEfectivo = string.Equals(request.MetodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
                decimal recibido = esEfectivo && request.MontoRecibido > 0 ? request.MontoRecibido : totalVenta;
                decimal devuelta = esEfectivo ? Math.Max(0, recibido - totalVenta) : 0;

                var transaccionCaja = new TransaccionCaja
                {
                    NumeroRecibo = GenerarNumeroRecibo("POS"),
                    FechaHora = DateTime.Now,
                    TipoTransaccion = "VentaProductos",
                    Concepto = $"Venta POS ({request.Items.Sum(i => i.Cantidad)} artículos)",
                    MontoTotal = totalVenta,
                    MetodoPago = request.MetodoPago,
                    MontoRecibido = recibido,
                    Devuelta = devuelta,
                    Cajero = request.Cajero,
                    Sucursal = request.Sucursal,
                    ClienteOBeneficiario = request.Cliente ?? "Cliente de Contado",
                    DetalleLineas = string.Join("\n", lineasDetalle)
                };

                _context.TransaccionesCaja.Add(transaccionCaja);

                if (request.OrdenId.HasValue && request.OrdenId.Value > 0)
                {
                    var ordenPrevia = await _context.OrdenesPedidoCaja.FindAsync(request.OrdenId.Value);
                    if (ordenPrevia != null)
                    {
                        ordenPrevia.Estado = "Cobrada";
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Venta registrada con éxito.",
                    totalCobrado = totalVenta,
                    recibo = transaccionCaja
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

        // 5. POST: api/caja/donacion (Registra una donación recibida en ventanilla)
        [HttpPost("donacion")]
        public async Task<IActionResult> RegistrarDonacion(
            [FromBody] Donacion donacion,
            [FromQuery] string cajero = "Caja Principal",
            [FromQuery] string sucursal = "Sede Central")
        {
            donacion.EstaRegistradaEnCaja = true;
            _context.Donaciones.Add(donacion);
            await _context.SaveChangesAsync();

            var transaccion = new TransaccionCaja
            {
                NumeroRecibo = GenerarNumeroRecibo("DON"),
                FechaHora = DateTime.Now,
                TipoTransaccion = "Donacion",
                Concepto = $"Donación Institucional - {donacion.Donante}",
                MontoTotal = donacion.Monto,
                MetodoPago = donacion.MetodoPago,
                MontoRecibido = donacion.Monto,
                Devuelta = 0,
                Cajero = cajero,
                Sucursal = sucursal,
                ClienteOBeneficiario = donacion.Donante,
                IdentificacionCliente = donacion.RncCedula,
                DetalleLineas = $"Donación recibida para apoyo de programas institucionales. Requiere NCF: {(donacion.RequiereComprobanteFiscal ? "SÍ" : "NO")}",
                ReferenciaId = donacion.Id
            };

            _context.TransaccionesCaja.Add(transaccion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Donación registrada con éxito en Caja.",
                reciboId = donacion.Id,
                recibo = transaccion
            });
        }

        // ==========================================
        // 3. REGISTRO DE NUEVAS CITAS Y PRÉSTAMOS
        // ==========================================

        // GET: api/caja/beneficiarios (Lista de beneficiarios invidentes)
        [HttpGet("beneficiarios")]
        public async Task<IActionResult> GetBeneficiarios()
        {
            var beneficiarios = await _context.Beneficiarios
                .Where(b => b.Activo)
                .OrderBy(b => b.Nombres)
                .ToListAsync();

            return Ok(beneficiarios);
        }

        // GET: api/caja/clientes-frecuentes (Autocompletado POS y Donaciones)
        [HttpGet("clientes-frecuentes")]
        public async Task<IActionResult> GetClientesFrecuentes()
        {
            var benefs = await _context.Beneficiarios
                .Select(b => new { Nombre = $"{b.Nombres} {b.Apellidos}".Trim(), Documento = b.Cedula, Tipo = "Beneficiario" })
                .ToListAsync();

            var donantes = await _context.Donaciones
                .Where(d => !string.IsNullOrEmpty(d.Donante))
                .Select(d => new { Nombre = d.Donante, Documento = d.RncCedula, Tipo = "Donante" })
                .Distinct()
                .ToListAsync();

            var clientesCaja = await _context.TransaccionesCaja
                .Where(t => !string.IsNullOrEmpty(t.ClienteOBeneficiario) && t.ClienteOBeneficiario != "Consumidor Final")
                .Select(t => new { Nombre = t.ClienteOBeneficiario!, Documento = t.IdentificacionCliente ?? "", Tipo = "Cliente POS" })
                .Distinct()
                .ToListAsync();

            var todos = benefs.Concat(donantes).Concat(clientesCaja)
                .GroupBy(c => c.Nombre.Trim().ToLower())
                .Select(g => g.First())
                .OrderBy(c => c.Nombre)
                .ToList();

            return Ok(todos);
        }

        // POST: api/caja/crear-masaje (Agendar nueva cita de masaje TACTO)
        [HttpPost("crear-masaje")]
        public async Task<IActionResult> CrearMasaje([FromBody] CrearMasajeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreCliente))
                return BadRequest("El nombre del cliente es obligatorio.");

            var sesion = new SesionMasajeTacto
            {
                NombreCliente = request.NombreCliente.Trim(),
                TelefonoCliente = request.TelefonoCliente?.Trim() ?? string.Empty,
                NombreMasajista = string.IsNullOrWhiteSpace(request.NombreMasajista) ? "Terapeuta Invidente Asignado" : request.NombreMasajista.Trim(),
                Sede = request.Sede,
                FechaHora = DateTime.Now,
                DuracionMinutos = request.DuracionMinutos > 0 ? request.DuracionMinutos : 45,
                PrecioTarifa = request.PrecioTarifa > 0 ? request.PrecioTarifa : 500m,
                EstaFacturadoEnCaja = false
            };

            _context.SesionesMasaje.Add(sesion);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Cita de masaje TACTO agendada exitosamente.", sesion });
        }

        // POST: api/caja/crear-prestamo (Otorgar nuevo crédito a beneficiario)
        [HttpPost("crear-prestamo")]
        public async Task<IActionResult> CrearPrestamo([FromBody] CrearPrestamoRequest request)
        {
            var beneficiario = await _context.Beneficiarios.FindAsync(request.BeneficiarioId);
            if (beneficiario == null)
                return NotFound("Beneficiario no encontrado en el sistema.");

            if (request.MontoAprobado <= 0)
                return BadRequest("El monto del préstamo debe ser mayor a cero.");

            var prestamo = new PrestamoEmprendimiento
            {
                BeneficiarioId = request.BeneficiarioId,
                MontoAprobado = request.MontoAprobado,
                SaldoPendiente = request.MontoAprobado,
                PlazoMeses = request.PlazoMeses > 0 ? request.PlazoMeses : 12,
                Rubro = request.Rubro,
                EstudioFactibilidadAprobado = true,
                InvolucraFamilia = request.InvolucraFamilia,
                Estado = EstadoPrestamo.Activo
            };

            _context.PrestamosEmprendimiento.Add(prestamo);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Préstamo de emprendimiento otorgado exitosamente.", prestamoId = prestamo.Id });
        }

        // ==========================================
        // 4. ÓRDENES DE PEDIDO Y COTIZACIONES (RECEPCIÓN Y CAJA)
        // ==========================================

        // GET: api/caja/ordenes-pendientes
        [HttpGet("ordenes-pendientes")]
        public async Task<IActionResult> GetOrdenesPendientes()
        {
            var ordenes = await _context.OrdenesPedidoCaja
                .Where(o => o.Estado == "Pendiente")
                .OrderByDescending(o => o.FechaHora)
                .ToListAsync();

            return Ok(ordenes);
        }

        // POST: api/caja/crear-orden (Recepción genera orden de insumos o cotización)
        [HttpPost("crear-orden")]
        public async Task<IActionResult> CrearOrden([FromBody] CrearOrdenRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest("La orden de pedido no contiene artículos.");

            decimal total = 0;
            foreach (var item in request.Items)
            {
                var prod = await _context.ProductosArticulos.FindAsync(item.ProductoId);
                if (prod == null)
                    return NotFound($"Producto #{item.ProductoId} no existe en el catálogo.");

                if (prod.StockDisponible < item.Cantidad)
                    return BadRequest($"Stock insuficiente para '{prod.Nombre}'. Disponibles: {prod.StockDisponible}.");

                total += prod.PrecioVenta * item.Cantidad;
            }

            var orden = new OrdenPedidoCaja
            {
                NumeroOrden = $"ORD-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                FechaHora = DateTime.Now,
                ClienteNombre = string.IsNullOrWhiteSpace(request.ClienteNombre) ? "Consumidor Final" : request.ClienteNombre.Trim(),
                ClienteDocumento = request.ClienteDocumento?.Trim(),
                CreadoPor = string.IsNullOrWhiteSpace(request.CreadoPor) ? "Recepción" : request.CreadoPor.Trim(),
                Estado = "Pendiente",
                Total = total,
                ItemsJson = JsonSerializer.Serialize(request.Items),
                Notas = request.Notas
            };

            _context.OrdenesPedidoCaja.Add(orden);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = $"Orden {orden.NumeroOrden} creada exitosamente.",
                orden
            });
        }

        private static string GenerarNumeroRecibo(string prefijo)
        {
            return $"{prefijo}-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        }
    }

    public class CrearOrdenRequest
    {
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteDocumento { get; set; }
        public string CreadoPor { get; set; } = "Recepción";
        public string? Notas { get; set; }
        public List<OrdenItemDto> Items { get; set; } = new();
    }

    public class OrdenItemDto
    {
        public int ProductoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    public class CrearMasajeRequest
    {
        public string NombreCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string NombreMasajista { get; set; } = "David Silverio (Terapeuta Invidente)";
        public SedeEnum_REVIEW Sede { get; set; } = SedeEnum_REVIEW.SantoDomingo;
        public int DuracionMinutos { get; set; } = 45;
        public decimal PrecioTarifa { get; set; } = 500m;
    }

    public class CrearPrestamoRequest
    {
        public int BeneficiarioId { get; set; }
        public decimal MontoAprobado { get; set; }
        public int PlazoMeses { get; set; } = 12;
        public RubroMicroemprendimiento Rubro { get; set; } = RubroMicroemprendimiento.Colmado;
        public bool InvolucraFamilia { get; set; } = true;
    }

    public class VentaCarritoItemRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    public class VentaCarritoRequest
    {
        public List<VentaCarritoItemRequest> Items { get; set; } = new();
        public string MetodoPago { get; set; } = "Efectivo";
        public decimal MontoRecibido { get; set; }
        public string Cajero { get; set; } = "Caja Principal";
        public string Sucursal { get; set; } = "Sede Central";
        public string? Cliente { get; set; }
        public int? OrdenId { get; set; }
    }
}
