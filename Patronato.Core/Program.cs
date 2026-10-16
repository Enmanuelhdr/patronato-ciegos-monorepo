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

// 1. Configurar la Base de Datos SQLite local con ruta absoluta garantizada
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=patronato.db";
string sqliteConn = rawConn;
if (rawConn.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    var dbFileName = rawConn.Substring("Data Source=".Length).Trim();
    if (!Path.IsPathRooted(dbFileName))
    {
        var contentRootDb = Path.Combine(builder.Environment.ContentRootPath, dbFileName);
        var baseDirDb = Path.Combine(AppContext.BaseDirectory, dbFileName);
        var resolvedPath = contentRootDb;
        if (!File.Exists(contentRootDb) && File.Exists(baseDirDb))
        {
            resolvedPath = baseDirDb;
        }
        sqliteConn = $"Data Source={resolvedPath}";
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteConn));

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
builder.Services.AddSingleton<IEmailService, EmailService>();
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

// 5. Configurar Swagger para pruebas interactivas (solo en desarrollo)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Helper para evitar escribir contraseñas y secretos en la tabla Logs
static string SanitizarQuery(QueryString queryString)
{
    if (!queryString.HasValue) return string.Empty;
    var parsed = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString.Value);
    var clean = new List<string>();
    foreach (var (k, v) in parsed)
    {
        if (k.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("clave", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("cvv", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("tarjeta", StringComparison.OrdinalIgnoreCase))
        {
            clean.Add($"{Uri.EscapeDataString(k)}=***REDACTED***");
        }
        else
        {
            clean.Add($"{Uri.EscapeDataString(k)}={Uri.EscapeDataString(v.ToString())}");
        }
    }
    return clean.Count > 0 ? "?" + string.Join("&", clean) : string.Empty;
}

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

        var urlSegura = $"{context.Request.Path}{SanitizarQuery(context.Request.QueryString)}";

        try
        {
            await next();
            accionesLogger.Info($"{context.Request.Method} {urlSegura} -> {context.Response.StatusCode}");
        }
        catch (Exception ex)
        {
            accionesLogger.Error($"Error en {context.Request.Method} {urlSegura}", ex);
            throw;
        }
        return;
    }

    await next();
});

app.MapMonitorEndpoints();
app.MapOperacionesEndpoints();

app.UseAuthentication();

// 7. Puente de autenticación interna para la terminal de Caja y el Gateway
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Gateway-Key", out var clave) && clave == gatewayApiKey)
    {
        var origen = context.Request.Headers.TryGetValue("X-Origen", out var o) ? o.ToString() : "Caja";
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, "1"),
            new(System.Security.Claims.ClaimTypes.Name, "Terminal-Caja"),
            new(System.Security.Claims.ClaimTypes.Role, "Administrador"),
            new(System.Security.Claims.ClaimTypes.Role, "Cajero"),
            new("ctx", "core"),
            new("origen", origen)
        };
        foreach (var p in PermisosCore.PermisosAdministrador)
        {
            claims.Add(new("permiso", p));
        }
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "GatewayCajaInternal");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
    }
    await next();
});

app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect(app.Environment.IsDevelopment() ? "/swagger" : "/monitor"));

app.Run();
