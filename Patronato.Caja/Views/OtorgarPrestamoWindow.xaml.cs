using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Patronato.Caja.Models;
using Patronato.Caja.Services;

namespace Patronato.Caja.Views
{
    public partial class OtorgarPrestamoWindow : Window
    {
        private readonly ApiService _apiService = new();
        public bool PrestamoOtorgado { get; private set; } = false;

        public OtorgarPrestamoWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await CargarBeneficiariosAsync();
        }

        private async System.Threading.Tasks.Task CargarBeneficiariosAsync()
        {
            var benefs = await _apiService.GetBeneficiariosAsync();
            CmbBeneficiarios.ItemsSource = benefs;
            if (benefs.Count > 0)
                CmbBeneficiarios.SelectedIndex = 0;
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (CmbBeneficiarios.SelectedItem is not BeneficiarioInfo benef)
            {
                MessageBox.Show("Por favor seleccione un beneficiario invidente de la lista.", "Beneficiario Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtMonto.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Por favor ingrese un monto de crédito válido mayor a cero.", "Monto Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int rubro = 1;
            if (CmbRubro.SelectedItem is ComboBoxItem rubroItem && rubroItem.Tag != null)
            {
                int.TryParse(rubroItem.Tag.ToString(), out rubro);
            }

            int plazo = 12;
            if (CmbPlazo.SelectedItem is ComboBoxItem plazoItem && plazoItem.Tag != null)
            {
                int.TryParse(plazoItem.Tag.ToString(), out plazo);
            }

            var dto = new CrearPrestamoDto
            {
                BeneficiarioId = benef.Id,
                MontoAprobado = monto,
                PlazoMeses = plazo,
                Rubro = rubro,
                InvolucraFamilia = ChkFamilia.IsChecked == true
            };

            var (success, message) = await _apiService.CrearPrestamoAsync(dto);
            if (!success)
            {
                MessageBox.Show(message, "Error al Otorgar Préstamo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"¡Microcrédito otorgado exitosamente!\n\nBeneficiario: {benef.NombreCompleto}\nMonto: RD$ {monto:N2}\nPlazo: {plazo} meses\n\nEl beneficiario ya puede abonar a sus cuotas en Caja.", "Préstamo Aprobado", MessageBoxButton.OK, MessageBoxImage.Information);
            PrestamoOtorgado = true;
            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
