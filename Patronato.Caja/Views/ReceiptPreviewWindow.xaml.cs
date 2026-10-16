using System;
using System.Windows;
using Patronato.Caja.Models;
using Patronato.Caja.Services;

namespace Patronato.Caja.Views
{
    public partial class ReceiptPreviewWindow : Window
    {
        private readonly TransaccionCajaDto _transaccion;

        public ReceiptPreviewWindow(TransaccionCajaDto transaccion)
        {
            InitializeComponent();
            _transaccion = transaccion;
            CargarDatos();
        }

        private void CargarDatos()
        {
            if (_transaccion.EsAnulada)
            {
                BannerAnulado.Visibility = Visibility.Visible;
                TxtInfoAnulacion.Text = $"Anulado por: {_transaccion.AnuladoPor ?? "Supervisor"} el {_transaccion.FechaHoraAnulacion:dd/MM/yyyy hh:mm tt}";
                TxtMotivoAnulacionTicket.Text = $"Motivo: {_transaccion.MotivoAnulacion ?? "Sin motivo registrado"}";
                TxtNumeroRecibo.Text = $"{_transaccion.NumeroRecibo} (ANULADO)";
                TxtNumeroRecibo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
            }
            else
            {
                BannerAnulado.Visibility = Visibility.Collapsed;
                TxtNumeroRecibo.Text = string.IsNullOrWhiteSpace(_transaccion.NumeroRecibo) ? $"REC-{_transaccion.Id:D6}" : _transaccion.NumeroRecibo;
            }

            TxtFechaHora.Text = _transaccion.FechaHora.ToString("dd/MM/yyyy hh:mm tt");
            TxtSucursal.Text = string.IsNullOrWhiteSpace(_transaccion.Sucursal) ? "Sede Central Santo Domingo" : _transaccion.Sucursal;
            TxtCajero.Text = string.IsNullOrWhiteSpace(_transaccion.Cajero) ? "Caja Principal" : _transaccion.Cajero;
            TxtTipoOperacion.Text = FormatearTipo(_transaccion.TipoTransaccion);

            if (!string.IsNullOrWhiteSpace(_transaccion.ClienteOBeneficiario))
            {
                TxtCliente.Text = _transaccion.ClienteOBeneficiario;
                PanelCliente.Visibility = Visibility.Visible;
            }
            else
            {
                PanelCliente.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrWhiteSpace(_transaccion.IdentificacionCliente))
            {
                TxtIdentificacion.Text = _transaccion.IdentificacionCliente;
                PanelIdentificacion.Visibility = Visibility.Visible;
            }
            else
            {
                PanelIdentificacion.Visibility = Visibility.Collapsed;
            }

            TxtDetalle.Text = !string.IsNullOrWhiteSpace(_transaccion.DetalleLineas)
                ? _transaccion.DetalleLineas
                : _transaccion.Concepto;

            TxtTotal.Text = $"RD$ {_transaccion.MontoTotal:N2}";
            TxtMetodoPago.Text = _transaccion.MetodoPago;

            bool esEfectivo = string.Equals(_transaccion.MetodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase);
            if (esEfectivo)
            {
                PanelMontoRecibido.Visibility = Visibility.Visible;
                PanelDevuelta.Visibility = Visibility.Visible;
                TxtMontoRecibido.Text = $"RD$ {_transaccion.MontoRecibido:N2}";
                TxtDevuelta.Text = $"RD$ {_transaccion.Devuelta:N2}";
            }
            else
            {
                // En pagos electrónicos (Tarjeta, Transferencia), no aplica recepción de efectivo ni cambio
                PanelMontoRecibido.Visibility = Visibility.Collapsed;
                PanelDevuelta.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            bool exito = TicketPrintService.ImprimirVisual(TicketVisualBorder, $"Recibo {_transaccion.NumeroRecibo}");
            if (exito)
            {
                MessageBox.Show("El recibo ha sido enviado a la cola de impresión.", "Impresión Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string FormatearTipo(string tipo) => tipo switch
        {
            "VentaProductos" => "VENTA DE PRODUCTOS E INSUMOS",
            "MasajeTacto" => "SESIÓN DE MASAJE TACTO",
            "ConsultaOftalmologica" => "CONSULTA OFTALMOLÓGICA",
            "AbonoPrestamo" => "ABONO A PRÉSTAMO DE EMPRENDIMIENTO",
            "Donacion" => "RECEPCIÓN DE DONACIÓN",
            _ => tipo?.ToUpperInvariant() ?? "COMPROBANTE DE PAGO"
        };
    }
}
