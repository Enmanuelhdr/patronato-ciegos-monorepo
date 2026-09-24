using System;

namespace Patronato.Core.Entities
{
    public class TransaccionCaja
    {
        public int Id { get; set; }
        public string NumeroRecibo { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public string TipoTransaccion { get; set; } = string.Empty; // "VentaProductos", "MasajeTacto", "AbonoPrestamo", "Donacion"
        public string Concepto { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public string MetodoPago { get; set; } = "Efectivo"; // "Efectivo", "Tarjeta", "Transferencia"
        public decimal MontoRecibido { get; set; }
        public decimal Devuelta { get; set; }
        public string Cajero { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public string? ClienteOBeneficiario { get; set; }
        public string? IdentificacionCliente { get; set; }
        public string? DetalleLineas { get; set; }
        public int? ReferenciaId { get; set; }
    }
}
