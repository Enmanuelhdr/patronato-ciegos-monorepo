using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using Patronato.Core.Servicios;
using Microsoft.EntityFrameworkCore;

namespace Patronato.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwt;
        public UsuariosController(ApplicationDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }
        // 1. GET: api/usuarios (Listar todos los empleados y ver sus roles) — solo Admin
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }
        // 2. POST: api/usuarios (Crear un nuevo empleado en el sistema) — solo Admin
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.Id }, usuario);
        }
        // 3. POST: api/usuarios/login (Para que la Web autentique a quien entra; sin JWT
        // todavía porque es justo lo que entrega — la Caja también llega aquí por socket).
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromQuery] string nombreUsuario, [FromQuery] string password)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && u.PasswordHash == password);
            if (usuario == null)
                return Unauthorized("Usuario o contraseña incorrectos.");
            if (!usuario.Activo)
                return Forbid("Esta cuenta de usuario está desactivada.");
            return Ok(new
            {
                mensaje = "Inicio de sesión exitoso.",
                usuarioId = usuario.Id,
                nombre = usuario.NombreCompleto,
                rol = usuario.Rol.ToString(), // "Administrador", "Cajero", etc.
                sucursalId = usuario.SucursalId,
                token = _jwt.GenerarToken(usuario), // la Caja ignora este campo; solo la Web lo usa
            });
        }
        // 4. PUT: api/usuarios/5/cambiar-rol (Para que el admin le cambie permisos a un empleado) — solo Admin
        [HttpPut("{id}/cambiar-rol")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CambiarRol(int id, [FromQuery] RolUsuario nuevoRol)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");
            usuario.Rol = nuevoRol;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Rol actualizado a {nuevoRol} para {usuario.NombreCompleto}." });
        }

        // 5. PUT: api/usuarios/5 (Editar nombre, correo y sucursal de un empleado) — solo Admin
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            var existente = await _context.Usuarios.FindAsync(id);
            if (existente == null)
                return NotFound("Usuario no encontrado.");

            existente.NombreCompleto = usuario.NombreCompleto;
            existente.Correo = usuario.Correo;
            existente.SucursalId = usuario.SucursalId;
            // La contraseña y el nombre de usuario (login) no se tocan aquí para no romper
            // accidentalmente el acceso del empleado; eso se maneja aparte si hace falta.

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Datos actualizados para {existente.NombreCompleto}." });
        }

        // 6. DELETE: api/usuarios/5 (Desactivar la cuenta de un empleado) — solo Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            usuario.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Cuenta de {usuario.NombreCompleto} desactivada." });
        }
    }

}
