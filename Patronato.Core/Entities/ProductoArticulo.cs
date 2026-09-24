using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class ProductoArticulo
    {
        public int Id { get; set; }

        // Identificación del producto
        public string Codigo { get; set; } = string.Empty; // Ej: "SUAP-01", "BAST-01", "DET-GL"
        public string Nombre { get; set; } = string.Empty; // "Suaper Institucional", "Bastón Plegable", "Desinfectante 1 Galón"
        public string Categoria { get; set; } = string.Empty; // "Limpieza", "Accesibilidad", "Artesanía"
        // Control de precios y existencia
        public decimal PrecioVenta { get; set; }
        public int StockDisponible { get; set; }
        // Regla de Negocio: La Caja descuenta stock al registrar una venta
        public void DescontarStock(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a vender debe ser mayor a cero.");
            if (cantidad > StockDisponible)
                throw new InvalidOperationException($"Stock insuficiente. Solo quedan {StockDisponible} unidades disponibles.");
            StockDisponible -= cantidad;
        }
        // Regla de Negocio: Entrada de nueva producción o donación
        public void AgregarStock(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero.");
            StockDisponible += cantidad;
        }
    }
}
