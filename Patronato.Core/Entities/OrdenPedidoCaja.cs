using System;

namespace Patronato.Core.Entities
{
    public class OrdenPedidoCaja
    {
        public int Id { get; set; }
        public string NumeroOrden { get; set; } = string.Empty; // Ej: ORD-202609241530-101
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteDocumento { get; set; }
        public string CreadoPor { get; set; } = string.Empty; // Ej: "Laura Sánchez (Recepcionista)"
        public string Estado { get; set; } = "Pendiente"; // "Pendiente", "Cobrada", "Cancelada"
        public decimal Total { get; set; }
        public string ItemsJson { get; set; } = "[]";
        public string? Notas { get; set; }
    }
}
