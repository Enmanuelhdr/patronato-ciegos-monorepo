using Patronato.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class SesionMasajeTacto
    {
        public int Id { get; set; }

        // Datos del cliente que recibe el masaje
        public string NombreCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        // Terapeuta invidente asignado y sede
        public string NombreMasajista { get; set; } = string.Empty;
        public SedeEnum_REVIEW Sede { get; set; }
        // Detalles de la cita
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; } // Ejemplo: 30, 45 o 60 minutos
        public decimal PrecioTarifa { get; set; }
        // Conexión con el módulo de Caja
        public bool EstaFacturadoEnCaja { get; set; } = false;
        // Regla de Negocio: Acción que ejecuta la Caja al cobrar
        public void MarcarComoCobrado()
        {
            EstaFacturadoEnCaja = true;
        }
    }
}
