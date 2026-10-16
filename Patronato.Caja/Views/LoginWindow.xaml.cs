using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Patronato.Caja.Services;

namespace Patronato.Caja.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService = new();

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await ProbarConexionAsync();
        }

        private async Task ProbarConexionAsync()
        {
            SessionManager.BaseApiUrl = TxtApiUrl.Text.Trim();
            LedConexion.Fill = new SolidColorBrush(Color.FromRgb(234, 179, 8)); // Amarillo
            TxtEstadoConexion.Text = "Comprobando conexión con el Gateway...";
            TxtEstadoConexion.Foreground = new SolidColorBrush(Color.FromRgb(161, 98, 7));

            bool conectado = await _apiService.TestConnectionAsync();
            if (conectado)
            {
                TxtApiUrl.Text = SessionManager.BaseApiUrl;
                LedConexion.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Verde
                TxtEstadoConexion.Text = $"Conectado al Gateway en {SessionManager.BaseApiUrl}";
                TxtEstadoConexion.Foreground = new SolidColorBrush(Color.FromRgb(4, 120, 87));
            }
            else
            {
                LedConexion.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Rojo
                TxtEstadoConexion.Text = "No conectado. Inicia Patronato.Gateway (y Patronato.Core)";
                TxtEstadoConexion.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
            }
        }

        private async void BtnProbarConexion_Click(object sender, RoutedEventArgs e)
        {
            await ProbarConexionAsync();
        }

        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (TxtPasswordVisible.Visibility == Visibility.Visible)
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                BtnTogglePassword.Content = "👁️";
            }
            else
            {
                TxtPasswordVisible.Text = TxtPassword.Password;
                TxtPassword.Visibility = Visibility.Collapsed;
                TxtPasswordVisible.Visibility = Visibility.Visible;
                BtnTogglePassword.Content = "🙈";
            }
        }

        private void BtnDemoCajero_Click(object sender, RoutedEventArgs e)
        {
            TxtUsuario.Text = "cajero";
            TxtPassword.Password = "cajero123";
            TxtPasswordVisible.Text = "cajero123";
            TxtError.Visibility = Visibility.Collapsed;
        }

        private void BtnDemoAdmin_Click(object sender, RoutedEventArgs e)
        {
            TxtUsuario.Text = "admin";
            TxtPassword.Password = "admin123";
            TxtPasswordVisible.Text = "admin123";
            TxtError.Visibility = Visibility.Collapsed;
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuario = TxtUsuario.Text.Trim();
            string pass = TxtPasswordVisible.Visibility == Visibility.Visible ? TxtPasswordVisible.Text : TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(pass))
            {
                MostrarError("Por favor ingrese usuario y contraseña.");
                return;
            }

            TxtError.Visibility = Visibility.Collapsed;
            BtnLogin.IsEnabled = false;
            BtnLogin.Content = "Autenticando...";

            try
            {
                SessionManager.BaseApiUrl = TxtApiUrl.Text.Trim();
                var (success, message, session) = await _apiService.LoginAsync(usuario, pass);

                if (!success || session == null)
                {
                    MostrarError(message);
                    return;
                }

                SessionManager.SetSession(session);

                // Abrir MainWindow
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MostrarError($"Error inesperado: {ex.Message}");
            }
            finally
            {
                BtnLogin.IsEnabled = true;
                BtnLogin.Content = "Entrar a Caja";
            }
        }

        private void MostrarError(string mensaje)
        {
            TxtError.Text = mensaje;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
