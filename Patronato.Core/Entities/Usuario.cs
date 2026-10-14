using Patronato.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    // El usuario interno del Core (empleado): recepción, caja, especialistas clínicos
    // y ahora también los niveles administrativos/operativos/auditoría/logística.
    // Separado por completo de WebUser (cliente externo de la plataforma web).
    public class Usuario
    {
        public int Id { get; set; }
        // Datos personales y credenciales de acceso
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty; // Para iniciar sesión en la Web
        public string Correo { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Hash real (PasswordHasher<Usuario>), no texto plano

        // Rol y permisos: ahora vienen de CoreRole/CorePermission, no de un enum fijo.
        public int RoleId { get; set; }
        public CoreRole? Role { get; set; }

        // Sucursal donde trabaja este empleado
        public int SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }

        public EstadoCuentaCore EstadoCuenta { get; set; } = EstadoCuentaCore.Activo;
        public bool DebeCambiarPassword { get; set; } = false;
        public DateTime? UltimoLogin { get; set; }
        public int IntentosFallidos { get; set; } = 0;
        public DateTime? BloqueadoHasta { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
