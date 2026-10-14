using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using Patronato.Core.Servicios;
using Microsoft.EntityFrameworkCore;

namespace Patronato.Core.Controllers
{
    public record UsuarioDto(
        int Id, string NombreCompleto, string NombreUsuario, string Correo,
        int RoleId, string RoleNombre, int SucursalId, string EstadoCuenta,
        bool DebeCambiarPassword, DateTime? UltimoLogin, DateTime FechaCreacion);

    public record CrearUsuarioRequest(string NombreCompleto, string NombreUsuario, string Correo, string PasswordTemporal, int RoleId, int SucursalId);
    public record EditarUsuarioRequest(string NombreCompleto, string Correo, int SucursalId);
    public record CambiarPasswordRequest(string PasswordActual, string PasswordNueva);

    // NOTA: el nombre de esta clase y su ruta (api/Usuarios) no se tocan: Patronato.Gateway
    // llama literalmente a "api/Usuarios/login" para que Patronato.Caja pueda autenticar
    // cajeros/administradores en la terminal de cobro (ver CoreGatewayService.LoginAsync).
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwt;
        private readonly IPasswordHasher<Usuario> _hasher;

        public UsuariosController(ApplicationDbContext context, JwtService jwt, IPasswordHasher<Usuario> hasher)
        {
            _context = context;
            _jwt = jwt;
            _hasher = hasher;
        }

        private bool ActorTienePermiso(string codigo) =>
            User.FindAll("permiso").Any(c => c.Value == codigo);

        private static UsuarioDto AUsuarioDto(Usuario u) => new(
            u.Id, u.NombreCompleto, u.NombreUsuario, u.Correo,
            u.RoleId, u.Role?.Nombre ?? "", u.SucursalId, u.EstadoCuenta.ToString(),
            u.DebeCambiarPassword, u.UltimoLogin, u.FechaCreacion);

        // 1. GET: api/usuarios (Listar todos los empleados y ver sus roles)
        [HttpGet]
        [RequierePermiso(PermisosCore.CoreUsersVer)]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetUsuarios()
        {
            var usuarios = await _context.Usuarios.Include(u => u.Role).ToListAsync();
            return Ok(usuarios.Select(AUsuarioDto));
        }

        // 2. POST: api/usuarios (Crear un nuevo empleado en el sistema, con clave temporal
        // que lo obliga a cambiarla en su primer inicio de sesión — ver /cambiar-password).
        [HttpPost]
        [RequierePermiso(PermisosCore.CoreUsersCrear)]
        public async Task<ActionResult<UsuarioDto>> PostUsuario(CrearUsuarioRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.PasswordTemporal))
                return BadRequest("El nombre de usuario y la contraseña temporal son obligatorios.");

            if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == request.NombreUsuario))
                return BadRequest("Ya existe un empleado con ese nombre de usuario.");

            var rol = await _context.CoreRoles.FindAsync(request.RoleId);
            if (rol == null)
                return BadRequest("El rol indicado no existe.");

            if (!await PuedeAsignarRolAsync(request.RoleId))
                return StatusCode(403, new { mensaje = $"No tiene permiso para asignar el rol '{rol.Nombre}'." });

            var usuario = new Usuario
            {
                NombreCompleto = request.NombreCompleto,
                NombreUsuario = request.NombreUsuario,
                Correo = request.Correo,
                RoleId = request.RoleId,
                SucursalId = request.SucursalId,
                DebeCambiarPassword = true,
                EstadoCuenta = EstadoCuentaCore.Activo,
            };
            usuario.PasswordHash = _hasher.HashPassword(usuario, request.PasswordTemporal);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            usuario.Role = rol;
            return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.Id }, AUsuarioDto(usuario));
        }

        // Evita que un AdminOperativo (o cualquier rol sin core-users.crear-superadmin)
        // cree o ascienda a otro empleado al rol Administrador (SuperAdmin/IT).
        private async Task<bool> PuedeAsignarRolAsync(int roleId)
        {
            var requiereSuperAdmin = await _context.CoreRolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.Permission!.Codigo == PermisosCore.CoreUsersCrearSuperAdmin);

            return !requiereSuperAdmin || ActorTienePermiso(PermisosCore.CoreUsersCrearSuperAdmin);
        }

        // 3. POST: api/usuarios/login (la Web y la Caja -vía Gateway- autentican aquí).
        // Ruta, verbo y forma de la respuesta NO cambian: solo se agregan "permisos" y
        // "debeCambiarPassword" a los campos que ya existían.
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromQuery] string nombreUsuario, [FromQuery] string password)
        {
            var resultado = await AutenticacionCoreService.IntentarLogin(_context, _hasher, nombreUsuario, password);
            if (!resultado.Exito)
                return StatusCode(resultado.StatusCodeError, new { mensaje = resultado.Error });

            var usuario = resultado.Usuario!;

            return Ok(new
            {
                mensaje = "Inicio de sesión exitoso.",
                usuarioId = usuario.Id,
                nombre = usuario.NombreCompleto,
                rol = resultado.RolNombre, // "Administrador", "Cajero", etc. — la Caja compara este string literalmente
                sucursalId = usuario.SucursalId,
                token = _jwt.GenerarToken(usuario, resultado.RolNombre, resultado.Permisos), // la Caja ignora este campo; solo la Web lo usa
                permisos = resultado.Permisos,
                debeCambiarPassword = usuario.DebeCambiarPassword,
            });
        }

        // 4. POST: api/usuarios/cambiar-password (el propio empleado autenticado cambia su
        // clave — se usa tanto para el cambio voluntario como para el forzado al primer login).
        [HttpPost("cambiar-password")]
        [RequierePermiso]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest request)
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var usuarioId))
                return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return NotFound();

            if (_hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.PasswordActual) == PasswordVerificationResult.Failed)
                return BadRequest("La contraseña actual no es correcta.");

            if (string.IsNullOrWhiteSpace(request.PasswordNueva) || request.PasswordNueva.Length < 6)
                return BadRequest("La nueva contraseña debe tener al menos 6 caracteres.");

            usuario.PasswordHash = _hasher.HashPassword(usuario, request.PasswordNueva);
            usuario.DebeCambiarPassword = false;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Contraseña actualizada." });
        }

        // 5. PUT: api/usuarios/5/cambiar-rol (Para que el admin le cambie permisos a un empleado)
        [HttpPut("{id}/cambiar-rol")]
        [RequierePermiso(PermisosCore.CoreUsersEditar)]
        public async Task<IActionResult> CambiarRol(int id, [FromQuery] int nuevoRolId)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            var rol = await _context.CoreRoles.FindAsync(nuevoRolId);
            if (rol == null)
                return BadRequest("El rol indicado no existe.");

            if (!await PuedeAsignarRolAsync(nuevoRolId))
                return StatusCode(403, new { mensaje = $"No tiene permiso para asignar el rol '{rol.Nombre}'." });

            usuario.RoleId = nuevoRolId;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Rol actualizado a {rol.Nombre} para {usuario.NombreCompleto}." });
        }

        // 6. PUT: api/usuarios/5 (Editar nombre, correo y sucursal de un empleado)
        [HttpPut("{id}")]
        [RequierePermiso(PermisosCore.CoreUsersEditar)]
        public async Task<IActionResult> PutUsuario(int id, EditarUsuarioRequest request)
        {
            var existente = await _context.Usuarios.FindAsync(id);
            if (existente == null)
                return NotFound("Usuario no encontrado.");

            existente.NombreCompleto = request.NombreCompleto;
            existente.Correo = request.Correo;
            existente.SucursalId = request.SucursalId;
            // La contraseña y el nombre de usuario (login) no se tocan aquí para no romper
            // accidentalmente el acceso del empleado; eso se maneja aparte si hace falta.

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Datos actualizados para {existente.NombreCompleto}." });
        }

        // 7. PUT: api/usuarios/5/estado (Activar, suspender o revocar el acceso de un empleado)
        [HttpPut("{id}/estado")]
        [RequierePermiso(PermisosCore.CoreUsersEstado)]
        public async Task<IActionResult> CambiarEstado(int id, [FromQuery] EstadoCuentaCore nuevoEstado)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (nuevoEstado != EstadoCuentaCore.Activo && idClaim == usuario.Id.ToString())
                return BadRequest("No puede suspender o revocar su propia cuenta.");

            usuario.EstadoCuenta = nuevoEstado;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Cuenta de {usuario.NombreCompleto} ahora está {nuevoEstado}." });
        }
    }

}
