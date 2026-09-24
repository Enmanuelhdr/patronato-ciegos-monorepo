using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;

namespace Patronato.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConsultasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/consultas (Listar todas las consultas médicas)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConsultaOftalmologica>>> GetConsultas()
        {
            return await _context.ConsultasOftalmologicas.ToListAsync();
        }

        // 2. GET: api/consultas/paciente/1 (Ver el historial médico de un paciente específico)
        [HttpGet("paciente/{pacienteId}")]
        public async Task<ActionResult<IEnumerable<ConsultaOftalmologica>>> GetConsultasPorPaciente(int pacienteId)
        {
            return await _context.ConsultasOftalmologicas
                .Where(c => c.BeneficiarioId == pacienteId)
                .ToListAsync();
        }

        // 3. POST: api/consultas (Agendar/registrar una nueva consulta oftalmológica)
        [HttpPost]
        public async Task<ActionResult<ConsultaOftalmologica>> PostConsulta(ConsultaOftalmologica consulta)
        {
            _context.ConsultasOftalmologicas.Add(consulta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConsultas), new { id = consulta.Id }, consulta);
        }

        // 4. PUT: api/consultas/5/diagnostico (El médico asienta el diagnóstico y tratamiento)
        [HttpPut("{id}/diagnostico")]
        public async Task<IActionResult> RegistrarDiagnostico(int id, [FromQuery] string diagnostico, [FromQuery] string tratamiento)
        {
            var consulta = await _context.ConsultasOftalmologicas.FindAsync(id);
            if (consulta == null)
                return NotFound("Consulta no encontrada.");

            try
            {
                consulta.RegistrarDiagnostico(diagnostico, tratamiento);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Diagnóstico registrado exitosamente por el especialista." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}