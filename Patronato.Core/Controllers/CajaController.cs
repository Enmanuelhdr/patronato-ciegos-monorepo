using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;

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

        // 1. POST: api/caja/cobrar-masaje/1 (Cobra una sesión de masajes TACTO)
        [HttpPost("cobrar-masaje/{id}")]
        public async Task<IActionResult> CobrarMasaje(int id)
        {
            var sesion = await _context.SesionesMasaje.FindAsync(id);
            if (sesion == null)
                return NotFound("Sesión de masaje no encontrada.");

            if (sesion.EstaFacturadoEnCaja)
                return BadRequest("Esta sesión ya fue cobrada previamente.");

            sesion.MarcarComoCobrado();
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Masaje cobrado con éxito en Caja.", monto = sesion.PrecioTarifa });
        }

        // 2. POST: api/caja/abonar-prestamo (Recibe el pago de cuota de un colmado/paletera)
        [HttpPost("abonar-prestamo")]
        public async Task<IActionResult> AbonarPrestamo([FromQuery] int prestamoId, [FromQuery] decimal monto)
        {
            var prestamo = await _context.PrestamosEmprendimiento.FindAsync(prestamoId);
            if (prestamo == null)
                return NotFound("Préstamo no encontrado.");

            try
            {
                prestamo.RegistrarAbono(monto);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Abono aplicado con éxito.", nuevoSaldo = prestamo.SaldoPendiente, estado = prestamo.Estado.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 3. POST: api/caja/vender-producto (Vende suapers, bastones o químicos y descuenta el stock)
        [HttpPost("vender-producto")]
        public async Task<IActionResult> VenderProducto([FromQuery] int productoId, [FromQuery] int cantidad)
        {
            var producto = await _context.ProductosArticulos.FindAsync(productoId);
            if (producto == null)
                return NotFound("Artículo no encontrado en el inventario.");

            try
            {
                producto.DescontarStock(cantidad);
                decimal totalVenta = producto.PrecioVenta * cantidad;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Venta realizada de {cantidad} unidad(es) de {producto.Nombre}.",
                    totalCobrado = totalVenta,
                    stockRestante = producto.StockDisponible
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 4. POST: api/caja/donacion (Registra una donación recibida en ventanilla)
        [HttpPost("donacion")]
        public async Task<IActionResult> RegistrarDonacion(Donacion donacion)
        {
            donacion.EstaRegistradaEnCaja = true;
            _context.Donaciones.Add(donacion);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Donación registrada con éxito en Caja.", reciboId = donacion.Id });
        }
    }
}
