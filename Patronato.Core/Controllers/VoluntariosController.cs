using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;

namespace Patronato.Core.Controllers
{
    // Exclusivo de la Web (la Caja nunca llama este controlador): requiere JWT.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    public class VoluntariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VoluntariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/voluntarios (Listar todos los voluntarios activos y sus áreas de apoyo)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Voluntario>>> GetVoluntarios()
        {
            return await _context.Voluntarios
                .Where(v => v.Activo)
                .ToListAsync();
        }

        // 2. GET: api/voluntarios/5 (Ver datos de un voluntario específico)
        [HttpGet("{id}")]
        public async Task<ActionResult<Voluntario>> GetVoluntario(int id)
        {
            var voluntario = await _context.Voluntarios.FindAsync(id);
            if (voluntario == null)
                return NotFound("Voluntario no encontrado.");

            return voluntario;
        }

        // 3. POST: api/voluntarios (Inscribir a una persona solidaria como voluntaria)
        [HttpPost]
        public async Task<ActionResult<Voluntario>> PostVoluntario(Voluntario voluntario)
        {
            _context.Voluntarios.Add(voluntario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVoluntarios), new { id = voluntario.Id }, voluntario);
        }

        // 4. GET: api/voluntarios/donaciones (Listado de todas las donaciones para reportes de la Web)
        [HttpGet("donaciones")]
        public async Task<ActionResult<IEnumerable<Donacion>>> GetDonaciones()
        {
            return await _context.Donaciones
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();
        }

        // 5. PUT: api/voluntarios/5 (Editar los datos de un voluntario)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVoluntario(int id, Voluntario voluntario)
        {
            if (id != voluntario.Id)
                return BadRequest("El ID no coincide.");

            _context.Entry(voluntario).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 6. DELETE: api/voluntarios/5 (Dar de baja a un voluntario; no se elimina su historial)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVoluntario(int id)
        {
            var voluntario = await _context.Voluntarios.FindAsync(id);
            if (voluntario == null)
                return NotFound("Voluntario no encontrado.");

            voluntario.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Voluntario dado de baja." });
        }
    }
}
