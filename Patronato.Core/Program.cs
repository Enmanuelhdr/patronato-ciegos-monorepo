using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Monitoreo;
using Patronato.Core.Servicios;

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

// 2b. Autenticación JWT entre el Core y la Web: la Caja no participa de esto (no inicia
// sesión de usuario, solo entra con X-Gateway-Key) — ver Servicios/JwtService.cs.
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Patronato.Core";
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddSingleton<IPasswordHasher<WebUser>, PasswordHasher<WebUser>>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtIssuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        };
    });
builder.Services.AddAuthorization();

// 3. Habilitar Controladores de API y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Los endpoints Minimal API del panel de soporte (Monitor/Operaciones, ver
// Monitoreo/*.cs) reciben/devuelven enums como texto ("Suspendido", no 2) — no afecta
// la serialización de los controladores de api/* (esos usan su propio JsonOptions de MVC).
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Swagger funciona aquí como panel de soporte/mantenimiento: acceso DIRECTO al Core,
// a propósito, saltándose el Gateway. No es un hueco nuevo de seguridad — el botón
// "Authorize" solo pide la misma X-Gateway-Key que ya otorga acceso total vía Gateway;
// esto simplemente le da un segundo cliente a esa credencial para depuración/soporte.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Patronato.Core — Panel de soporte (acceso directo, bypass del Gateway)",
        Version = "v1",
        Description = "Uso exclusivo de soporte/mantenimiento interno. En operación normal, Caja y Web " +
                      "hablan con este Core únicamente a través del API Gateway."
    });

    c.AddSecurityDefinition("GatewayKey", new OpenApiSecurityScheme
    {
        Name = "X-Gateway-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Pega aquí el mismo valor de Gateway:ApiKey para habilitar \"Try it out\" en cada endpoint."
    });
    c.AddSecurityRequirement(doc =>
    {
        var requerimiento = new OpenApiSecurityRequirement();
        requerimiento.Add(new OpenApiSecuritySchemeReference("GatewayKey", doc, null), new List<string>());
        return requerimiento;
    });

    c.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
});

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

app.MapMonitorEndpoints();
app.MapOperacionesEndpoints();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
