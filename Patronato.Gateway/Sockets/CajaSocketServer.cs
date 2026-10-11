using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Patronato.Gateway.Protocol;
using Patronato.Gateway.Services;

namespace Patronato.Gateway.Sockets;

// Entrada por socket usada por la Caja (WinForms). Cada mensaje es un
// TransaccionRequest en JSON terminado en '\n'; la respuesta llega igual.
public class CajaSocketServer : BackgroundService
{
    private static readonly UTF8Encoding Utf8SinBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);
    private static readonly log4net.ILog SocketLogger = log4net.LogManager.GetLogger(typeof(CajaSocketServer));

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CajaSocketServer> _logger;
    private readonly int _port;

    public CajaSocketServer(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<CajaSocketServer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _port = config.GetValue<int>("CajaSocket:Port", 6000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("Servidor de sockets para Caja escuchando en el puerto {Port}", _port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = ManejarClienteAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task ManejarClienteAsync(TcpClient client, CancellationToken stoppingToken)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Utf8SinBom))
        using (var writer = new StreamWriter(stream, Utf8SinBom) { AutoFlush = true })
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string? linea;
                try
                {
                    linea = await reader.ReadLineAsync(stoppingToken);
                }
                catch (IOException)
                {
                    break;
                }

                if (linea == null)
                    break;

                var respuesta = await ProcesarLineaAsync(linea);
                await writer.WriteLineAsync(JsonSerializer.Serialize(respuesta, JsonOpciones));
            }
        }
    }

    private async Task<TransaccionResponse> ProcesarLineaAsync(string linea)
    {
        log4net.LogicalThreadContext.Properties["Origen"] = "Caja";

        try
        {
            var solicitud = JsonSerializer.Deserialize<TransaccionRequest>(linea, JsonOpciones)
                ?? throw new JsonException("Mensaje vacío.");

            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<CoreGatewayService>();
            return await service.ProcesarAsync(solicitud, "Caja");
        }
        catch (Exception ex)
        {
            SocketLogger.Error($"Mensaje inválido recibido de la Caja: {linea}", ex);
            return new TransaccionResponse { CodigoRespuesta = "96", Mensaje = $"Mensaje inválido: {ex.Message}" };
        }
    }
}
