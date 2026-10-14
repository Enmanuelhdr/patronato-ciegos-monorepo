using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;

namespace Patronato.Core.Controllers
{
    // Acciones de Recepción/Admin que generan trabajo para que la Caja cobre después
    // (agendar masaje, otorgar préstamo, cotizar una orden). Antes vivían en CajaController,
    // pero no son cobro de dinero — las usa exclusivamente la Web vía el reverse-proxy del
    // Gateway, nunca el protocolo de socket de la Caja (ver Patronato.Gateway/Services/CoreGatewayService.cs).
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public class RecepcionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RecepcionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/recepcion/masajes (Agendar nueva cita de masaje TACTO; Caja solo cobra)
        [HttpPost("masajes")]
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

        // POST: api/recepcion/prestamos (Otorgar nuevo crédito a beneficiario; Caja solo abona cuotas)
        [HttpPost("prestamos")]
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

        // POST: api/recepcion/ordenes (Cotización de insumos; Caja solo la cobra vía VENTA_CARRITO)
        [HttpPost("ordenes")]
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
                ItemsJson = System.Text.Json.JsonSerializer.Serialize(request.Items),
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
    }
}
