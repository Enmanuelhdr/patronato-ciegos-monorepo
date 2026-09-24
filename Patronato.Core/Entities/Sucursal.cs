using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class Sucursal
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty; // Ej: "Sede Central Santo Domingo", "Regional Norte Santiago", "Regional Sur Barahona"
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Encargado { get; set; } = string.Empty; // Nombre del director o administrador de la sucursal
        public bool Activa { get; set; } = true;
    }    
}
