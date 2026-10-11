using Patronato.Gateway.Services;
using Patronato.Gateway.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Configurar log4net: log a nivel de archivo (ver log4net.config) para todo lo
// que entra por HTTP (Web) o por socket (Caja, ver Sockets/CajaSocketServer).
// LogRoot ancla la ruta del log al proyecto, sin importar desde dónde se ejecute el host.
log4net.GlobalContext.Properties["LogRoot"] = builder.Environment.ContentRootPath;
log4net.Config.XmlConfigurator.Configure(new FileInfo(Path.Combine(builder.Environment.ContentRootPath, "log4net.config")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Permite que la Web (React) llame al Gateway desde el navegador. La Caja no la necesita
// (entra por socket), pero la Web sí: es un cliente JavaScript corriendo en otro origen.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

void ConfigurarClienteCore(HttpClient client)
{
    var baseUrl = builder.Configuration["CoreApi:BaseUrl"] ?? "http://localhost:57141";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    // Única credencial que el Core acepta: sin esto, el Core rechaza la petición.
    var apiKey = builder.Configuration["CoreApi:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("X-Gateway-Key", apiKey);
}

builder.Services.AddSingleton<PendingQueueStore>();
builder.Services.AddHttpClient<CoreGatewayService>(ConfigurarClienteCore);
builder.Services.AddHttpClient<ReverseProxyService>(ConfigurarClienteCore);

builder.Services.AddHostedService<CajaSocketServer>();
// Cuando el Core está caído, las transacciones de cobro quedan en PendingQueueStore;
// este worker las reenvía automáticamente en cuanto el Core vuelve a responder.
builder.Services.AddHostedService<CoreSyncWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// Middleware: marca el origen "Web" para todo lo que entra por HTTP (API Gateway REST
// y el reverse proxy hacia el Core) y registra la acción y cualquier error en el log.
// La Caja no pasa por aquí: llega por socket y se registra en CajaSocketServer.
var httpLogger = log4net.LogManager.GetLogger("Patronato.Gateway.Http");
app.Use(async (context, next) =>
{
    // La Caja también entra por HTTP para sus consultas de solo lectura (ver ReverseProxyService);
    // respeta el X-Origen que ella misma coloca en vez de asumir siempre "Web".
    var origen = context.Request.Headers.TryGetValue("X-Origen", out var origenHeader) && !string.IsNullOrWhiteSpace(origenHeader)
        ? origenHeader.ToString()
        : "Web";
    log4net.LogicalThreadContext.Properties["Origen"] = origen;

    try
    {
        await next();
        httpLogger.Info($"{context.Request.Method} {context.Request.Path}{context.Request.QueryString} -> {context.Response.StatusCode}");
    }
    catch (Exception ex)
    {
        httpLogger.Error($"Error en {context.Request.Method} {context.Request.Path}{context.Request.QueryString}", ex);
        throw;
    }
});

app.MapControllers();

// Cualquier /api/* que no sea /api/gateway/transaccion se reenvía tal cual al Core.
app.MapFallback("/api/{**path}", async (HttpContext context, ReverseProxyService proxy) =>
    await proxy.ProxyAsync(context));

app.Run();
