using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;

namespace Patronato.Core.Servicios
{
    public class ResultadoLogin
    {
        public bool Exito { get; init; }
        public string? Error { get; init; }
        public int StatusCodeError { get; init; } = 401;
        public Usuario? Usuario { get; init; }
        public string RolNombre { get; init; } = string.Empty;
        public List<string> Permisos { get; init; } = new();
    }

    // Login de un Usuario (empleado del Core) compartido por api/Usuarios/login (lo usan
    // la Web y, vía Gateway, la Caja) y por monitor/login (panel de soporte, fuera del
    // Gateway): misma tabla, mismo hash, mismo bloqueo por intentos fallidos — para que
    // esa lógica no viva duplicada (y posiblemente diverja) en dos sitios.
    public static class AutenticacionCoreService
    {
        private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromSeconds(30);
        private const int IntentosMaximos = 5;

        public static async Task<ResultadoLogin> IntentarLogin(ApplicationDbContext context, IPasswordHasher<Usuario> hasher, string nombreUsuario, string password)
        {
            var usuario = await context.Usuarios.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
            if (usuario == null)
                return new ResultadoLogin { Exito = false, Error = "Usuario o contraseña incorrectos." };

            if (usuario.EstadoCuenta != EstadoCuentaCore.Activo)
                return new ResultadoLogin { Exito = false, Error = "Esta cuenta de usuario está desactivada.", StatusCodeError = 403 };

            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
            {
                var segundosRestantes = Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.UtcNow).TotalSeconds);
                return new ResultadoLogin
                {
                    Exito = false,
                    Error = $"Cuenta bloqueada temporalmente por intentos fallidos. Intente de nuevo en {segundosRestantes} segundo(s).",
                    StatusCodeError = 403,
                };
            }

            var resultado = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
            if (resultado == PasswordVerificationResult.Failed)
            {
                usuario.IntentosFallidos++;
                if (usuario.IntentosFallidos >= IntentosMaximos)
                {
                    usuario.BloqueadoHasta = DateTime.UtcNow.Add(DuracionBloqueo);
                    usuario.IntentosFallidos = 0;
                }
                await context.SaveChangesAsync();
                return new ResultadoLogin { Exito = false, Error = "Usuario o contraseña incorrectos." };
            }

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
                usuario.PasswordHash = hasher.HashPassword(usuario, password);

            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            usuario.UltimoLogin = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var permisos = await context.CoreRolePermissions
                .Where(rp => rp.RoleId == usuario.RoleId)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission!.Codigo)
                .ToListAsync();

            return new ResultadoLogin { Exito = true, Usuario = usuario, RolNombre = usuario.Role!.Nombre, Permisos = permisos };
        }
    }
}
