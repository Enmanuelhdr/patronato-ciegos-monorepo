using System;
using System.Collections.Generic;
using System.Text;
using Patronato.Core.Enums;

namespace Patronato.Core.Entities
{
    public class Beneficiario
    {
        public int Id { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty; // Formato dominicano: 001-0000000-0
        public string Telefono { get; set; } = string.Empty;
        public SedeEnum_REVIEW SedeAsignada { get; set; }
        public TipoDiscapacidadVisual CondicionVisual { get; set; }
        public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;
    }
}
