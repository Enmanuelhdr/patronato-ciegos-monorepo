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
            if (string.IsNullOrWhiteSpace(solicitud.NombreSolicitante) || string.IsNullOrWhiteSpace(solicitud.Telefono) || string.IsNullOrWhiteSpace(solicitud.Correo))
                return BadRequest("El nombre, teléfono y correo electrónico institucional/personal son obligatorios para procesar la cita.");

            if (!solicitud.Correo.Contains("@") || !solicitud.Correo.Contains("."))
                return BadRequest("El correo electrónico proporcionado no tiene un formato válido (ejemplo: usuario@correo.com).");

            // Consulta Oftalmológica y Rehabilitación exigen un Beneficiario registrado
            // (cédula + condición visual) que una solicitud pública y anónima no puede
            // establecer. Esas dos se agendan directo desde Recepción, no desde aquí.
            if (solicitud.TipoServicio == TipoServicioCita.ConsultaOftalmologica || solicitud.TipoServicio == TipoServicioCita.Rehabilitacion)
                return BadRequest("Este servicio requiere estar registrado como Beneficiario. Visita o llama a una sede para agendarlo con Recepción.");

            // Una solicitud pública siempre nace pendiente; el estado lo decide el personal.
            solicitud.Id = 0;
            solicitud.Estado = EstadoCita.Pendiente;
            solicitud.FechaSolicitud = DateTime.UtcNow;

            _context.SolicitudesCita.Add(solicitud);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCitas), new { id = solicitud.Id }, solicitud);
        }

        // 2b. GET: api/citas/consultar?termino=809... (Pública: el solicitante consulta sus citas por teléfono o código SOL-XXXXXX)
        [HttpGet("consultar")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SolicitudCita>>> ConsultarSolicitudes([FromQuery] string? termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return BadRequest("Ingresa tu teléfono, correo o código de solicitud.");

            var limpio = termino.Trim();
            int idBuscado = 0;
            if (limpio.StartsWith("SOL-", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(limpio.Substring(4), out idBuscado);
            }
            else
            {
                int.TryParse(limpio, out idBuscado);
            }

            var soloNumeros = new string(limpio.Where(char.IsDigit).ToArray());

            var query = _context.SolicitudesCita.AsQueryable();

            if (idBuscado > 0)
            {
                query = query.Where(c => c.Id == idBuscado);
            }
            else if (!string.IsNullOrEmpty(soloNumeros) && soloNumeros.Length >= 4)
            {
                query = query.Where(c => c.Telefono.Contains(soloNumeros) || c.Telefono.Contains(limpio));
            }
            else
            {
                query = query.Where(c => c.Correo.ToLower() == limpio.ToLower() || c.NombreSolicitante.ToLower().Contains(limpio.ToLower()));
            }

            var citas = await query
                .OrderByDescending(c => c.FechaSolicitud)
                .Take(20)
                .ToListAsync();

            return Ok(citas);
        }

        // 3. PUT: api/citas/5/confirmar (Recepción/especialista confirma fecha y hora con el solicitante)
        // Si el servicio pedido es Masaje TACTO, además crea la SesionMasajeTacto (sin facturar)
        // para que quede visible y cobrable en la Caja cuando el cliente llegue en persona —
        // el mismo mecanismo que ya usa Recepción al agendar un masaje desde /panel/masajes.
        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> Confirmar(int id)
        {
            var solicitud = await _context.SolicitudesCita.FindAsync(id);
            if (solicitud == null)
                return NotFound("Solicitud de cita no encontrada.");

            solicitud.Confirmar();

            string mensaje = "Cita confirmada.";
            if (solicitud.TipoServicio == TipoServicioCita.MasajeTacto)
            {
                var sesion = new SesionMasajeTacto
                {
                    NombreCliente = solicitud.NombreSolicitante,
                    TelefonoCliente = solicitud.Telefono,
                    NombreMasajista = "Terapeuta Invidente Asignado",
                    Sede = solicitud.SedePreferida,
                    FechaHora = solicitud.FechaPreferida,
                    DuracionMinutos = 45,
                    PrecioTarifa = 500m,
                    EstaFacturadoEnCaja = false
                };

                _context.SesionesMasaje.Add(sesion);
                mensaje = "Cita confirmada. Quedó pendiente de cobro en Caja (Masajes TACTO).";
            }
            else if (solicitud.TipoServicio == TipoServicioCita.ConsultaOftalmologica)
            {
                mensaje = "Cita confirmada. Verifica o registra al solicitante como Beneficiario y agenda la consulta desde Consultas Oftalmológicas.";
            }
            else if (solicitud.TipoServicio == TipoServicioCita.Rehabilitacion)
            {
                mensaje = "Cita confirmada. Verifica o registra al solicitante como Beneficiario y crea la inscripción desde Rehabilitación.";
            }
            else
            {
                mensaje = "Cita confirmada. Da seguimiento según el servicio solicitado (no tiene un módulo dedicado en el sistema).";
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje });
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
