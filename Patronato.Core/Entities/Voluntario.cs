using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class Voluntario
    {
        public int Id { get; set; }
        // Datos del voluntario
        public string NombreCompleto { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        // Área en la que apoya
        // Ej: "Lectura y Acompañamiento", "Docencia Braille", "Logística Eventos (Ajedrez)"
        public string AreaApoyo { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;
    }
}
