
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;

namespace Patronato.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SucursalesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public SucursalesController(ApplicationDbContext context)
        {
            _context = context;
        }
        // 1. GET: api/sucursales (Listar todas las sucursales)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sucursal>>> GetSucursales()
        {
            return await _context.Sucursales.ToListAsync();
        }
        // 2. GET: api/sucursales/1 (Ver detalle de una sucursal)
        [HttpGet("{id}")]
        public async Task<ActionResult<Sucursal>> GetSucursal(int id)
        {
            var sucursal = await _context.Sucursales.FindAsync(id);
            if (sucursal == null)
                return NotFound("Sucursal no encontrada.");
            return sucursal;
        }
        // 3. POST: api/sucursales (Crear una nueva sucursal)
        [HttpPost]
        public async Task<ActionResult<Sucursal>> PostSucursal(Sucursal sucursal)
        {
            _context.Sucursales.Add(sucursal);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSucursal), new { id = sucursal.Id }, sucursal);
        }
        // 4. PUT: api/sucursales/1 (Editar dirección, teléfono o encargado)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSucursal(int id, Sucursal sucursal)
        {
            if (id != sucursal.Id)
                return BadRequest("El ID no coincide.");
            _context.Entry(sucursal).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
