using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;

namespace Patronato.Core.Controllers
{
    // Exclusivo de la Web (la Caja nunca llama este controlador): requiere JWT de un
    // usuario con alguno de estos roles, igual que ya restringe la propia Web su menú.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,MedicoOftalmologo,Recepcionista")]
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

        // 5. PUT: api/consultas/5 (Editar los datos generales de una consulta ya registrada)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutConsulta(int id, ConsultaOftalmologica consulta)
        {
            if (id != consulta.Id)
                return BadRequest("El ID no coincide.");

            _context.Entry(consulta).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 6. DELETE: api/consultas/5 (Eliminar un registro erróneo) — solo Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConsulta(int id)
        {
            var consulta = await _context.ConsultasOftalmologicas.FindAsync(id);
            if (consulta == null)
                return NotFound("Consulta no encontrada.");

            _context.ConsultasOftalmologicas.Remove(consulta);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}