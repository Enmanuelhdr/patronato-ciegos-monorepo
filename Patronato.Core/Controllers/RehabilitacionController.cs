using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;

namespace Patronato.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RehabilitacionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RehabilitacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/rehabilitacion (Listar todas las inscripciones activas)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InscripcionRehabilitacion>>> GetInscripciones()
        {
            return await _context.InscripcionesRehabilitacion.ToListAsync();
        }

        // 2. GET: api/rehabilitacion/alumno/1 (Ver qué talleres ha cursado un paciente)
        [HttpGet("alumno/{beneficiarioId}")]
        public async Task<ActionResult<IEnumerable<InscripcionRehabilitacion>>> GetHistorialAlumno(int beneficiarioId)
        {
            return await _context.InscripcionesRehabilitacion
                .Where(r => r.BeneficiarioId == beneficiarioId)
                .ToListAsync();
        }

        // 3. POST: api/rehabilitacion (Inscribir a un paciente en un taller de Braille, bastón u oficio)
        [HttpPost]
        public async Task<ActionResult<InscripcionRehabilitacion>> PostInscripcion(InscripcionRehabilitacion inscripcion)
        {
            _context.InscripcionesRehabilitacion.Add(inscripcion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInscripciones), new { id = inscripcion.Id }, inscripcion);
        }

        // 4. PUT: api/rehabilitacion/5/graduar (Marcar al alumno como graduado del taller)
        [HttpPut("{id}/graduar")]
        public async Task<IActionResult> GraduarAlumno(int id)
        {
            var inscripcion = await _context.InscripcionesRehabilitacion.FindAsync(id);
            if (inscripcion == null)
                return NotFound("Inscripción no encontrada.");

            inscripcion.GraduarBeneficiario();
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Beneficiario graduado exitosamente de esta etapa formativa." });
        }
    }
}
