using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Patronato.Caja.Models;

namespace Patronato.Caja.Services
{
    public static class TicketPrintService
    {
        public static string GenerarTextoTicket(TransaccionCajaDto transaccion)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine("       PATRONATO NACIONAL DE CIEGOS, INC.         ");
            sb.AppendLine("              RNC: 4-01-00623-1                   ");
            sb.AppendLine("  Calle Huáscar Tejeda #54, Santo Domingo, D.N.   ");
            sb.AppendLine("             Tel: (809) 533-2833                  ");
            sb.AppendLine("==================================================");
            sb.AppendLine($"RECIBO / COMPROBANTE: {transaccion.NumeroRecibo}");
            sb.AppendLine($"FECHA Y HORA: {transaccion.FechaHora:dd/MM/yyyy hh:mm tt}");
            sb.AppendLine($"SUCURSAL: {transaccion.Sucursal}");
            sb.AppendLine($"CAJERO: {transaccion.Cajero}");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"TIPO DE OPERACIÓN: {FormatearTipo(transaccion.TipoTransaccion)}");
            if (!string.IsNullOrWhiteSpace(transaccion.ClienteOBeneficiario))
                sb.AppendLine($"CLIENTE / BENEFICIARIO: {transaccion.ClienteOBeneficiario}");
            if (!string.IsNullOrWhiteSpace(transaccion.IdentificacionCliente))
                sb.AppendLine($"IDENTIFICACIÓN: {transaccion.IdentificacionCliente}");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine("DESCRIPCIÓN / DETALLE:");
            if (!string.IsNullOrWhiteSpace(transaccion.DetalleLineas))
            {
                foreach (var linea in transaccion.DetalleLineas.Split('\n'))
                {
                    sb.AppendLine($"  • {linea.Trim()}");
                }
            }
            else
            {
                sb.AppendLine($"  • {transaccion.Concepto}");
            }
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"TOTAL A PAGAR:        RD$ {transaccion.MontoTotal,12:N2}");
            sb.AppendLine($"MÉTODO DE PAGO:       {transaccion.MetodoPago,16}");
            sb.AppendLine($"MONTO RECIBIDO:       RD$ {transaccion.MontoRecibido,12:N2}");
            sb.AppendLine($"DEVUELTA / CAMBIO:    RD$ {transaccion.Devuelta,12:N2}");
            sb.AppendLine("==================================================");
            sb.AppendLine("   \"La ceguera no te impide ver con el corazón\"  ");
            sb.AppendLine(" ¡Gracias por su valioso apoyo a nuestra misión!  ");
            sb.AppendLine("==================================================");

            return sb.ToString();
        }

        public static bool ImprimirVisual(Visual visual, string descripcion)
        {
            try
            {
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    printDlg.PrintVisual(visual, descripcion);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo completar la impresión: {ex.Message}", "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static string FormatearTipo(string tipo) => tipo switch
        {
            "VentaProductos" => "VENTA DE PRODUCTOS E INSUMOS",
            "MasajeTacto" => "SESIÓN DE MASAJE TACTO",
            "AbonoPrestamo" => "ABONO A PRÉSTAMO DE EMPRENDIMIENTO",
            "Donacion" => "RECEPCIÓN DE DONACIÓN",
            _ => tipo
        };
    }
}
