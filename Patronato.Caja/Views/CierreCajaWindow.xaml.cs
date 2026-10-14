using System;
using System.Windows;
using Patronato.Caja.Models;
using Patronato.Caja.Services;

namespace Patronato.Caja.Views
{
    // Reporte de cierre/arqueo de caja: deliberadamente distinto del comprobante de venta
    // (ReceiptPreviewWindow), que es angosto y pensado para una sola transacción. Este es un
    // documento de corte de turno: ancho, con tablas de resumen y espacio para firmas.
    public partial class CierreCajaWindow : Window
    {
        private readonly CuadreCajaDto _cuadre;

        public CierreCajaWindow(CuadreCajaDto cuadre)
        {
            InitializeComponent();
            _cuadre = cuadre;
            CargarDatos();
        }

        private void CargarDatos()
        {
            TxtNumeroCierre.Text = $"CIERRE-{DateTime.Now:yyyyMMdd-HHmm}";
            TxtFecha.Text = DateTime.TryParse(_cuadre.Fecha, out var fecha) ? fecha.ToString("dd/MM/yyyy") : _cuadre.Fecha;
            TxtSucursal.Text = string.IsNullOrWhiteSpace(SessionManager.CurrentSession?.SucursalNombre)
                ? "Sede Central Santo Domingo"
                : SessionManager.CurrentSession!.SucursalNombre;
            TxtCajero.Text = string.IsNullOrWhiteSpace(_cuadre.Cajero)
                ? (SessionManager.CurrentSession?.Nombre ?? "Caja Principal")
                : _cuadre.Cajero;

            TxtTotalEfectivo.Text = $"RD$ {_cuadre.PorMetodo.Efectivo:N2}";
            TxtTotalTarjeta.Text = $"RD$ {_cuadre.PorMetodo.Tarjeta:N2}";
            TxtTotalTransferencia.Text = $"RD$ {_cuadre.PorMetodo.Transferencia:N2}";

            TxtTotalVentas.Text = $"RD$ {_cuadre.PorConcepto.VentasProductos:N2}";
            TxtTotalMasajes.Text = $"RD$ {_cuadre.PorConcepto.MasajesTacto:N2}";
            TxtTotalPrestamos.Text = $"RD$ {_cuadre.PorConcepto.AbonosPrestamos:N2}";
            TxtTotalDonaciones.Text = $"RD$ {_cuadre.PorConcepto.Donaciones:N2}";

            TxtTotalGeneral.Text = $"RD$ {_cuadre.TotalGeneral:N2}";
            TxtCantidadTx.Text = $"{_cuadre.CantidadTransacciones} transacciones auditadas en este corte";

            GridMovimientos.ItemsSource = _cuadre.Movimientos;

            TxtGeneradoEn.Text = $"Generado el {DateTime.Now:dd/MM/yyyy hh:mm tt}";
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            bool exito = TicketPrintService.ImprimirVisual(ReporteVisualBorder, $"Cierre de Caja {TxtNumeroCierre.Text}");
            if (exito)
            {
                MessageBox.Show("El reporte de cierre de caja ha sido enviado a la cola de impresión.", "Impresión Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
