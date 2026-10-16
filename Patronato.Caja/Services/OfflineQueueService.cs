using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Patronato.Caja.Models;

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
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(OfflineQueueService));

        private static readonly string RutaCola = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PatronatoCaja", "cola_pendientes.json");

        private static readonly object Lock = new();
        private static readonly List<ColaItem> Pendientes = Cargar();
        private static int _contador = 0;

        private static readonly string RutaRechazados = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PatronatoCaja", "cola_rechazadas.json");

        private static readonly string RutaTurnoOffline = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PatronatoCaja", "turno_offline_activo.json");

        // Se dispara cada vez que la cola cambia (se encola algo nuevo o se confirma una
        // pendiente); MainWindow lo usa para refrescar el badge de estado sin tener que
        // preguntar en cada tick.
        public static event Action? CambioPendientes;

        // Se dispara cuando el Core rechaza formalmente un cobro offline (código 05, etc.)
        public static event Action<string, string>? TransaccionRechazada;

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
                ActualizarMetricasTurnoOfflineSiAplica(codigo, datos);
            }
            CambioPendientes?.Invoke();
        }

        public static int GenerarIdTurnoOffline()
        {
            lock (Lock)
            {
                _contador++;
                return -1 * ((int)(DateTime.Now.Ticks % 100000) + _contador);
            }
        }

        public static void GuardarTurnoOffline(TurnoCajaDto turno)
        {
            lock (Lock)
            {
                try
                {
                    var dir = Path.GetDirectoryName(RutaTurnoOffline)!;
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(RutaTurnoOffline, JsonSerializer.Serialize(turno, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                }
                catch (Exception ex)
                {
                    Logger.Error("No se pudo persistir el turno offline.", ex);
                }
            }
        }

        public static TurnoCajaDto? ObtenerTurnoOfflineActivo()
        {
            lock (Lock)
            {
                try
                {
                    if (File.Exists(RutaTurnoOffline))
                    {
                        var json = File.ReadAllText(RutaTurnoOffline);
                        var turno = JsonSerializer.Deserialize<TurnoCajaDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        if (turno != null && string.Equals(turno.Estado, "Abierto", StringComparison.OrdinalIgnoreCase))
                        {
                            return turno;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error al leer el turno offline activo.", ex);
                }
                return null;
            }
        }

        public static TurnoCajaDto CerrarTurnoOffline(
            int turnoId, 
            decimal efectivoContado, 
            string desgloseJson, 
            string? observaciones,
            decimal? montoBoveda, 
            decimal? fondoRemanente, 
            string? destinoFondos)
        {
            lock (Lock)
            {
                var turno = ObtenerTurnoOfflineActivo() ?? new TurnoCajaDto
                {
                    Id = turnoId,
                    Cajero = "Caja Principal",
                    Sucursal = "Sede Central",
                    FondoInicial = 0,
                    FechaHoraApertura = DateTime.Now
                };

                turno.Estado = "Cerrado";
                turno.FechaHoraCierre = DateTime.Now;
                turno.EfectivoContado = efectivoContado;
                turno.Diferencia = efectivoContado - (turno.FondoInicial + turno.TotalVentasEfectivo);
                turno.ObservacionesCierre = observaciones;
                turno.MontoEntregadoBoveda = montoBoveda ?? 0;
                turno.FondoRemanenteCaja = fondoRemanente ?? 0;
                turno.DestinoFondos = destinoFondos ?? "RetiroTotalBoveda";

                try
                {
                    if (File.Exists(RutaTurnoOffline))
                    {
                        File.Delete(RutaTurnoOffline);
                    }
                }
                catch { }

                return turno;
            }
        }

        public static void LimpiarTurnoOffline()
        {
            lock (Lock)
            {
                try
                {
                    if (File.Exists(RutaTurnoOffline))
                    {
                        File.Delete(RutaTurnoOffline);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("No se pudo limpiar el turno offline.", ex);
                }
            }
        }

        private static void ActualizarMetricasTurnoOfflineSiAplica(string codigo, object datos)
        {
            try
            {
                var turno = ObtenerTurnoOfflineActivo();
                if (turno == null) return;

                decimal monto = 0;
                string metodo = "Efectivo";

                var json = JsonSerializer.Serialize(datos, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("metodoPago", out var mpEl))
                {
                    metodo = mpEl.GetString() ?? "Efectivo";
                }

                if (root.TryGetProperty("montoRecibido", out var mrEl) && mrEl.TryGetDecimal(out var mr))
                {
                    monto = mr;
                }
                else if (root.TryGetProperty("montoAprobado", out var maEl) && maEl.TryGetDecimal(out var ma))
                {
                    monto = ma;
                }
                else if (root.TryGetProperty("montoDonacion", out var mdEl) && mdEl.TryGetDecimal(out var md))
                {
                    monto = md;
                }

                if (string.Equals(metodo, "Efectivo", StringComparison.OrdinalIgnoreCase))
                {
                    turno.TotalVentasEfectivo += monto;
                }
                else if (string.Equals(metodo, "Tarjeta", StringComparison.OrdinalIgnoreCase))
                {
                    turno.TotalVentasTarjeta += monto;
                }
                else
                {
                    turno.TotalVentasTransferencia += monto;
                }

                turno.TotalGeneralVentas = turno.TotalVentasEfectivo + turno.TotalVentasTarjeta + turno.TotalVentasTransferencia;
                turno.EfectivoEsperado = turno.FondoInicial + turno.TotalVentasEfectivo;

                GuardarTurnoOffline(turno);
            }
            catch (Exception ex)
            {
                Logger.Error("No se pudieron actualizar métricas del turno offline.", ex);
            }
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

        public static void ArchivarRechazado(ColaItem item, string motivo)
        {
            lock (Lock)
            {
                Pendientes.Remove(item);
                Guardar();

                try
                {
                    var dir = Path.GetDirectoryName(RutaRechazados)!;
                    Directory.CreateDirectory(dir);
                    var lista = new List<object>();
                    if (File.Exists(RutaRechazados))
                    {
                        var json = File.ReadAllText(RutaRechazados);
                        lista = JsonSerializer.Deserialize<List<object>>(json) ?? new();
                    }
                    lista.Add(new
                    {
                        item.Codigo,
                        item.NumeroReciboProvisional,
                        item.FechaHora,
                        FechaRechazo = DateTime.Now,
                        Motivo = motivo,
                        Datos = item.Datos
                    });
                    File.WriteAllText(RutaRechazados, JsonSerializer.Serialize(lista));
                }
                catch (Exception ex)
                {
                    Logger.Error("No se pudo archivar la transacción rechazada.", ex);
                }
            }
            CambioPendientes?.Invoke();
            TransaccionRechazada?.Invoke(item.NumeroReciboProvisional, motivo);
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
            catch (Exception ex)
            {
                // Archivo corrupto o ilegible: se empieza con la cola vacía en vez de tumbar la Caja.
                Logger.Error("No se pudo leer la cola de pendientes hacia el Gateway; se empieza vacía.", ex);
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
            catch (Exception ex)
            {
                // Si no se puede persistir, la transacción sigue en memoria para este proceso;
                // se reintentará igual mientras la app no se cierre.
                Logger.Error("No se pudo persistir la cola de pendientes hacia el Gateway.", ex);
            }
        }
    }
}
