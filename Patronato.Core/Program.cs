using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;

var builder = WebApplication.CreateBuilder(args);

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
    DbInitializer.Initialize(db); // Carga datos iniciales para usuarios, catálogo, masajes y préstamos
}

// 5. Configurar Swagger para pruebas interactivas
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
