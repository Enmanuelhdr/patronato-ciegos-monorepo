using System;
using System.Windows;
using System.Windows.Controls;
using Patronato.Caja.Models;
using Patronato.Caja.Services;

namespace Patronato.Caja.Views
{
    public partial class AgendarMasajeWindow : Window
    {
        private readonly ApiService _apiService = new();
        public bool CitaAgendada { get; private set; } = false;

        public AgendarMasajeWindow()
        {
            InitializeComponent();
        }

        private void CmbDuracion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtTarifa == null) return;
            if (CmbDuracion.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                TxtTarifa.Text = $"{item.Tag}.00";
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string cliente = TxtCliente.Text.Trim();
            string telefono = TxtTelefono.Text.Trim();

            if (string.IsNullOrWhiteSpace(cliente))
            {
                MessageBox.Show("Por favor ingrese el nombre del cliente.", "Campo Obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string masajista = ((ComboBoxItem)CmbMasajista.SelectedItem)?.Content?.ToString() ?? "David Silverio (Terapeuta Invidente)";
            int duracion = 45;
            decimal tarifa = 500m;
            int sede = 1;

            if (CmbDuracion.SelectedItem is ComboBoxItem durItem && durItem.Tag != null)
            {
                int.TryParse(durItem.Tag.ToString(), out int parsedTarifa);
                tarifa = parsedTarifa;
                duracion = durItem.Content.ToString()!.Contains("30") ? 30 : (durItem.Content.ToString()!.Contains("60") ? 60 : 45);
            }

            if (CmbSede.SelectedItem is ComboBoxItem sedeItem && sedeItem.Tag != null)
            {
                int.TryParse(sedeItem.Tag.ToString(), out sede);
            }

            var dto = new CrearMasajeDto
            {
                NombreCliente = cliente,
                TelefonoCliente = telefono,
                NombreMasajista = masajista,
                DuracionMinutos = duracion,
                PrecioTarifa = tarifa,
                Sede = sede
            };

            var (success, message) = await _apiService.CrearMasajeAsync(dto);
            if (!success)
            {
                MessageBox.Show(message, "Error al Agendar", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"¡Cita para {cliente} agendada con éxito!\n\nTarifa a cobrar en Caja: RD$ {tarifa:N2}\nTerapeuta: {masajista}", "Cita Agendada", MessageBoxButton.OK, MessageBoxImage.Information);
            CitaAgendada = true;
            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
