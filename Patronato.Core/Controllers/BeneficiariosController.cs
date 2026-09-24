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
        [HttpPost]
        public async Task<ActionResult<Beneficiario>> PostBeneficiario(Beneficiario beneficiario)
        {
            _context.Beneficiarios.Add(beneficiario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBeneficiario), new { id = beneficiario.Id }, beneficiario);
        }
    }
}