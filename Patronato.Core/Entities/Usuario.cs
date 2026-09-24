using Patronato.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        // Datos personales y credenciales de acceso
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty; // Para iniciar sesión en la Web
        public string Correo { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Contraseña encriptada por seguridad
        // Permisos y perfil del empleado
        public RolUsuario Rol { get; set; } = RolUsuario.Recepcionista;
        // Sucursal donde trabaja este empleado
        public int SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
