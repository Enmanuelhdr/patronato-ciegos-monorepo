using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

using System.Windows.Threading;

namespace Patronato.Caja;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(App));

    protected override void OnStartup(StartupEventArgs e)
    {
        // Configurar log4net antes de abrir cualquier ventana (ver log4net.config).
        // LogRoot apunta a AppData, no a la carpeta de instalación: la Caja puede vivir en
        // Program Files (solo lectura), igual que cola_pendientes.json (ver OfflineQueueService).
        var logRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PatronatoCaja");
        log4net.GlobalContext.Properties["LogRoot"] = logRoot;
        log4net.Config.XmlConfigurator.Configure(new FileInfo(Path.Combine(AppContext.BaseDirectory, "log4net.config")));

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        base.OnStartup(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("Excepción no controlada capturada por App.DispatcherUnhandledException.", e.Exception);
        MessageBox.Show(
            $"Se produjo un error en la aplicación de Caja:\n\n{e.Exception.Message}\n\nLos detalles han sido registrados en la bitácora de auditoría.",
            "Aviso del Sistema de Caja",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Logger.Fatal("Excepción no controlada en AppDomain.", ex);
        }
    }
}

