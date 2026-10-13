using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Patronato.Caja.Services
{
    // Último login exitoso de cada usuario contra el Core, guardado localmente para que
    // esta terminal pueda seguir abriendo turno cuando el Gateway no responde. No guarda la
    // contraseña: solo su hash (igual de "secreto" que lo que ya viaja y se guarda en el
    // Core, ver Usuario.PasswordHash), así que un acceso al archivo no revela la clave real.
    public class CredencialOffline
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string PasswordHashLocal { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public DateTime UltimoLoginExitoso { get; set; }
    }

    // Guarda, por cada usuario que alguna vez entró con conexión al Gateway, los datos
    // mínimos para reabrir sesión sin conexión (ver ApiService.LoginAsync). Es la mitad
    // "autenticación" del mismo patrón de resiliencia que ya usa OfflineQueueService para
    // los cobros: lo último bueno conocido se guarda en disco y se usa si el Gateway cae.
    public static class OfflineCredentialsService
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(OfflineCredentialsService));

        private static readonly string RutaArchivo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PatronatoCaja", "credenciales_offline.json");

        private static readonly object Lock = new();

        private static string Hash(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        // Se llama justo después de un LOGIN exitoso por el Gateway, para que esta terminal
        // recuerde a este usuario la próxima vez que no haya conexión.
        public static void GuardarUltimoLogin(string nombreUsuario, string password, LoginResponseResumen datos)
        {
            lock (Lock)
            {
                var lista = Cargar();
                lista.RemoveAll(c => string.Equals(c.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase));
                lista.Add(new CredencialOffline
                {
                    NombreUsuario = nombreUsuario,
                    PasswordHashLocal = Hash(password),
                    UsuarioId = datos.UsuarioId,
                    Nombre = datos.Nombre,
                    Rol = datos.Rol,
                    SucursalId = datos.SucursalId,
                    SucursalNombre = datos.SucursalNombre,
                    UltimoLoginExitoso = DateTime.Now
                });
                Guardar(lista);
            }
        }

        // Intenta validar contra la última credencial guardada localmente para este usuario.
        // Solo tiene éxito si coincide el usuario Y el hash de la contraseña.
        public static CredencialOffline? ValidarOffline(string nombreUsuario, string password)
        {
            lock (Lock)
            {
                var lista = Cargar();
                string hash = Hash(password);
                return lista.FirstOrDefault(c =>
                    string.Equals(c.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase) &&
                    c.PasswordHashLocal == hash);
            }
        }

        private static List<CredencialOffline> Cargar()
        {
            try
            {
                if (File.Exists(RutaArchivo))
                {
                    var json = File.ReadAllText(RutaArchivo);
                    return JsonSerializer.Deserialize<List<CredencialOffline>>(json) ?? new List<CredencialOffline>();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("No se pudo leer las credenciales guardadas para login sin conexión; se empieza vacío.", ex);
            }
            return new List<CredencialOffline>();
        }

        private static void Guardar(List<CredencialOffline> lista)
        {
            try
            {
                var dir = Path.GetDirectoryName(RutaArchivo)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(RutaArchivo, JsonSerializer.Serialize(lista));
            }
            catch (Exception ex)
            {
                Logger.Error("No se pudo persistir las credenciales para login sin conexión.", ex);
            }
        }
    }

    // Subconjunto de LoginResponse/UsuarioSession que necesita OfflineCredentialsService;
    // evita acoplar este servicio al modelo completo de sesión.
    public class LoginResponseResumen
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
    }
}
