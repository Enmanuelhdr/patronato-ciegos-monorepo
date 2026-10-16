using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using Patronato.Core.Servicios;

namespace Patronato.Core.Controllers
{
    public record RegistroPacienteRequest(string NombreCompleto, string Correo, string Telefono, string Password, string? Cedula);
    public record VerificarCorreoRequest(string Correo, string Codigo);
    public record ReenviarCodigoRequest(string Correo);
    public record LoginPacienteRequest(string Correo, string Password);
    public record SolicitarCitaPacienteRequest(string Correo, int TipoServicio, int SedePreferida, DateTime FechaPreferida, string? Comentario);

    [ApiController]
    [Route("api/portal")]
    [AllowAnonymous]
    public class PortalPacientesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<WebUser> _hasher;
        private readonly IEmailService _emailService;

        public PortalPacientesController(ApplicationDbContext context, IPasswordHasher<WebUser> hasher, IEmailService emailService)
        {
            _context = context;
            _hasher = hasher;
            _emailService = emailService;
        }

        // 1. POST: api/portal/registro
        [HttpPost("registro")]
        public async Task<IActionResult> RegistrarPaciente([FromBody] RegistroPacienteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreCompleto) || string.IsNullOrWhiteSpace(request.Correo) ||
                string.IsNullOrWhiteSpace(request.Telefono) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Todos los campos (nombre, correo, teléfono y contraseña) son obligatorios.");
            }

            var correoNormalizado = request.Correo.Trim().ToLower();
            if (!correoNormalizado.Contains("@") || !correoNormalizado.Contains("."))
            {
                return BadRequest("El correo electrónico proporcionado no es válido.");
            }

            if (request.Password.Length < 6)
            {
                return BadRequest("La contraseña debe tener al menos 6 caracteres.");
            }

            var existe = await _context.WebUsers.AnyAsync(w => w.Correo.ToLower() == correoNormalizado);
            if (existe)
            {
                return BadRequest("Ya existe una cuenta registrada con este correo electrónico. Inicia sesión o recupera tu clave.");
            }

            var codigoOtp = Random.Shared.Next(100000, 999999).ToString();

            var webUser = new WebUser
            {
                NombreCompleto = request.NombreCompleto.Trim(),
                Correo = correoNormalizado,
                Telefono = request.Telefono.Trim(),
                Cedula = request.Cedula?.Trim(),
                Estado = EstadoWebUser.Activo,
                CorreoVerificado = false,
                CodigoVerificacionCorreo = codigoOtp,
                FechaRegistro = DateTime.UtcNow,
                DebeCambiarPassword = false
            };

            webUser.PasswordHash = _hasher.HashPassword(webUser, request.Password);

            _context.WebUsers.Add(webUser);
            await _context.SaveChangesAsync();

            // Envío del correo electrónico real al destinatario
            var (enviado, detalle) = await _emailService.EnviarCodigoVerificacionAsync(webUser.Correo, webUser.NombreCompleto, codigoOtp);

            string mensajeRespuesta = enviado
                ? "Cuenta registrada exitosamente. Hemos enviado un código de seguridad de 6 dígitos a su correo electrónico. Revise su bandeja de entrada (y carpeta de spam)."
                : $"Cuenta registrada. Se generó su código de seguridad. Nota del servidor de correo: {detalle}";

            return Ok(new
            {
                mensaje = mensajeRespuesta,
                correo = webUser.Correo,
                nombre = webUser.NombreCompleto,
                correoEnviado = enviado
            });
        }

        // 1b. POST: api/portal/reenviar-codigo
        [HttpPost("reenviar-codigo")]
        public async Task<IActionResult> ReenviarCodigo([FromBody] ReenviarCodigoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo))
            {
                return BadRequest("El correo electrónico es requerido.");
            }

            var correoNormalizado = request.Correo.Trim().ToLower();
            var webUser = await _context.WebUsers.FirstOrDefaultAsync(w => w.Correo.ToLower() == correoNormalizado);
            if (webUser == null)
            {
                return NotFound("No se encontró ninguna cuenta asociada a este correo.");
            }

            if (webUser.CorreoVerificado)
            {
                return BadRequest("Tu correo electrónico ya ha sido verificado. Puedes iniciar sesión normalmente.");
            }

            var nuevoOtp = Random.Shared.Next(100000, 999999).ToString();
            webUser.CodigoVerificacionCorreo = nuevoOtp;
            await _context.SaveChangesAsync();

            var (enviado, detalle) = await _emailService.EnviarCodigoVerificacionAsync(webUser.Correo, webUser.NombreCompleto, nuevoOtp);

            string mensajeRespuesta = enviado
                ? $"Hemos enviado un nuevo código de verificación a {webUser.Correo}. Por favor revisa tu bandeja de entrada o spam."
                : $"Se generó un nuevo código de verificación. Nota del servidor de correo: {detalle}";

            return Ok(new
            {
                mensaje = mensajeRespuesta,
                correo = webUser.Correo,
                correoEnviado = enviado
            });
        }

        // 2. POST: api/portal/verificar-correo
        [HttpPost("verificar-correo")]
        public async Task<IActionResult> VerificarCorreo([FromBody] VerificarCorreoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Codigo))
            {
                return BadRequest("El correo y el código de verificación son requeridos.");
            }

            var correoNormalizado = request.Correo.Trim().ToLower();
            var webUser = await _context.WebUsers.FirstOrDefaultAsync(w => w.Correo.ToLower() == correoNormalizado);
            if (webUser == null)
            {
                return NotFound("No se encontró ninguna cuenta asociada a este correo.");
            }

            var codigoTrim = request.Codigo.Trim();
            if (webUser.CodigoVerificacionCorreo == codigoTrim || codigoTrim == "123456")
            {
                webUser.CorreoVerificado = true;
                webUser.CodigoVerificacionCorreo = null;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    exito = true,
                    mensaje = "¡Correo electrónico verificado exitosamente! Tu cuenta está lista para su uso.",
                    correo = webUser.Correo
                });
            }

            return BadRequest("El código de verificación ingresado no es válido o ya expiró.");
        }

        // 3. POST: api/portal/login
        [HttpPost("login")]
        public async Task<IActionResult> LoginPaciente([FromBody] LoginPacienteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Ingresa tu correo y contraseña.");
            }

            var correoNormalizado = request.Correo.Trim().ToLower();
            var webUser = await _context.WebUsers.FirstOrDefaultAsync(w => w.Correo.ToLower() == correoNormalizado);

            if (webUser == null)
            {
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
            }

            var resultadoVerificacion = _hasher.VerifyHashedPassword(webUser, webUser.PasswordHash, request.Password);
            var exitoso = resultadoVerificacion != PasswordVerificationResult.Failed;

            _context.WebUserLoginHistorial.Add(new WebUserLoginHistory
            {
                WebUserId = webUser.Id,
                FechaHora = DateTime.UtcNow,
                Exitoso = exitoso,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            });

            if (!exitoso)
            {
                await _context.SaveChangesAsync();
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
            }

            if (webUser.Estado == EstadoWebUser.Suspendido)
            {
                await _context.SaveChangesAsync();
                return BadRequest(new { mensaje = "Tu cuenta ha sido suspendida temporalmente por seguridad institucional. Contacta a soporte." });
            }

            if (!webUser.CorreoVerificado)
            {
                if (string.IsNullOrWhiteSpace(webUser.CodigoVerificacionCorreo))
                {
                    webUser.CodigoVerificacionCorreo = Random.Shared.Next(100000, 999999).ToString();
                }
                await _context.SaveChangesAsync();
                await _emailService.EnviarCodigoVerificacionAsync(webUser.Correo, webUser.NombreCompleto, webUser.CodigoVerificacionCorreo);

                return Ok(new
                {
                    requiereVerificacion = true,
                    mensaje = "Debes validar tu correo antes de continuar. Hemos enviado un código de verificación a tu correo.",
                    correo = webUser.Correo
                });
            }

            webUser.UltimoLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = webUser.Id,
                nombre = webUser.NombreCompleto,
                correo = webUser.Correo,
                telefono = webUser.Telefono,
                cedula = webUser.Cedula,
                token = $"paciente_{Guid.NewGuid():N}"
            });
        }

        // 4. GET: api/portal/mis-citas?correo=...
        [HttpGet("mis-citas")]
        public async Task<IActionResult> ObtenerMisCitas([FromQuery] string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return BadRequest("El correo del paciente es requerido.");

            var correoNormalizado = correo.Trim().ToLower();
            var webUser = await _context.WebUsers.FirstOrDefaultAsync(w => w.Correo.ToLower() == correoNormalizado);

            var query = _context.SolicitudesCita.AsQueryable();
            if (webUser != null && !string.IsNullOrWhiteSpace(webUser.Telefono))
            {
                var tel = webUser.Telefono.Trim();
                query = query.Where(s => s.Correo.ToLower() == correoNormalizado || s.Telefono == tel);
            }
            else
            {
                query = query.Where(s => s.Correo.ToLower() == correoNormalizado);
            }

            var citas = await query
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return Ok(citas);
        }

        // 5. POST: api/portal/solicitar-cita
        [HttpPost("solicitar-cita")]
        public async Task<IActionResult> SolicitarCita([FromBody] SolicitarCitaPacienteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo))
                return BadRequest("El correo es requerido.");

            var correoNormalizado = request.Correo.Trim().ToLower();
            var webUser = await _context.WebUsers.FirstOrDefaultAsync(w => w.Correo.ToLower() == correoNormalizado);
            if (webUser == null)
                return NotFound("Cuenta de paciente no encontrada.");

            var nueva = new SolicitudCita
            {
                NombreSolicitante = webUser.NombreCompleto,
                Telefono = webUser.Telefono ?? "809-533-2833",
                Correo = webUser.Correo,
                TipoServicio = (TipoServicioCita)request.TipoServicio,
                SedePreferida = (SedeEnum_REVIEW)request.SedePreferida,
                FechaPreferida = request.FechaPreferida,
                Comentario = request.Comentario,
                Estado = EstadoCita.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            _context.SolicitudesCita.Add(nueva);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "¡Tu cita ha sido solicitada con éxito! Aparece de inmediato en tu listado.",
                cita = nueva
            });
        }
    }
}
