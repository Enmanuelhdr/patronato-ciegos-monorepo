using System.Text.Json;

namespace Patronato.Gateway.Services;

// Una transacción de cobro que no pudo llegar al Core porque estaba caído.
// Guarda todo lo necesario para reenviarla tal cual cuando el Core vuelva (ver CoreSyncWorker).
public class SolicitudPendiente
{
    public string Metodo { get; set; } = "POST";
    public string Ruta { get; set; } = "";
    public string? CuerpoJson { get; set; }
    public string Origen { get; set; } = "Desconocido";
    public string CodigoTransaccion { get; set; } = "";
    public string Stan { get; set; } = "";
    public DateTime FechaHora { get; set; } = DateTime.Now;
}

// Cola de transacciones pendientes de confirmar con el Core, persistida en disco para que
// sobreviva un reinicio del propio Gateway mientras el Core sigue sin responder. Es la mitad
// "Gateway -> Core" del store-and-forward; la otra mitad ("Caja -> Gateway") vive en
// Patronato.Caja.Services.OfflineQueueService, de forma simétrica e independiente.
public class PendingQueueStore
{
    private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(PendingQueueStore));
    private readonly string _ruta;
    private readonly object _lock = new();
    private List<SolicitudPendiente> _pendientes;

    public PendingQueueStore(IWebHostEnvironment env)
    {
        _ruta = Path.Combine(env.ContentRootPath, "cola-pendientes-core.json");
        _pendientes = Cargar();
    }

    public int Cantidad
    {
        get { lock (_lock) return _pendientes.Count; }
    }

    public void Encolar(SolicitudPendiente solicitud)
    {
        lock (_lock)
        {
            _pendientes.Add(solicitud);
            Guardar();
        }
    }

    public List<SolicitudPendiente> ObtenerTodos()
    {
        lock (_lock) return new List<SolicitudPendiente>(_pendientes);
    }

    public void QuitarPrimero()
    {
        lock (_lock)
        {
            if (_pendientes.Count > 0)
            {
                _pendientes.RemoveAt(0);
                Guardar();
            }
        }
    }

    private List<SolicitudPendiente> Cargar()
    {
        try
        {
            if (File.Exists(_ruta))
                return JsonSerializer.Deserialize<List<SolicitudPendiente>>(File.ReadAllText(_ruta)) ?? new();
        }
        catch (Exception ex)
        {
            Logger.Error("No se pudo leer la cola de pendientes hacia el Core; se empieza vacía.", ex);
        }
        return new();
    }

    private void Guardar()
    {
        try
        {
            File.WriteAllText(_ruta, JsonSerializer.Serialize(_pendientes));
        }
        catch (Exception ex)
        {
            Logger.Error("No se pudo persistir la cola de pendientes hacia el Core.", ex);
        }
    }
}
