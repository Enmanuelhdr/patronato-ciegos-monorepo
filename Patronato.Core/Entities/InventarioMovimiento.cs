using System;
using Patronato.Core.Enums;

namespace Patronato.Core.Entities
{
    // Bitácora de movimientos de stock hechos desde el Core/Web: qué producto, qué
    // tipo de movimiento, cuánto cambió el stock y qué usuario del Core lo hizo.
    // No cubre las ventas de la Caja (esas ya descuentan stock por el socket existente
    // y quedan fuera de este alcance).
    public class InventarioMovimiento
    {
        public int Id { get; set; }

        public int ProductoId { get; set; }
        public ProductoArticulo? Producto { get; set; }

        public TipoMovimientoInventario Tipo { get; set; }
        public int Cantidad { get; set; }
        public int StockResultante { get; set; }
        public string? Motivo { get; set; }

        public int CoreUserId { get; set; }
        public Usuario? CoreUser { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
