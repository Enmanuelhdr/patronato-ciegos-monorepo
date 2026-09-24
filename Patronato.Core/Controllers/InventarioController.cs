using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;

namespace Patronato.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/inventario (Listar todos los productos y ver stock actual)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoArticulo>>> GetProductos()
        {
            return await _context.ProductosArticulos.ToListAsync();
        }

        // 2. GET: api/inventario/5 (Consultar un producto específico)
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoArticulo>> GetProducto(int id)
        {
            var producto = await _context.ProductosArticulos.FindAsync(id);
            if (producto == null)
                return NotFound("Producto no encontrado.");

            return producto;
        }

        // 3. POST: api/inventario (Dar de alta un nuevo producto en catálogo)
        [HttpPost]
        public async Task<ActionResult<ProductoArticulo>> PostProducto(ProductoArticulo producto)
        {
            _context.ProductosArticulos.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductos), new { id = producto.Id }, producto);
        }

        // 4. PUT: api/inventario/5/agregar-stock (Entrada de producción de suapers o compra de bastones)
        [HttpPut("{id}/agregar-stock")]
        public async Task<IActionResult> AgregarStock(int id, [FromQuery] int cantidad)
        {
            var producto = await _context.ProductosArticulos.FindAsync(id);
            if (producto == null)
                return NotFound("Producto no encontrado.");

            try
            {
                producto.AgregarStock(cantidad);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Stock actualizado exitosamente para {producto.Nombre}.",
                    nuevoStock = producto.StockDisponible
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
