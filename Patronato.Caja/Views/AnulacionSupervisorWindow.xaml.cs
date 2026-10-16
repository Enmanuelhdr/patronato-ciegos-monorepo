using System;
using System.Windows;
using Patronato.Caja.Models;
using Patronato.Caja.Services;

namespace Patronato.Caja.Views
{
    public partial class AnulacionSupervisorWindow : Window
    {
        private readonly TransaccionCajaDto _recibo;
        private readonly ApiService _apiService = new();
        public bool AnuladaExitosa { get; private set; }

        public AnulacionSupervisorWindow(TransaccionCajaDto recibo)
        {
            InitializeComponent();
            _recibo = recibo;
            CargarDatos();
        }

        private void CargarDatos()
        {
            TxtNumeroRecibo.Text = $"RECIBO: {_recibo.NumeroRecibo}";
            TxtConcepto.Text = $"Concepto: {_recibo.Concepto}";
            TxtCliente.Text = $"Cliente / Beneficiario: {_recibo.ClienteOBeneficiario ?? "N/A"}";
            TxtMontoTotal.Text = $"RD$ {_recibo.MontoTotal:N2}";
        }

        private void BtnToggleSupervisorPassword_Click(object sender, RoutedEventArgs e)
        {
            if (TxtPasswordSupervisorVisible.Visibility == Visibility.Visible)
            {
                TxtPasswordSupervisor.Password = TxtPasswordSupervisorVisible.Text;
                TxtPasswordSupervisorVisible.Visibility = Visibility.Collapsed;
                TxtPasswordSupervisor.Visibility = Visibility.Visible;
                BtnToggleSupervisorPassword.Content = "👁️";
            }
            else
            {
                TxtPasswordSupervisorVisible.Text = TxtPasswordSupervisor.Password;
                TxtPasswordSupervisor.Visibility = Visibility.Collapsed;
                TxtPasswordSupervisorVisible.Visibility = Visibility.Visible;
                BtnToggleSupervisorPassword.Content = "🙈";
            }
        }

        private async void BtnConfirmarAnulacion_Click(object sender, RoutedEventArgs e)
        {
            string supervisor = TxtUsuarioSupervisor.Text.Trim();
            string password = TxtPasswordSupervisorVisible.Visibility == Visibility.Visible
                ? TxtPasswordSupervisorVisible.Text
                : TxtPasswordSupervisor.Password;
            string motivo = TxtMotivo.Text.Trim();

            if (string.IsNullOrWhiteSpace(supervisor))
            {
                MessageBox.Show("Por favor indique el usuario supervisor.", "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Por favor ingrese la contraseña del supervisor.", "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("Por favor ingrese el motivo de la anulación para los registros de auditoría.", "Motivo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnConfirmarAnulacion.IsEnabled = false;

            try
            {
                var (success, message, _) = await _apiService.AnularTransaccionAsync(_recibo.Id, supervisor, password, motivo);
                if (!success)
                {
                    MessageBox.Show(message, "Error al Anular Transacción", MessageBoxButton.OK, MessageBoxImage.Error);
                    BtnConfirmarAnulacion.IsEnabled = true;
                    return;
                }

                AnuladaExitosa = true;
                MessageBox.Show($"La transacción {_recibo.NumeroRecibo} ha sido anulada con éxito.", "Anulación Confirmada", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado al anular la transacción: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnConfirmarAnulacion.IsEnabled = true;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
