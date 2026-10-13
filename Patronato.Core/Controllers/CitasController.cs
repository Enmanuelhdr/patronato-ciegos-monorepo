using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;

namespace Patronato.Core.Controllers
{
    // El personal que da seguimiento a estas solicitudes (Admin/Recepción y los
    // especialistas de cada programa); el POST público lo usa cualquier visitante
    // de la Web sin haber iniciado sesión (ver AllowAnonymous abajo).
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Recepcionista,MedicoOftalmologo,InstructorRehabilitacion")]
    public class CitasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CitasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/citas (Listar solicitudes de cita, más recientes primero) — Recepción/Admin/especialistas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SolicitudCita>>> GetCitas()
        {
            return await _context.SolicitudesCita
                .OrderByDescending(c => c.FechaSolicitud)
                .ToListAsync();
        }

        // 2. POST: api/citas (Cualquier persona, sin iniciar sesión, solicita que la contacten para agendar)
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<SolicitudCita>> PostCita(SolicitudCita solicitud)
        {
            if (string.IsNullOrWhiteSpace(solicitud.NombreSolicitante) || string.IsNullOrWhiteSpace(solicitud.Telefono))
                return BadRequest("El nombre y el teléfono son obligatorios.");

            // Una solicitud pública siempre nace pendiente; el estado lo decide el personal.
            solicitud.Id = 0;
            solicitud.Estado = EstadoCita.Pendiente;
            solicitud.FechaSolicitud = DateTime.UtcNow;

            _context.SolicitudesCita.Add(solicitud);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCitas), new { id = solicitud.Id }, solicitud);
        }

        // 3. PUT: api/citas/5/confirmar (Recepción/especialista confirma fecha y hora con el solicitante)
        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> Confirmar(int id)
        {
            var solicitud = await _context.SolicitudesCita.FindAsync(id);
            if (solicitud == null)
                return NotFound("Solicitud de cita no encontrada.");

            solicitud.Confirmar();
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Cita confirmada." });
        }

        // 4. PUT: api/citas/5/cancelar (No se pudo contactar al solicitante o este desistió)
        [HttpPut("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var solicitud = await _context.SolicitudesCita.FindAsync(id);
            if (solicitud == null)
                return NotFound("Solicitud de cita no encontrada.");

            solicitud.Cancelar();
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Cita cancelada." });
        }

        // 5. DELETE: api/citas/5 (Eliminar una solicitud duplicada o de prueba) — solo Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteCita(int id)
        {
            var solicitud = await _context.SolicitudesCita.FindAsync(id);
            if (solicitud == null)
                return NotFound("Solicitud de cita no encontrada.");

            _context.SolicitudesCita.Remove(solicitud);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
