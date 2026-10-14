using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using Patronato.Core.Servicios;

namespace Patronato.Core.Controllers
{
    public record WebUserDto(int Id, string NombreCompleto, string Correo, string? Telefono, string Estado, DateTime FechaRegistro, DateTime? UltimoLogin, bool DebeCambiarPassword);
    public record WebUserLoginHistorialDto(int Id, DateTime FechaHora, bool Exitoso, string? Ip, string? UserAgent);

    // Auditoría y administración, desde el Core, de las cuentas de la plataforma Web
    // pública (clientes/beneficiarios externos). El registro/login público real todavía
    // no existe (ver WebUser.cs) — esta pantalla gestiona la tabla y queda lista para
    // cuando ese portal de autoservicio se construya.
    [ApiController]
    [Route("api/[controller]")]
    public class WebUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<WebUser> _hasher;

        public WebUsersController(ApplicationDbContext context, IPasswordHasher<WebUser> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        private static WebUserDto ADto(WebUser w) =>
            new(w.Id, w.NombreCompleto, w.Correo, w.Telefono, w.Estado.ToString(), w.FechaRegistro, w.UltimoLogin, w.DebeCambiarPassword);

        // 1. GET: api/webusers (filtros: estado, búsqueda por nombre/correo)
        [HttpGet]
        [RequierePermiso(PermisosCore.WebUsuariosVer)]
        public async Task<ActionResult<IEnumerable<WebUserDto>>> GetWebUsers([FromQuery] EstadoWebUser? estado, [FromQuery] string? busqueda)
        {
            var query = _context.WebUsers.AsQueryable();
            if (estado.HasValue) query = query.Where(w => w.Estado == estado.Value);
            if (!string.IsNullOrWhiteSpace(busqueda))
                query = query.Where(w => w.NombreCompleto.Contains(busqueda) || w.Correo.Contains(busqueda));

            var webUsers = await query.OrderBy(w => w.NombreCompleto).ToListAsync();
            return Ok(webUsers.Select(ADto));
        }

        // 2. GET: api/webusers/5/historial-accesos
        [HttpGet("{id}/historial-accesos")]
        [RequierePermiso(PermisosCore.WebUsuariosVer)]
        public async Task<ActionResult<IEnumerable<WebUserLoginHistorialDto>>> GetHistorial(int id)
        {
            var historial = await _context.WebUserLoginHistorial
                .Where(h => h.WebUserId == id)
                .OrderByDescending(h => h.FechaHora)
                .Select(h => new WebUserLoginHistorialDto(h.Id, h.FechaHora, h.Exitoso, h.Ip, h.UserAgent))
                .ToListAsync();

            return Ok(historial);
        }

        // 3. PUT: api/webusers/5/habilitar
        [HttpPut("{id}/habilitar")]
        [RequierePermiso(PermisosCore.WebUsuariosGestionar)]
        public async Task<IActionResult> Habilitar(int id)
        {
            var webUser = await _context.WebUsers.FindAsync(id);
            if (webUser == null) return NotFound("Usuario web no encontrado.");

            webUser.Estado = EstadoWebUser.Activo;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Cuenta de {webUser.NombreCompleto} habilitada." });
        }

        // 4. PUT: api/webusers/5/deshabilitar
        [HttpPut("{id}/deshabilitar")]
        [RequierePermiso(PermisosCore.WebUsuariosGestionar)]
        public async Task<IActionResult> Deshabilitar(int id)
        {
            var webUser = await _context.WebUsers.FindAsync(id);
            if (webUser == null) return NotFound("Usuario web no encontrado.");

            webUser.Estado = EstadoWebUser.Suspendido;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Cuenta de {webUser.NombreCompleto} deshabilitada." });
        }

        // 5. POST: api/webusers/5/resetear-password (genera una clave temporal y la
        // devuelve para que el administrador se la comunique al cliente)
        [HttpPost("{id}/resetear-password")]
        [RequierePermiso(PermisosCore.WebUsuariosGestionar)]
        public async Task<IActionResult> ResetearPassword(int id)
        {
            var webUser = await _context.WebUsers.FindAsync(id);
            if (webUser == null) return NotFound("Usuario web no encontrado.");

            var passwordTemporal = $"Temp-{Guid.NewGuid():N}"[..13];
            webUser.PasswordHash = _hasher.HashPassword(webUser, passwordTemporal);
            webUser.DebeCambiarPassword = true;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Clave temporal generada para {webUser.NombreCompleto}.", passwordTemporal });
        }

        // 6. POST: api/webusers/5/forzar-cierre-sesion
        [HttpPost("{id}/forzar-cierre-sesion")]
        [RequierePermiso(PermisosCore.WebUsuariosGestionar)]
        public async Task<IActionResult> ForzarCierreSesion(int id)
        {
            var webUser = await _context.WebUsers.FindAsync(id);
            if (webUser == null) return NotFound("Usuario web no encontrado.");

            var sesionesActivas = await _context.WebUserSessions
                .Where(s => s.WebUserId == id && s.RevocadoEn == null)
                .ToListAsync();

            foreach (var sesion in sesionesActivas)
                sesion.RevocadoEn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Se cerraron {sesionesActivas.Count} sesión(es) activa(s) de {webUser.NombreCompleto}." });
        }
    }
}
