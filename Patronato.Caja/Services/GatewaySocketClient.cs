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

    // Cliente de la entrada por socket del Gateway (ver Patronato.Gateway.Sockets.CajaSocketServer).
    // Toda operación que mueve dinero (cobros, abonos, ventas, donaciones) viaja por aquí usando
    // un código de transacción, igual que la Web lo hace vía REST (/api/gateway/transaccion).
    public class GatewaySocketClient
    {
        private static readonly UTF8Encoding Utf8SinBom = new(encoderShouldEmitUTF8Identifier: false);
        private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);
        private static int _stanCounter = 0;

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
            var conexion = client.ConnectAsync(host, port);
            if (await Task.WhenAny(conexion, Task.Delay(TimeSpan.FromSeconds(5))) != conexion)
                throw new TimeoutException($"No se pudo conectar al Gateway ({host}:{port}).");
            await conexion;

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Utf8SinBom) { AutoFlush = true };
            using var reader = new StreamReader(stream, Utf8SinBom);

            await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOpciones));

            var lecturaTask = reader.ReadLineAsync();
            if (await Task.WhenAny(lecturaTask, Task.Delay(TimeSpan.FromSeconds(10))) != lecturaTask)
                throw new TimeoutException("El Gateway no respondió a tiempo.");

            var linea = await lecturaTask
                ?? throw new IOException("El Gateway cerró la conexión sin responder.");

            return JsonSerializer.Deserialize<TransaccionResponse>(linea, JsonOpciones)
                ?? throw new JsonException("Respuesta vacía del Gateway.");
        }

        private static string GenerarStan() =>
            Interlocked.Increment(ref _stanCounter).ToString("D6");
    }
}
