using System;
using System.Globalization;
using System.Windows;
using Patronato.Caja.Models;
using Patronato.Caja.Services;

namespace Patronato.Caja.Views
{
    public partial class AperturaTurnoWindow : Window
    {
        private readonly ApiService _apiService = new();
        public TurnoCajaDto? TurnoAbierto { get; private set; }

        public AperturaTurnoWindow()
        {
            InitializeComponent();
            TxtCajero.Text = SessionManager.CurrentSession?.Nombre ?? "Caja Principal";
            TxtSucursal.Text = SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central";
        }

        private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            string rawFondo = TxtFondoInicial.Text?.Trim().Replace(",", "") ?? "";
            if (!decimal.TryParse(rawFondo, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fondo) || fondo < 0)
            {
                MessageBox.Show("Por favor ingrese un monto de fondo inicial válido.", "Fondo Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnConfirmar.IsEnabled = false;

            try
            {
                var (success, message, turno) = await _apiService.AbrirTurnoAsync(fondo, TxtObservaciones.Text.Trim());
                if (!success || turno == null)
                {
                    MessageBox.Show(message, "Error al Abrir Turno", MessageBoxButton.OK, MessageBoxImage.Error);
                    BtnConfirmar.IsEnabled = true;
                    return;
                }

                TurnoAbierto = turno;
                if (turno.Id < 0 || ApiService.SinConexionGateway)
                {
                    MessageBox.Show(
                        $"Turno de caja abierto en MODO SIN CONEXIÓN (Turno Contingencia #{Math.Abs(turno.Id):0000}) con un fondo inicial de RD$ {fondo:N2}.\n\n" +
                        "La caja queda completamente habilitada para operar y cobrar en efectivo o tarjeta. Todas las operaciones y transacciones se guardan localmente y se sincronizarán automáticamente con el Gateway y el Core al restablecerse la red.",
                        "Turno Abierto (Modo Sin Conexión)",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Turno #{turno.Id} abierto exitosamente con un fondo de RD$ {fondo:N2}.", "Turno Abierto", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado al abrir turno: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnConfirmar.IsEnabled = true;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
