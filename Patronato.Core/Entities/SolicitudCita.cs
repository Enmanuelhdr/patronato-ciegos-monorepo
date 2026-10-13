using Patronato.Core.Enums;
using System;

namespace Patronato.Core.Entities
{
    // Solicitud de cita que cualquier persona (todavía no registrada como Beneficiario)
    // puede enviar desde la Web pública, sin iniciar sesión. Recepción/Admin la revisa
    // y, si aplica, la confirma y da seguimiento creando el registro real (Consulta,
    // Inscripción de Rehabilitación o Sesión de Masaje) desde su propia pantalla.
    public class SolicitudCita
    {
        public int Id { get; set; }

        public string NombreSolicitante { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;

        public TipoServicioCita TipoServicio { get; set; } = TipoServicioCita.ConsultaOftalmologica;
        public SedeEnum_REVIEW SedePreferida { get; set; } = SedeEnum_REVIEW.SantoDomingo;
        public DateTime FechaPreferida { get; set; }
        public string? Comentario { get; set; }

        public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        public void Confirmar()
        {
            Estado = EstadoCita.Confirmada;
        }

        public void Cancelar()
        {
            Estado = EstadoCita.Cancelada;
        }
    }
}
