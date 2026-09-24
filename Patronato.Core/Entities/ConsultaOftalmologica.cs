using Patronato.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class ConsultaOftalmologica
    {
        public int Id { get; set; }
        // Relación con el Beneficiario
        public int BeneficiarioId { get; set; }
        public Beneficiario? Beneficiario { get; set; }
        // Sede y médico especialista
        public SedeEnum_REVIEW Sede { get; set; }
        public string Oftalmologo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        // Diagnóstico y tratamiento preventivo
        public string DiagnosticoOcular { get; set; } = string.Empty;
        public string TratamientoPrescrito { get; set; } = string.Empty;
        // Aspectos financieros y de seguro
        public decimal TarifaConsulta { get; set; }
        public bool CubiertoPorARS { get; set; } // Conecta con Integración (validación con SENASA/ARS)
        public bool EstaFacturadaEnCaja { get; set; } = false;
        // Regla de Negocio: Asentar diagnóstico médico
        public void RegistrarDiagnostico(string diagnostico, string tratamiento)
        {
            if (string.IsNullOrWhiteSpace(diagnostico))
                throw new ArgumentException("El diagnóstico no puede estar vacío.");
            DiagnosticoOcular = diagnostico;
            TratamientoPrescrito = tratamiento;
        }
        public void MarcarComoCobrada()
        {
            EstaFacturadaEnCaja = true;
        }
    }
}
