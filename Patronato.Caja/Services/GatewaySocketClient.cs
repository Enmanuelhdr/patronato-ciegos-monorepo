using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Patronato.Caja.Services
{
    // Sobre transaccional inspirado en ISO 8583 que usa el Gateway: MTI + STAN para casar
    // petición/respuesta, código de transacción y datos. Debe ser compatible en forma con
    // Patronato.Gateway.Protocol.TransaccionRequest/TransaccionResponse.
    public class TransaccionRequest
    {
        public string Mti { get; set; } = "0200";
        public string Stan { get; set; } = "";
        public string CodigoTransaccion { get; set; } = "";
        public object? Datos { get; set; }
    }

    public class TransaccionResponse
    {
        public string Mti { get; set; } = "0210";
        public string Stan { get; set; } = "";
        public string CodigoRespuesta { get; set; } = "96";
        public string Mensaje { get; set; } = "";
        public JsonElement? Datos { get; set; }
    }

    // Se lanza únicamente cuando la Caja no logró ni conectar con el Gateway (línea caída),
    // nunca cuando el Gateway respondió algo (aunque sea un rechazo de negocio). Es la señal
    // que usa ApiService para decidir si debe encolar la operación localmente (ver
    // OfflineQueueService) en vez de tratarla como un error normal.
    public class GatewayNoDisponibleException : Exception
    {
        public GatewayNoDisponibleException(string mensaje, Exception? inner = null) : base(mensaje, inner) { }
    }

    // Cliente de la entrada por socket del Gateway (ver Patronato.Gateway.Sockets.CajaSocketServer).
    // Toda operación de la Caja —consultas y cobros— viaja por aquí usando un código de
    // transacción, igual que la Web lo hace vía REST (/api/gateway/transaccion).
    public class GatewaySocketClient
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(GatewaySocketClient));
        private static readonly UTF8Encoding Utf8SinBom = new(encoderShouldEmitUTF8Identifier: false);
        private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);
        private static readonly string StanFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PatronatoCaja", "stan_sequence.txt");
        private static readonly object StanLock = new();
        private static int _stanCounter = CargarUltimoStan();

        private static int CargarUltimoStan()
        {
            try
            {
                if (File.Exists(StanFilePath) && int.TryParse(File.ReadAllText(StanFilePath), out var val))
                    return val % 999999;
            }
            catch { }
            return 0;
        }

        private static string GenerarStan()
        {
            lock (StanLock)
            {
                _stanCounter++;
                if (_stanCounter > 999999) _stanCounter = 1;
                try
                {
                    var dir = Path.GetDirectoryName(StanFilePath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(StanFilePath, _stanCounter.ToString());
                }
                catch { }
                return _stanCounter.ToString("D6");
            }
        }

        public async Task<TransaccionResponse> EnviarAsync(string codigoTransaccion, object datos)
        {
            var request = new TransaccionRequest
            {
                Stan = GenerarStan(),
                CodigoTransaccion = codigoTransaccion,
                Datos = datos
            };

            var (host, port) = SessionManager.GetGatewaySocketEndpoint();

            using var client = new TcpClient();
            try
            {
                var conexion = client.ConnectAsync(host, port);
                if (await Task.WhenAny(conexion, Task.Delay(TimeSpan.FromSeconds(3))) != conexion)
                {
                    Logger.Warn($"Tiempo de espera agotado conectando al Gateway ({host}:{port}) para {codigoTransaccion} STAN={request.Stan}.");
                    throw new GatewayNoDisponibleException($"Tiempo de espera agotado conectando al Gateway ({host}:{port}).");
                }
                await conexion;
            }
            catch (SocketException ex)
            {
                Logger.Warn($"No se pudo conectar al Gateway ({host}:{port}) para {codigoTransaccion} STAN={request.Stan}.", ex);
                throw new GatewayNoDisponibleException($"No se pudo conectar al Gateway ({host}:{port}): {ex.Message}", ex);
            }

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Utf8SinBom) { AutoFlush = true };
            using var reader = new StreamReader(stream, Utf8SinBom);

            await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOpciones));

            var lecturaTask = reader.ReadLineAsync();
            if (await Task.WhenAny(lecturaTask, Task.Delay(TimeSpan.FromSeconds(10))) != lecturaTask)
            {
                Logger.Error($"El Gateway no respondió a tiempo para {codigoTransaccion} STAN={request.Stan}.");
                throw new TimeoutException("El Gateway no respondió a tiempo.");
            }

            var linea = await lecturaTask
                ?? throw new IOException("El Gateway cerró la conexión sin responder.");

            return JsonSerializer.Deserialize<TransaccionResponse>(linea, JsonOpciones)
                ?? throw new JsonException("Respuesta vacía del Gateway.");
        }
    }
}
