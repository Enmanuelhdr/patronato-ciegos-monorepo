using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class Donacion
    {
        public int Id { get; set; }
        // Datos del benefactor o empresa donante
        public string Donante { get; set; } = string.Empty;
        public string RncCedula { get; set; } = string.Empty; // Cédula o RNC para fines de descargo fiscal
        // Monto y transacción
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string MetodoPago { get; set; } = "Efectivo"; // "Efectivo", "Tarjeta", "Transferencia"
        public bool RequiereComprobanteFiscal { get; set; } = false; // Comprobante fiscal dominicano (NCF)
        // Conexión con Caja
        public bool EstaRegistradaEnCaja { get; set; } = true;
    }
}
