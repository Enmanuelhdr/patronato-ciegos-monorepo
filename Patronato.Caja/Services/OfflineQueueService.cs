using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Patronato.Caja.Services
{
    // Una transacción de cobro que no pudo llegar al Gateway porque la Caja se quedó sin
    // conexión. Guarda el código y los datos exactamente como se mandarían por el socket,
    // para poder reenviarla tal cual en cuanto vuelva la conexión (ver ApiService.SincronizarPendientesAsync).
    public class ColaItem
    {
        public string Codigo { get; set; } = "";
        public JsonElement Datos { get; set; }
        public string NumeroReciboProvisional { get; set; } = "";
        public DateTime FechaHora { get; set; } = DateTime.Now;
    }

    // Cola de cobros pendientes de confirmar con el Gateway, persistida en un archivo local
    // para sobrevivir un cierre de la Caja mientras sigue sin conexión. Es la mitad
    // "Caja -> Gateway" del store-and-forward; la otra mitad ("Gateway -> Core") vive en
    // Patronato.Gateway.Services.PendingQueueStore, de forma simétrica e independiente: cada
    // nivel solo se preocupa por su propio siguiente salto.
    public static class OfflineQueueService
    {
        private static readonly string RutaCola = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PatronatoCaja", "cola_pendientes.json");

        private static readonly object Lock = new();
        private static readonly List<ColaItem> Pendientes = Cargar();
        private static int _contador = 0;

        // Se dispara cada vez que la cola cambia (se encola algo nuevo o se confirma una
        // pendiente); MainWindow lo usa para refrescar el badge de estado sin tener que
        // preguntar en cada tick.
        public static event Action? CambioPendientes;

        public static int CantidadPendientes
        {
            get { lock (Lock) return Pendientes.Count; }
        }

        public static string GenerarNumeroProvisional(string prefijo)
        {
            lock (Lock)
            {
                _contador++;
                return $"OFFLINE-{prefijo}-{DateTime.Now:yyyyMMddHHmmss}-{_contador:D3}";
            }
        }

        public static void Encolar(string codigo, object datos, string numeroReciboProvisional)
        {
            lock (Lock)
            {
                Pendientes.Add(new ColaItem
                {
                    Codigo = codigo,
                    Datos = JsonSerializer.SerializeToElement(datos, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    NumeroReciboProvisional = numeroReciboProvisional,
                    FechaHora = DateTime.Now
                });
                Guardar();
            }
            CambioPendientes?.Invoke();
        }

        public static List<ColaItem> ObtenerTodos()
        {
            lock (Lock) return new List<ColaItem>(Pendientes);
        }

        public static void QuitarPrimero()
        {
            lock (Lock)
            {
                if (Pendientes.Count > 0)
                {
                    Pendientes.RemoveAt(0);
                    Guardar();
                }
            }
            CambioPendientes?.Invoke();
        }

        private static List<ColaItem> Cargar()
        {
            try
            {
                if (File.Exists(RutaCola))
                {
                    var json = File.ReadAllText(RutaCola);
                    return JsonSerializer.Deserialize<List<ColaItem>>(json) ?? new List<ColaItem>();
                }
            }
            catch
            {
                // Archivo corrupto o ilegible: se empieza con la cola vacía en vez de tumbar la Caja.
            }
            return new List<ColaItem>();
        }

        private static void Guardar()
        {
            try
            {
                var dir = Path.GetDirectoryName(RutaCola)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(RutaCola, JsonSerializer.Serialize(Pendientes));
            }
            catch
            {
                // Si no se puede persistir, la transacción sigue en memoria para este proceso;
                // se reintentará igual mientras la app no se cierre.
            }
        }
    }
}
