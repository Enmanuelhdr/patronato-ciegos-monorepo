using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize(Roles = "Administrador,Cajero")]
    public class CajaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Usuario> _hasher;

        public CajaController(ApplicationDbContext context, IPasswordHasher<Usuario> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        private async Task<TurnoCaja?> ObtenerTurnoActivoAsync(string? cajero = null, string? sucursal = null)
        {
            var query = _context.TurnosCaja.Where(t => t.Estado == "Abierto");
            if (!string.IsNullOrWhiteSpace(cajero) && !cajero.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                query = query.Where(t => t.Cajero.ToLower() == cajero.Trim().ToLower());
            if (!string.IsNullOrWhiteSpace(sucursal))
                query = query.Where(t => t.Sucursal.ToLower() == sucursal.Trim().ToLower());

            return await query.OrderByDescending(t => t.FechaHoraApertura).FirstOrDefaultAsync()
                ?? await _context.TurnosCaja.Where(t => t.Estado == "Abierto").OrderByDescending(t => t.FechaHoraApertura).FirstOrDefaultAsync();
        }

        private string? ObtenerStan() =>
            Request.Headers["X-Stan"].FirstOrDefault() ?? Request.Headers["X-Idempotency-Key"].FirstOrDefault();

        private async Task<TransaccionCaja?> BuscarTransaccionExistenteAsync(string? stan)
        {
            if (string.IsNullOrWhiteSpace(stan)) return null;
            var hoy = DateTime.Today;
            var mañana = hoy.AddDays(1);
            return await _context.TransaccionesCaja
                .FirstOrDefaultAsync(t => (t.Stan == stan || t.IdempotencyKey == stan) && t.FechaHora >= hoy && t.FechaHora < mañana);
        }

        // ==========================================
        // 1. ENDPOINTS DE CONSULTA PARA LA CAJA
        // ==========================================

        // GET: api/caja/masajes-pendientes
        [HttpGet("masajes-pendientes")]
        [Authorize(Roles = "Administrador,Cajero,Recepcionista")]
        public async Task<IActionResult> GetMasajesPendientes()
        {
            var pendientes = await _context.SesionesMasaje
                .Where(s => !s.EstaFacturadoEnCaja)
                .OrderBy(s => s.FechaHora)
                .ToListAsync();

            return Ok(pendientes);
        }

        // GET: api/caja/consultas-pendientes (las cubiertas por ARS no requieren cobro en efectivo)
        [HttpGet("consultas-pendientes")]
        [Authorize(Roles = "Administrador,Cajero,Recepcionista")]
        public async Task<IActionResult> GetConsultasPendientes()
        {
            var pendientes = await _context.ConsultasOftalmologicas
                .Include(c => c.Beneficiario)
                .Where(c => !c.EstaFacturadaEnCaja && !c.CubiertoPorARS)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            return Ok(pendientes);
        }

        // GET: api/caja/prestamos-activos
        [HttpGet("prestamos-activos")]
        [Authorize(Roles = "Administrador,Cajero,Recepcionista")]
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
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> GetTransacciones(
            [FromQuery] DateTime? fecha = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] string? cajero = null)
        {
            var start = (fechaInicio ?? fecha ?? DateTime.Today).Date;
            var end = (fechaFin ?? fecha ?? DateTime.Today).Date.AddDays(1);
            var query = _context.TransaccionesCaja
                .Where(t => t.FechaHora >= start && t.FechaHora < end);

            if (!string.IsNullOrWhiteSpace(cajero) && !cajero.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Cajero.ToLower() == cajero.Trim().ToLower());
            }

            var list = await query.OrderByDescending(t => t.FechaHora).ToListAsync();
            return Ok(list);
        }

        // GET: api/caja/cuadre-caja (Arqueo de turno/día o rango)
        [HttpGet("cuadre-caja")]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> GetCuadreCaja(
            [FromQuery] DateTime? fecha = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] string? cajero = null)
        {
            var start = (fechaInicio ?? fecha ?? DateTime.Today).Date;
            var end = (fechaFin ?? fecha ?? DateTime.Today).Date.AddDays(1);
            var query = _context.TransaccionesCaja
                .Where(t => t.FechaHora >= start && t.FechaHora < end);

            if (!string.IsNullOrWhiteSpace(cajero) && !cajero.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Cajero.ToLower() == cajero.Trim().ToLower());
            }

            var transacciones = await query.ToListAsync();
            var validas = transacciones.Where(t => !t.EsAnulada).ToList();
            var anuladas = transacciones.Where(t => t.EsAnulada).ToList();

            var totalGeneral = validas.Sum(t => t.MontoTotal);
            var totalEfectivo = validas.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.MontoTotal);
            var totalTarjeta = validas.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.MontoTotal);
            var totalTransferencia = validas.Where(t => t.MetodoPago == "Transferencia").Sum(t => t.MontoTotal);

            var totalVentas = validas.Where(t => t.TipoTransaccion == "VentaProductos").Sum(t => t.MontoTotal);
            var totalMasajes = validas.Where(t => t.TipoTransaccion == "MasajeTacto").Sum(t => t.MontoTotal);
            var totalPrestamos = validas.Where(t => t.TipoTransaccion == "AbonoPrestamo").Sum(t => t.MontoTotal);
            var totalDonaciones = validas.Where(t => t.TipoTransaccion == "Donacion").Sum(t => t.MontoTotal);
            var totalConsultas = validas.Where(t => t.TipoTransaccion == "ConsultaOftalmologica").Sum(t => t.MontoTotal);

            var turnoActivo = await ObtenerTurnoActivoAsync(cajero);
            var fondoInicial = turnoActivo?.FondoInicial ?? 0m;
            var efectivoEsperadoEnCaja = fondoInicial + totalEfectivo;

            return Ok(new
            {
                fecha = start.ToString("yyyy-MM-dd"),
                fechaInicio = start.ToString("yyyy-MM-dd"),
                fechaFin = (fechaFin ?? fecha ?? DateTime.Today).Date.ToString("yyyy-MM-dd"),
                cajero = cajero ?? "Todos",
                tieneTurnoActivo = turnoActivo != null,
                turnoId = turnoActivo?.Id,
                fondoInicial,
                efectivoEsperadoEnCaja,
                cantidadTransacciones = validas.Count,
                cantidadAnuladas = anuladas.Count,
                totalAnulado = anuladas.Sum(t => t.MontoTotal),
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
                    donaciones = totalDonaciones,
                    consultas = totalConsultas
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
            var stan = ObtenerStan();
            var existente = await BuscarTransaccionExistenteAsync(stan);
            if (existente != null)
                return Ok(new { mensaje = "Transacción ya procesada previamente (idempotente).", monto = existente.MontoTotal, recibo = existente, yaProcesada = true });

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
                ReferenciaId = sesion.Id,
                Stan = stan,
                IdempotencyKey = stan,
                TurnoId = (await ObtenerTurnoActivoAsync(cajero, sucursal))?.Id
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

        // 1.1. POST: api/caja/cobrar-consulta/1 (Cobra una consulta oftalmológica no cubierta por ARS)
        [HttpPost("cobrar-consulta/{id}")]
        public async Task<IActionResult> CobrarConsulta(
            int id,
            [FromQuery] string metodoPago = "Efectivo",
            [FromQuery] decimal? montoRecibido = null,
            [FromQuery] string cajero = "Caja Principal",
            [FromQuery] string sucursal = "Sede Central")
        {
            var stan = ObtenerStan();
            var existente = await BuscarTransaccionExistenteAsync(stan);
            if (existente != null)
                return Ok(new { mensaje = "Transacción ya procesada previamente (idempotente).", monto = existente.MontoTotal, recibo = existente, yaProcesada = true });

            var consulta = await _context.ConsultasOftalmologicas
                .Include(c => c.Beneficiario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consulta == null)
                return NotFound("Consulta oftalmológica no encontrada.");

            if (consulta.EstaFacturadaEnCaja)
                return BadRequest("Esta consulta ya fue cobrada previamente.");

            if (consulta.CubiertoPorARS)
                return BadRequest("Esta consulta está cubierta por ARS: no se cobra en Caja.");

            consulta.MarcarComoCobrada();

            bool esEfectivo = string.Equals(metodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
            decimal recibido = esEfectivo && montoRecibido.HasValue && montoRecibido.Value > 0 ? montoRecibido.Value : consulta.TarifaConsulta;
            decimal devuelta = esEfectivo ? Math.Max(0, recibido - consulta.TarifaConsulta) : 0;

            string nombreBeneficiario = consulta.Beneficiario != null
                ? $"{consulta.Beneficiario.Nombres} {consulta.Beneficiario.Apellidos}".Trim()
                : "Beneficiario No. " + consulta.BeneficiarioId;

            var transaccion = new TransaccionCaja
            {
                NumeroRecibo = GenerarNumeroRecibo("CON"),
                FechaHora = DateTime.Now,
                TipoTransaccion = "ConsultaOftalmologica",
                Concepto = $"Consulta Oftalmológica - Dr(a). {consulta.Oftalmologo} - {nombreBeneficiario}",
                MontoTotal = consulta.TarifaConsulta,
                MetodoPago = metodoPago,
                MontoRecibido = recibido,
                Devuelta = devuelta,
                Cajero = cajero,
                Sucursal = sucursal,
                ClienteOBeneficiario = nombreBeneficiario,
                IdentificacionCliente = consulta.Beneficiario?.Cedula ?? string.Empty,
                DetalleLineas = $"1x Consulta Oftalmológica - Dr(a). {consulta.Oftalmologo} - RD$ {consulta.TarifaConsulta:N2}",
                ReferenciaId = consulta.Id,
                Stan = stan,
                IdempotencyKey = stan,
                TurnoId = (await ObtenerTurnoActivoAsync(cajero, sucursal))?.Id
            };

            _context.TransaccionesCaja.Add(transaccion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Consulta cobrada con éxito en Caja.",
                monto = consulta.TarifaConsulta,
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
            var stan = ObtenerStan();
            var existente = await BuscarTransaccionExistenteAsync(stan);
            if (existente != null)
                return Ok(new { mensaje = "Transacción ya procesada previamente (idempotente).", nuevoSaldo = 0m, estado = "Activo", recibo = existente, yaProcesada = true });

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
                    ReferenciaId = prestamo.Id,
                    Stan = stan,
                    IdempotencyKey = stan,
                    TurnoId = (await ObtenerTurnoActivoAsync(cajero, sucursal))?.Id
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
            var stan = ObtenerStan();
            var existente = await BuscarTransaccionExistenteAsync(stan);
            if (existente != null)
                return Ok(new { mensaje = "Transacción ya procesada previamente (idempotente).", totalCobrado = existente.MontoTotal, stockRestante = 0, recibo = existente, yaProcesada = true });

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
                    ReferenciaId = producto.Id,
                    Stan = stan,
                    IdempotencyKey = stan,
                    TurnoId = (await ObtenerTurnoActivoAsync(cajero, sucursal))?.Id
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

            var stan = ObtenerStan();
            var existente = await BuscarTransaccionExistenteAsync(stan);
            if (existente != null)
                return Ok(new { mensaje = "Transacción ya procesada previamente (idempotente).", totalCobrado = existente.MontoTotal, recibo = existente, yaProcesada = true });

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

                    lineasDetalle.Add($"{item.Cantidad}x [{producto.Codigo}] {producto.Nombre} (RD$ {producto.PrecioVenta:N2}) = RD$ {subtotal:N2}");
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
                    DetalleLineas = string.Join("\n", lineasDetalle),
                    ReferenciaId = request.OrdenId,
                    Stan = stan,
                    IdempotencyKey = stan,
                    TurnoId = (await ObtenerTurnoActivoAsync(request.Cajero, request.Sucursal))?.Id
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
            var stan = ObtenerStan();
            var existente = await BuscarTransaccionExistenteAsync(stan);
            if (existente != null)
                return Ok(new { mensaje = "Transacción ya procesada previamente (idempotente).", reciboId = existente.ReferenciaId ?? 0, recibo = existente, yaProcesada = true });

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
                ReferenciaId = donacion.Id,
                Stan = stan,
                IdempotencyKey = stan,
                TurnoId = (await ObtenerTurnoActivoAsync(cajero, sucursal))?.Id
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
        // 3. CONSULTAS DE APOYO PARA LA PANTALLA DE CAJA
        // ==========================================
        // (Agendar masajes/consultas/préstamos, registrar beneficiarios, etc. son tareas de
        // Recepción — ver Patronato.Core/Controllers/RecepcionController.cs y BeneficiariosController.cs.
        // Aquí solo queda lo que la propia Caja necesita leer para cobrar.)

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

        // GET: api/caja/ordenes-pendientes
        [HttpGet("ordenes-pendientes")]
        [Authorize(Roles = "Administrador,Cajero,Recepcionista")]
        public async Task<IActionResult> GetOrdenesPendientes()
        {
            var ordenes = await _context.OrdenesPedidoCaja
                .Where(o => o.Estado == "Pendiente")
                .OrderByDescending(o => o.FechaHora)
                .ToListAsync();

            return Ok(ordenes);
        }

        // ==========================================
        // 4. GESTIÓN DE TURNOS DE CAJA (APERTURA Y CIERRE)
        // ==========================================

        // GET: api/caja/turno-activo
        [HttpGet("turno-activo")]
        public async Task<IActionResult> GetTurnoActivo([FromQuery] string? cajero = null, [FromQuery] string? sucursal = null)
        {
            var turno = await ObtenerTurnoActivoAsync(cajero, sucursal);
            if (turno == null)
            {
                return Ok(new
                {
                    tieneTurnoActivo = false,
                    mensaje = "No hay ningún turno de caja abierto actualmente para este cajero/sucursal."
                });
            }

            var transacciones = await _context.TransaccionesCaja
                .Where(t => (t.TurnoId == turno.Id || (t.TurnoId == null && t.FechaHora >= turno.FechaHoraApertura)) && !t.EsAnulada)
                .ToListAsync();

            decimal totalEfectivo = transacciones.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.MontoTotal);
            decimal totalTarjeta = transacciones.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.MontoTotal);
            decimal totalTransferencia = transacciones.Where(t => t.MetodoPago == "Transferencia").Sum(t => t.MontoTotal);
            decimal totalGeneral = transacciones.Sum(t => t.MontoTotal);
            decimal efectivoEsperado = turno.FondoInicial + totalEfectivo;

            var anuladas = await _context.TransaccionesCaja
                .Where(t => (t.TurnoId == turno.Id || (t.TurnoId == null && t.FechaHora >= turno.FechaHoraApertura)) && t.EsAnulada)
                .CountAsync();

            return Ok(new
            {
                tieneTurnoActivo = true,
                turno = new
                {
                    turno.Id,
                    turno.Cajero,
                    turno.Sucursal,
                    turno.FechaHoraApertura,
                    turno.FondoInicial,
                    turno.Estado,
                    turno.ObservacionesApertura,
                    totalVentasEfectivo = totalEfectivo,
                    totalVentasTarjeta = totalTarjeta,
                    totalVentasTransferencia = totalTransferencia,
                    totalGeneralVentas = totalGeneral,
                    efectivoEsperadoEnCaja = efectivoEsperado,
                    cantidadTransacciones = transacciones.Count,
                    cantidadAnuladas = anuladas
                }
            });
        }

        // POST: api/caja/abrir-turno
        [HttpPost("abrir-turno")]
        public async Task<IActionResult> AbrirTurno([FromBody] AbrirTurnoRequest request)
        {
            if (request == null)
                return BadRequest("Datos de apertura de turno requeridos.");

            if (request.FondoInicial < 0)
                return BadRequest("El fondo inicial no puede ser negativo.");

            var cajero = string.IsNullOrWhiteSpace(request.Cajero) ? (User.Identity?.Name ?? "Caja Principal") : request.Cajero.Trim();
            var sucursal = string.IsNullOrWhiteSpace(request.Sucursal) ? "Sede Central" : request.Sucursal.Trim();

            var existente = await _context.TurnosCaja
                .FirstOrDefaultAsync(t => t.Estado == "Abierto" && t.Cajero.ToLower() == cajero.ToLower());

            if (existente != null)
            {
                return BadRequest($"Ya existe un turno abierto para {cajero} (Turno #{existente.Id}, iniciado {existente.FechaHoraApertura:hh:mm tt}). Debe cerrarlo antes de iniciar otro.");
            }

            var nuevoTurno = new TurnoCaja
            {
                Cajero = cajero,
                Sucursal = sucursal,
                FechaHoraApertura = DateTime.Now,
                FondoInicial = request.FondoInicial,
                Estado = "Abierto",
                ObservacionesApertura = request.Observaciones
            };

            _context.TurnosCaja.Add(nuevoTurno);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = $"Turno #{nuevoTurno.Id} abierto exitosamente con fondo inicial de RD$ {nuevoTurno.FondoInicial:N2}.",
                turno = nuevoTurno
            });
        }

        // POST: api/caja/cerrar-turno
        [HttpPost("cerrar-turno")]
        public async Task<IActionResult> CerrarTurno([FromBody] CerrarTurnoRequest request)
        {
            if (request == null)
                return BadRequest("Datos de cierre de turno requeridos.");

            TurnoCaja? turno = null;
            if (request.TurnoId > 0)
            {
                turno = await _context.TurnosCaja.FindAsync(request.TurnoId);
            }

            // Si no se encontró por ID directo (o vino un ID de contingencia negativo), buscar el turno actualmente abierto
            if (turno == null)
            {
                turno = await _context.TurnosCaja
                    .Where(t => t.Estado == "Abierto")
                    .OrderByDescending(t => t.FechaHoraApertura)
                    .FirstOrDefaultAsync();
            }

            if (turno == null)
            {
                // Si no hay ningún turno abierto, verificar si el más reciente ya se encuentra cerrado
                var ultimo = await _context.TurnosCaja
                    .OrderByDescending(t => t.FechaHoraCierre ?? t.FechaHoraApertura)
                    .FirstOrDefaultAsync();

                if (ultimo != null && ultimo.Estado == "Cerrado")
                {
                    return Ok(new
                    {
                        mensaje = $"El turno #{ultimo.Id} ya fue cerrado previamente.",
                        turno = ultimo,
                        estadoCuadre = ultimo.Diferencia == 0 ? "CUADRE EXACTO" : (ultimo.Diferencia > 0 ? $"SOBRANTE DE RD$ {ultimo.Diferencia:N2}" : $"FALTANTE DE RD$ {Math.Abs(ultimo.Diferencia):N2}"),
                        montoEntregadoBoveda = ultimo.MontoEntregadoBoveda,
                        fondoRemanenteCaja = ultimo.FondoRemanenteCaja,
                        destinoFondos = ultimo.DestinoFondos,
                        cantidadTransacciones = 0
                    });
                }

                return NotFound("No se encontró ningún turno activo o abierto para cerrar.");
            }

            if (turno.Estado == "Cerrado")
                return BadRequest($"El turno #{turno.Id} ya fue cerrado previamente el {turno.FechaHoraCierre:dd/MM/yyyy hh:mm tt}.");

            var transacciones = await _context.TransaccionesCaja
                .Where(t => (t.TurnoId == turno.Id || (t.TurnoId == null && t.FechaHora >= turno.FechaHoraApertura)) && !t.EsAnulada)
                .ToListAsync();

            foreach (var t in transacciones.Where(t => t.TurnoId == null))
            {
                t.TurnoId = turno.Id;
            }

            decimal totalEfectivo = transacciones.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.MontoTotal);
            decimal totalTarjeta = transacciones.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.MontoTotal);
            decimal totalTransferencia = transacciones.Where(t => t.MetodoPago == "Transferencia").Sum(t => t.MontoTotal);
            decimal totalVentas = transacciones.Sum(t => t.MontoTotal);
            decimal efectivoEsperado = turno.FondoInicial + totalEfectivo;
            decimal diferencia = request.EfectivoContado - efectivoEsperado;

            turno.Estado = "Cerrado";
            turno.FechaHoraCierre = DateTime.Now;
            turno.TotalVentasEfectivo = totalEfectivo;
            turno.TotalVentasTarjeta = totalTarjeta;
            turno.TotalVentasTransferencia = totalTransferencia;
            turno.TotalGeneralVentas = totalVentas;
            turno.EfectivoEsperado = efectivoEsperado;
            turno.EfectivoContado = request.EfectivoContado;
            turno.Diferencia = diferencia;
            turno.DesgloseDenominacionesJson = request.DesgloseDenominacionesJson;
            turno.ObservacionesCierre = !string.IsNullOrWhiteSpace(request.ObservacionesCierre) ? request.ObservacionesCierre : request.Observaciones;

            // Manejo de destino de fondos (Retiro a Bóveda vs Fondo en Gaveta)
            string destino = string.IsNullOrWhiteSpace(request.DestinoFondos) ? "RetiroTotalBoveda" : request.DestinoFondos;
            decimal remanenteCaja = 0;
            decimal entregadoBoveda = request.EfectivoContado;

            if (destino == "DejarFondoEnCaja")
            {
                remanenteCaja = request.FondoRemanenteCaja ?? Math.Min(turno.FondoInicial, request.EfectivoContado);
                entregadoBoveda = Math.Max(0, request.EfectivoContado - remanenteCaja);
            }
            else if (destino == "Personalizado")
            {
                entregadoBoveda = request.MontoEntregadoBoveda ?? request.EfectivoContado;
                remanenteCaja = request.FondoRemanenteCaja ?? Math.Max(0, request.EfectivoContado - entregadoBoveda);
            }

            turno.DestinoFondos = destino;
            turno.MontoEntregadoBoveda = entregadoBoveda;
            turno.FondoRemanenteCaja = remanenteCaja;

            await _context.SaveChangesAsync();

            string estadoCuadre = diferencia switch
            {
                0 => "CUADRE EXACTO",
                > 0 => $"SOBRANTE DE RD$ {diferencia:N2}",
                < 0 => $"FALTANTE DE RD$ {Math.Abs(diferencia):N2}"
            };

            return Ok(new
            {
                mensaje = $"Turno #{turno.Id} cerrado exitosamente. Resultado: {estadoCuadre}. Entregado a Bóveda: RD$ {turno.MontoEntregadoBoveda:N2}, Remanente en Gaveta: RD$ {turno.FondoRemanenteCaja:N2}.",
                turno,
                estadoCuadre,
                montoEntregadoBoveda = turno.MontoEntregadoBoveda,
                fondoRemanenteCaja = turno.FondoRemanenteCaja,
                destinoFondos = turno.DestinoFondos,
                cantidadTransacciones = transacciones.Count
            });
        }

        // ==========================================
        // 5. ANULACIÓN DE TRANSACCIONES (SUPERVISOR)
        // ==========================================

        // POST: api/caja/anular-transaccion/5
        [HttpPost("anular-transaccion/{id}")]
        public async Task<IActionResult> AnularTransaccion(int id, [FromBody] AnularTransaccionRequest request)
        {
            if (request == null)
                return BadRequest("Datos de anulación requeridos.");

            if (string.IsNullOrWhiteSpace(request.SupervisorUsuario) || string.IsNullOrWhiteSpace(request.SupervisorPassword))
                return BadRequest("Se requiere usuario y contraseña de supervisor/administrador para autorizar la anulación.");

            if (string.IsNullOrWhiteSpace(request.Motivo) || request.Motivo.Trim().Length < 5)
                return BadRequest("El motivo de anulación es obligatorio (mínimo 5 caracteres).");

            var supervisor = await _context.Usuarios
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.NombreUsuario == request.SupervisorUsuario.Trim());

            if (supervisor == null || supervisor.Role == null || supervisor.Role.Nombre != "Administrador")
            {
                return StatusCode(403, new { mensaje = "El usuario proporcionado no tiene rol de Administrador/Supervisor para autorizar anulaciones." });
            }

            var verif = _hasher.VerifyHashedPassword(supervisor, supervisor.PasswordHash, request.SupervisorPassword);
            if (verif == PasswordVerificationResult.Failed)
            {
                return StatusCode(401, new { mensaje = "Contraseña de supervisor incorrecta." });
            }

            var transaccion = await _context.TransaccionesCaja.FindAsync(id);
            if (transaccion == null)
                return NotFound("Transacción no encontrada.");

            if (transaccion.EsAnulada)
                return BadRequest($"La transacción {transaccion.NumeroRecibo} ya fue anulada previamente por {transaccion.AnuladoPor} el {transaccion.FechaHoraAnulacion:dd/MM/yyyy hh:mm tt}.");

            transaccion.EsAnulada = true;
            transaccion.MotivoAnulacion = request.Motivo.Trim();
            transaccion.FechaHoraAnulacion = DateTime.Now;
            transaccion.AnuladoPor = $"{supervisor.NombreCompleto} ({supervisor.NombreUsuario})";

            switch (transaccion.TipoTransaccion)
            {
                case "VentaProductos":
                    if (!string.IsNullOrWhiteSpace(transaccion.DetalleLineas))
                    {
                        foreach (var linea in transaccion.DetalleLineas.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var matchConCodigo = System.Text.RegularExpressions.Regex.Match(linea, @"(\d+)x\s+\[(.*?)\]");
                            if (matchConCodigo.Success && int.TryParse(matchConCodigo.Groups[1].Value, out var cantCod))
                            {
                                var codigo = matchConCodigo.Groups[2].Value.Trim();
                                var prod = await _context.ProductosArticulos.FirstOrDefaultAsync(p => p.Codigo == codigo);
                                if (prod != null) prod.StockDisponible += cantCod;
                            }
                            else
                            {
                                var matchSinCodigo = System.Text.RegularExpressions.Regex.Match(linea, @"(\d+)x\s+(.*?)(?:\s+\(RD\$|\s+@|\z)");
                                if (matchSinCodigo.Success && int.TryParse(matchSinCodigo.Groups[1].Value, out var cantNom))
                                {
                                    var nombre = matchSinCodigo.Groups[2].Value.Trim();
                                    var prod = await _context.ProductosArticulos.FirstOrDefaultAsync(p => p.Nombre == nombre);
                                    if (prod != null) prod.StockDisponible += cantNom;
                                }
                            }
                        }
                    }
                    else if (transaccion.ReferenciaId.HasValue && transaccion.ReferenciaId.Value > 0)
                    {
                        var prod = await _context.ProductosArticulos.FindAsync(transaccion.ReferenciaId.Value);
                        if (prod != null)
                        {
                            int cant = 1;
                            var match = System.Text.RegularExpressions.Regex.Match(transaccion.Concepto ?? "", @"Venta de (\d+)x");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out var c)) cant = c;
                            prod.StockDisponible += cant;
                        }
                    }

                    if (transaccion.ReferenciaId.HasValue && transaccion.ReferenciaId.Value > 0)
                    {
                        var orden = await _context.OrdenesPedidoCaja.FindAsync(transaccion.ReferenciaId.Value);
                        if (orden != null) orden.Estado = "Pendiente";
                    }
                    break;

                case "MasajeTacto":
                    if (transaccion.ReferenciaId.HasValue)
                    {
                        var sesion = await _context.SesionesMasaje.FindAsync(transaccion.ReferenciaId.Value);
                        if (sesion != null) sesion.EstaFacturadoEnCaja = false;
                    }
                    break;

                case "ConsultaOftalmologica":
                    if (transaccion.ReferenciaId.HasValue)
                    {
                        var consulta = await _context.ConsultasOftalmologicas.FindAsync(transaccion.ReferenciaId.Value);
                        if (consulta != null) consulta.EstaFacturadaEnCaja = false;
                    }
                    break;

                case "AbonoPrestamo":
                    if (transaccion.ReferenciaId.HasValue)
                    {
                        var prestamo = await _context.PrestamosEmprendimiento.FindAsync(transaccion.ReferenciaId.Value);
                        if (prestamo != null)
                        {
                            prestamo.SaldoPendiente += transaccion.MontoTotal;
                            if (prestamo.Estado == EstadoPrestamo.Pagado)
                                prestamo.Estado = EstadoPrestamo.Activo;
                        }
                    }
                    break;

                case "Donacion":
                    if (transaccion.ReferenciaId.HasValue)
                    {
                        var donacion = await _context.Donaciones.FindAsync(transaccion.ReferenciaId.Value);
                        if (donacion != null) donacion.EstaRegistradaEnCaja = false;
                    }
                    break;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = $"Transacción {transaccion.NumeroRecibo} anulada exitosamente por {transaccion.AnuladoPor}.",
                recibo = transaccion
            });
        }

        // POST: api/caja/sembrar-datos-masivos
        [HttpPost("sembrar-datos-masivos")]
        public IActionResult SembrarDatosMasivos()
        {
            var (agregados, detalle) = DbMassiveSeeder.SembrarDatosSiFaltan(_context);
            return Ok(new
            {
                mensaje = agregados > 0 
                    ? $"Se generaron {agregados} registros masivos reales exitosamente." 
                    : "La base de datos ya cuenta con más de 350 registros poblados.",
                nuevosRegistros = agregados,
                detalle,
                conteoActual = new
                {
                    productos = _context.ProductosArticulos.Count(),
                    beneficiarios = _context.Beneficiarios.Count(),
                    consultas = _context.ConsultasOftalmologicas.Count(),
                    masajes = _context.SesionesMasaje.Count(),
                    prestamos = _context.PrestamosEmprendimiento.Count(),
                    donaciones = _context.Donaciones.Count(),
                    rehabilitacion = _context.InscripcionesRehabilitacion.Count(),
                    voluntarios = _context.Voluntarios.Count(),
                    transacciones = _context.TransaccionesCaja.Count()
                }
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

    public class AbrirTurnoRequest
    {
        public string Cajero { get; set; } = "Caja Principal";
        public string Sucursal { get; set; } = "Sede Central";
        public decimal FondoInicial { get; set; }
        public string? Observaciones { get; set; }
    }

    public class CerrarTurnoRequest
    {
        public int TurnoId { get; set; }
        public decimal EfectivoContado { get; set; }
        public string? DesgloseDenominacionesJson { get; set; }
        public string? ObservacionesCierre { get; set; }
        public string? Observaciones { get; set; }
        public decimal? MontoEntregadoBoveda { get; set; }
        public decimal? FondoRemanenteCaja { get; set; }
        public string? DestinoFondos { get; set; }
    }

    public class AnularTransaccionRequest
    {
        public string SupervisorUsuario { get; set; } = string.Empty;
        public string SupervisorPassword { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }
}
