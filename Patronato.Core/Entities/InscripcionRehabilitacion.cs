using Patronato.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class InscripcionRehabilitacion
    {
        public int Id { get; set; }
        // Relación con el Beneficiario
        public int BeneficiarioId { get; set; }
        public Beneficiario? Beneficiario { get; set; }
        // Sede y etapa del programa
        public SedeEnum_REVIEW Sede { get; set; }
        public EtapaRehabilitacion Etapa { get; set; } // Funcional o Profesional
        // Taller u oficio específico que cursa
        // Ej: "Orientación y Movilidad (Bastón)", "Braille", "Telefonista", "Artesanía"
        public string TallerMateria { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
        public bool Completado { get; set; } = false;
        // Regla de Negocio: Graduar al alumno de la etapa
        public void GraduarBeneficiario()
        {
            Completado = true;
        }
    }
}
