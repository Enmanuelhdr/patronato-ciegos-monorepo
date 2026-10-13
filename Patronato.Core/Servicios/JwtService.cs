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

        public string GenerarToken(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
                new Claim("sucursalId", usuario.SucursalId.ToString()),
            };

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
