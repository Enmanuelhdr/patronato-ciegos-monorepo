using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace Patronato.Caja;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
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

        base.OnStartup(e);
    }
}

