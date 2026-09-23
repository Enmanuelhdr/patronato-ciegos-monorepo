using Microsoft.AspNetCore.Mvc;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Patronato.Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }
        // 1. GET: api/usuarios (Listar todos los empleados y ver sus roles)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }
        // 2. POST: api/usuarios (Crear un nuevo empleado en el sistema)
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.Id }, usuario);
        }
        // 3. POST: api/usuarios/login (Para que la Web autentique a quien entra)
        [HttpPost("login")]
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
                sucursalId = usuario.SucursalId
            });
        }
        // 4. PUT: api/usuarios/5/cambiar-rol (Para que el admin le cambie permisos a un empleado)
        [HttpPut("{id}/cambiar-rol")]
        public async Task<IActionResult> CambiarRol(int id, [FromQuery] RolUsuario nuevoRol)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");
            usuario.Rol = nuevoRol;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Rol actualizado a {nuevoRol} para {usuario.NombreCompleto}." });
        }
    }

}
