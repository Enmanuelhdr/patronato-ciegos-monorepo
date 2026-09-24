using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class EmpresaAliada
    {
        public int Id { get; set; }
        // Identificación fiscal y corporativa dominicana
        public string RazonSocial { get; set; } = string.Empty; // Nombre de la empresa
        public string RNC { get; set; } = string.Empty;         // Registro Nacional del Contribuyente (RNC)
        public string SectorEmpresa { get; set; } = string.Empty; // "Zona Franca", "Telecomunicaciones", "Banca", "Servicios"
        // Enlace institucional y seguimiento
        public string ContactoRecursosHumanos { get; set; } = string.Empty;
        public string TelefonoContacto { get; set; } = string.Empty;
        public string PuestosOfertados { get; set; } = string.Empty; // Ej: "Telefonista, Recepción, Empaque de Tabaco"
        public bool ConvenioActivo { get; set; } = true;
    }
}
