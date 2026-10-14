using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Patronato.Core.Entities;

namespace Patronato.Core.Servicios
{
    // Emite el JWT que la Web usa para autenticarse ante el Core en cada request
    // (la Caja sigue sin tocar esto: entra por socket y solo con X-Gateway-Key).
    public class JwtService
    {
        private readonly string _claveSecreta;
        private readonly string _issuer;
        private readonly int _expiracionMinutos;

        public JwtService(IConfiguration config)
        {
            _claveSecreta = config["Jwt:SecretKey"] ?? throw new InvalidOperationException("Falta Jwt:SecretKey en la configuración.");
            _issuer = config["Jwt:Issuer"] ?? "Patronato.Core";
            _expiracionMinutos = int.TryParse(config["Jwt:ExpiracionMinutos"], out var minutos) ? minutos : 480;
        }

        // rolNombre y permisos se resuelven aparte (desde CoreRole/CoreRolePermission)
        // porque este servicio no depende del DbContext.
        public string GenerarToken(Usuario usuario, string rolNombre, IEnumerable<string> permisos)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Role, rolNombre),
                new Claim("sucursalId", usuario.SucursalId.ToString()),
                // Marca el token como emitido para el dominio del Core: ningún token de
                // WebUser (dominio "web", cuando exista su login) debe poder pasar esta
                // validación aunque llegue firmado con la misma clave.
                new Claim("ctx", "core"),
                new Claim("debeCambiarPassword", usuario.DebeCambiarPassword.ToString()),
            };

            foreach (var permiso in permisos)
            {
                claims.Add(new Claim("permiso", permiso));
            }

            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_claveSecreta)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiracionMinutos),
                signingCredentials: credenciales);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
