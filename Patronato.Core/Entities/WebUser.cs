using System;
using Patronato.Core.Enums;

namespace Patronato.Core.Entities
{
    // Cuenta de un usuario de la plataforma Web pública (cliente/beneficiario externo).
    // Completamente separada de Usuario (empleado del Core): tabla propia, login propio
    // (a futuro), sin ningún rol/permiso del Core. Hoy el registro/login público real
    // todavía no existe (la Web solo recibe solicitudes anónimas de cita) — esta tabla
    // y su administración desde el Core quedan listas para cuando se construya ese
    // portal de autoservicio.
    public class WebUser
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Telefono { get; set; }

        public EstadoWebUser Estado { get; set; } = EstadoWebUser.Activo;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public DateTime? UltimoLogin { get; set; }
        public bool DebeCambiarPassword { get; set; } = false;
    }
}
