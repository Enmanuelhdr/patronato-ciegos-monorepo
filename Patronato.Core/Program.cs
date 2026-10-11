using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;

var builder = WebApplication.CreateBuilder(args);

// 0. Configurar log4net: cada acción y cada error quedan registrados en la tabla
// Logs de patronato.db (ver log4net.config y Entities/LogEntry.cs).
log4net.Config.XmlConfigurator.Configure(new FileInfo(Path.Combine(builder.Environment.ContentRootPath, "log4net.config")));
var accionesLogger = log4net.LogManager.GetLogger("Patronato.Core.Acciones");

// 1. Configurar la Base de Datos SQLite local
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Permitir que la Web y la Caja de tus compañeros se conecten (CORS)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. Habilitar Controladores de API y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Crear la Base de Datos automáticamente al arrancar si no existe
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated(); // ¡Crea patronato.db con todas sus tablas al instante!

    // EnsureCreated no agrega tablas nuevas a una base de datos que ya existía
    // (como esta): se crea aparte la tabla Logs que usa el AdoNetAppender de log4net.
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Logs"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Logs"" PRIMARY KEY AUTOINCREMENT,
            ""Fecha"" TEXT NOT NULL,
            ""Nivel"" TEXT NOT NULL,
            ""Origen"" TEXT NOT NULL,
            ""Logger"" TEXT NOT NULL,
            ""Mensaje"" TEXT NOT NULL,
            ""Excepcion"" TEXT NULL
        );");

    DbInitializer.Initialize(db); // Carga datos iniciales para usuarios, catálogo, masajes y préstamos
}

// 5. Configurar Swagger para pruebas interactivas
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// 6. Bloquear todo lo que no venga del API Gateway: solo él conoce esta clave.
// De paso, registrar cada acción (y sus errores) indicando si vino de Caja o de Web,
// según el header X-Origen que coloca el Gateway (ver CoreGatewayService/ReverseProxyService).
var gatewayApiKey = builder.Configuration["Gateway:ApiKey"];
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var tieneClaveValida = !string.IsNullOrEmpty(gatewayApiKey) &&
            context.Request.Headers.TryGetValue("X-Gateway-Key", out var clave) &&
            clave == gatewayApiKey;

        if (!tieneClaveValida)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Acceso directo al Core no permitido. Use el API Gateway.");
            return;
        }

        var origen = context.Request.Headers.TryGetValue("X-Origen", out var origenHeader)
            ? origenHeader.ToString()
            : "Desconocido";
        log4net.LogicalThreadContext.Properties["Origen"] = origen;

        try
        {
            await next();
            accionesLogger.Info($"{context.Request.Method} {context.Request.Path}{context.Request.QueryString} -> {context.Response.StatusCode}");
        }
        catch (Exception ex)
        {
            accionesLogger.Error($"Error en {context.Request.Method} {context.Request.Path}{context.Request.QueryString}", ex);
            throw;
        }
        return;
    }

    await next();
});

app.UseAuthorization();
app.MapControllers();

app.Run();
