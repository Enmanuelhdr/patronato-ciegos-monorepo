using System;

namespace Patronato.Core.Entities
{
    // Historial de intentos de inicio de sesión de un WebUser (exitosos y fallidos),
    // para que el Core pueda auditarlos desde la pantalla de Accesos Web.
    public class WebUserLoginHistory
    {
        public int Id { get; set; }

        public int WebUserId { get; set; }
        public WebUser? WebUser { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public bool Exitoso { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
    }
}
