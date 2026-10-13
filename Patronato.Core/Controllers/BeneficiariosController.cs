using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;

namespace Patronato.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BeneficiariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Inyectamos la base de datos
        public BeneficiariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/beneficiarios (Para que la Web liste todos los pacientes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Beneficiario>>> GetBeneficiarios()
        {
            return await _context.Beneficiarios.ToListAsync();
        }

        // 2. GET: api/beneficiarios/5 (Para ver la ficha de un paciente específico)
        [HttpGet("{id}")]
        public async Task<ActionResult<Beneficiario>> GetBeneficiario(int id)
        {
            var beneficiario = await _context.Beneficiarios.FindAsync(id);

            if (beneficiario == null)
                return NotFound("Beneficiario no encontrado en el sistema.");

            return beneficiario;
        }

        // 3. POST: api/beneficiarios (Para que la Web registre un nuevo paciente)
        // Los GET se quedan públicos a propósito: la landing pública los usa sin login.
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Beneficiario>> PostBeneficiario(Beneficiario beneficiario)
        {
            _context.Beneficiarios.Add(beneficiario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBeneficiario), new { id = beneficiario.Id }, beneficiario);
        }

        // 4. PUT: api/beneficiarios/5 (Editar los datos de un paciente)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutBeneficiario(int id, Beneficiario beneficiario)
        {
            if (id != beneficiario.Id)
                return BadRequest("El ID no coincide.");

            _context.Entry(beneficiario).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. DELETE: api/beneficiarios/5 (Desactivar un beneficiario; no se elimina su historial) — solo Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteBeneficiario(int id)
        {
            var beneficiario = await _context.Beneficiarios.FindAsync(id);
            if (beneficiario == null)
                return NotFound("Beneficiario no encontrado en el sistema.");

            beneficiario.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Beneficiario desactivado." });
        }
    }
}