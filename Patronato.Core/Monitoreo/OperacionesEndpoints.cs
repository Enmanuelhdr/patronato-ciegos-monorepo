using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;

namespace Patronato.Core.Monitoreo
{
    // Panel de soporte: CRUD completo (crear/editar/eliminar, además de los toggles de
    // estado que ya había) sobre las entidades que más mantenimiento manual necesitan,
    // directo sobre el Core, fuera de /api (no pasa por el Gateway ni por el JWT de la
    // Web). Mismo candado que /monitor (X-Admin-Key).
    public static class OperacionesEndpoints
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Patronato.Core.PanelSoporte");

        public static void MapOperacionesEndpoints(this WebApplication app, string? claveAdmin)
        {
            app.MapGet("/monitor/operaciones", () => Results.Content(PaginaHtml, "text/html"));

            app.MapGet("/monitor/operaciones/datos", async (HttpContext ctx, ApplicationDbContext db) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";

                var usuarios = await db.Usuarios.OrderBy(u => u.NombreCompleto).Select(u => new
                {
                    u.Id, u.NombreCompleto, u.NombreUsuario, u.Correo, u.SucursalId, u.Activo,
                    rol = (int)u.Rol, rolTexto = u.Rol.ToString(),
                }).ToListAsync();

                var sucursales = await db.Sucursales.OrderBy(s => s.Nombre)
                    .Select(s => new { s.Id, s.Nombre, s.Direccion, s.Telefono, s.Encargado, s.Activa })
                    .ToListAsync();

                var productos = await db.ProductosArticulos.OrderBy(p => p.Nombre)
                    .Select(p => new { p.Id, p.Codigo, p.Nombre, p.Categoria, p.PrecioVenta, p.StockDisponible })
                    .ToListAsync();

                var beneficiarios = await db.Beneficiarios.OrderBy(b => b.Nombres)
                    .Select(b => new { b.Id, b.Nombres, b.Apellidos, b.Cedula, b.Telefono, b.Activo })
                    .ToListAsync();

                return Results.Json(new { usuarios, sucursales, productos, beneficiarios });
            });

            app.MapPost("/monitor/operaciones/usuarios/{id:int}/activo", async (HttpContext ctx, ApplicationDbContext db, int id, CambiarActivoRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var usuario = await db.Usuarios.FindAsync(id);
                if (usuario == null) return Results.Json(new { mensaje = "Usuario no encontrado." }, statusCode: 404);

                usuario.Activo = body.Activo;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Usuario #{id} ({usuario.NombreUsuario}) marcado como {(body.Activo ? "ACTIVO" : "INACTIVO")}.");
                return Results.Json(new { mensaje = $"Usuario {(body.Activo ? "activado" : "desactivado")} correctamente." });
            });

            app.MapPost("/monitor/operaciones/usuarios/{id:int}/rol", async (HttpContext ctx, ApplicationDbContext db, int id, CambiarRolRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                if (!Enum.IsDefined(typeof(RolUsuario), body.Rol)) return Results.Json(new { mensaje = "Rol inválido." }, statusCode: 400);

                var usuario = await db.Usuarios.FindAsync(id);
                if (usuario == null) return Results.Json(new { mensaje = "Usuario no encontrado." }, statusCode: 404);

                var rolAnterior = usuario.Rol;
                usuario.Rol = (RolUsuario)body.Rol;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Usuario #{id} ({usuario.NombreUsuario}) cambió de rol {rolAnterior} -> {usuario.Rol}.");
                return Results.Json(new { mensaje = $"Rol actualizado a {usuario.Rol}." });
            });

            app.MapPost("/monitor/operaciones/sucursales/{id:int}/activa", async (HttpContext ctx, ApplicationDbContext db, int id, CambiarActivaRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var sucursal = await db.Sucursales.FindAsync(id);
                if (sucursal == null) return Results.Json(new { mensaje = "Sucursal no encontrada." }, statusCode: 404);

                sucursal.Activa = body.Activa;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Sucursal #{id} ({sucursal.Nombre}) marcada como {(body.Activa ? "ACTIVA" : "INACTIVA")}.");
                return Results.Json(new { mensaje = $"Sucursal {(body.Activa ? "activada" : "desactivada")} correctamente." });
            });

            app.MapPost("/monitor/operaciones/beneficiarios/{id:int}/activo", async (HttpContext ctx, ApplicationDbContext db, int id, CambiarActivoRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var beneficiario = await db.Beneficiarios.FindAsync(id);
                if (beneficiario == null) return Results.Json(new { mensaje = "Beneficiario no encontrado." }, statusCode: 404);

                beneficiario.Activo = body.Activo;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Beneficiario #{id} ({beneficiario.Nombres} {beneficiario.Apellidos}) marcado como {(body.Activo ? "ACTIVO" : "INACTIVO")}.");
                return Results.Json(new { mensaje = $"Beneficiario {(body.Activo ? "activado" : "desactivado")} correctamente." });
            });

            app.MapPost("/monitor/operaciones/productos/{id:int}/ajustar-stock", async (HttpContext ctx, ApplicationDbContext db, int id, AjustarStockRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var producto = await db.ProductosArticulos.FindAsync(id);
                if (producto == null) return Results.Json(new { mensaje = "Producto no encontrado." }, statusCode: 404);

                try
                {
                    if (body.Delta > 0) producto.AgregarStock(body.Delta);
                    else if (body.Delta < 0) producto.DescontarStock(-body.Delta);
                    else return Results.Json(new { mensaje = "El ajuste no puede ser cero." }, statusCode: 400);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { mensaje = ex.Message }, statusCode: 400);
                }

                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Producto #{id} ({producto.Nombre}) ajuste de stock {(body.Delta > 0 ? "+" : "")}{body.Delta} -> nuevo stock {producto.StockDisponible}.");
                return Results.Json(new { mensaje = "Stock ajustado.", nuevoStock = producto.StockDisponible });
            });

            // ================= CRUD: Usuarios =================
            app.MapPost("/monitor/operaciones/usuarios", async (HttpContext ctx, ApplicationDbContext db, CrearUsuarioRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                if (!Enum.IsDefined(typeof(RolUsuario), body.Rol)) return Results.Json(new { mensaje = "Rol inválido." }, statusCode: 400);

                var usuario = new Usuario
                {
                    NombreCompleto = body.NombreCompleto,
                    NombreUsuario = body.NombreUsuario,
                    Correo = body.Correo,
                    PasswordHash = body.PasswordHash,
                    Rol = (RolUsuario)body.Rol,
                    SucursalId = body.SucursalId,
                    Activo = true
                };
                db.Usuarios.Add(usuario);
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Usuario creado: '{usuario.NombreUsuario}' ({usuario.Rol}).");
                return Results.Json(new { mensaje = "Usuario creado correctamente.", id = usuario.Id });
            });

            app.MapPut("/monitor/operaciones/usuarios/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id, EditarUsuarioRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var usuario = await db.Usuarios.FindAsync(id);
                if (usuario == null) return Results.Json(new { mensaje = "Usuario no encontrado." }, statusCode: 404);

                usuario.NombreCompleto = body.NombreCompleto;
                usuario.Correo = body.Correo;
                usuario.SucursalId = body.SucursalId;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Usuario #{id} ({usuario.NombreUsuario}) editado.");
                return Results.Json(new { mensaje = "Usuario actualizado." });
            });

            app.MapDelete("/monitor/operaciones/usuarios/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var usuario = await db.Usuarios.FindAsync(id);
                if (usuario == null) return Results.Json(new { mensaje = "Usuario no encontrado." }, statusCode: 404);

                db.Usuarios.Remove(usuario);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Results.Json(new { mensaje = "No se pudo eliminar: el usuario tiene registros relacionados. Desactívalo en su lugar." }, statusCode: 400);
                }
                Logger.Warn($"[Panel de soporte] Usuario #{id} ({usuario.NombreUsuario}) eliminado permanentemente.");
                return Results.Json(new { mensaje = "Usuario eliminado." });
            });

            // ================= CRUD: Sucursales =================
            app.MapPost("/monitor/operaciones/sucursales", async (HttpContext ctx, ApplicationDbContext db, CrearSucursalRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";

                var sucursal = new Sucursal
                {
                    Nombre = body.Nombre,
                    Direccion = body.Direccion,
                    Telefono = body.Telefono,
                    Encargado = body.Encargado,
                    Activa = true
                };
                db.Sucursales.Add(sucursal);
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Sucursal creada: '{sucursal.Nombre}'.");
                return Results.Json(new { mensaje = "Sucursal creada correctamente.", id = sucursal.Id });
            });

            app.MapPut("/monitor/operaciones/sucursales/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id, EditarSucursalRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var sucursal = await db.Sucursales.FindAsync(id);
                if (sucursal == null) return Results.Json(new { mensaje = "Sucursal no encontrada." }, statusCode: 404);

                sucursal.Nombre = body.Nombre;
                sucursal.Direccion = body.Direccion;
                sucursal.Telefono = body.Telefono;
                sucursal.Encargado = body.Encargado;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Sucursal #{id} editada.");
                return Results.Json(new { mensaje = "Sucursal actualizada." });
            });

            app.MapDelete("/monitor/operaciones/sucursales/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var sucursal = await db.Sucursales.FindAsync(id);
                if (sucursal == null) return Results.Json(new { mensaje = "Sucursal no encontrada." }, statusCode: 404);

                db.Sucursales.Remove(sucursal);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Results.Json(new { mensaje = "No se pudo eliminar: hay empleados u otros registros asignados a esta sucursal. Desactívala en su lugar." }, statusCode: 400);
                }
                Logger.Warn($"[Panel de soporte] Sucursal #{id} ({sucursal.Nombre}) eliminada permanentemente.");
                return Results.Json(new { mensaje = "Sucursal eliminada." });
            });

            // ================= CRUD: Productos (Inventario) =================
            app.MapPost("/monitor/operaciones/productos", async (HttpContext ctx, ApplicationDbContext db, CrearProductoRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";

                var producto = new ProductoArticulo
                {
                    Codigo = body.Codigo,
                    Nombre = body.Nombre,
                    Categoria = body.Categoria,
                    PrecioVenta = body.PrecioVenta,
                    StockDisponible = body.StockDisponible
                };
                db.ProductosArticulos.Add(producto);
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Producto creado: '{producto.Codigo}' - {producto.Nombre}.");
                return Results.Json(new { mensaje = "Producto creado correctamente.", id = producto.Id });
            });

            app.MapPut("/monitor/operaciones/productos/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id, EditarProductoRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var producto = await db.ProductosArticulos.FindAsync(id);
                if (producto == null) return Results.Json(new { mensaje = "Producto no encontrado." }, statusCode: 404);

                producto.Codigo = body.Codigo;
                producto.Nombre = body.Nombre;
                producto.Categoria = body.Categoria;
                producto.PrecioVenta = body.PrecioVenta;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Producto #{id} editado.");
                return Results.Json(new { mensaje = "Producto actualizado." });
            });

            app.MapDelete("/monitor/operaciones/productos/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var producto = await db.ProductosArticulos.FindAsync(id);
                if (producto == null) return Results.Json(new { mensaje = "Producto no encontrado." }, statusCode: 404);

                db.ProductosArticulos.Remove(producto);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Results.Json(new { mensaje = "No se pudo eliminar: el producto tiene ventas registradas." }, statusCode: 400);
                }
                Logger.Warn($"[Panel de soporte] Producto #{id} ({producto.Codigo}) eliminado permanentemente.");
                return Results.Json(new { mensaje = "Producto eliminado." });
            });

            // ================= CRUD: Beneficiarios =================
            app.MapPost("/monitor/operaciones/beneficiarios", async (HttpContext ctx, ApplicationDbContext db, CrearBeneficiarioRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";

                var beneficiario = new Beneficiario
                {
                    Nombres = body.Nombres,
                    Apellidos = body.Apellidos,
                    Cedula = body.Cedula,
                    Telefono = body.Telefono,
                    SedeAsignada = (SedeEnum_REVIEW)body.SedeAsignada,
                    CondicionVisual = (TipoDiscapacidadVisual)body.CondicionVisual,
                    Activo = true
                };
                db.Beneficiarios.Add(beneficiario);
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Beneficiario creado: '{beneficiario.Nombres} {beneficiario.Apellidos}'.");
                return Results.Json(new { mensaje = "Beneficiario creado correctamente.", id = beneficiario.Id });
            });

            app.MapPut("/monitor/operaciones/beneficiarios/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id, EditarBeneficiarioRequest body) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var beneficiario = await db.Beneficiarios.FindAsync(id);
                if (beneficiario == null) return Results.Json(new { mensaje = "Beneficiario no encontrado." }, statusCode: 404);

                beneficiario.Nombres = body.Nombres;
                beneficiario.Apellidos = body.Apellidos;
                beneficiario.Cedula = body.Cedula;
                beneficiario.Telefono = body.Telefono;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Beneficiario #{id} editado.");
                return Results.Json(new { mensaje = "Beneficiario actualizado." });
            });

            app.MapDelete("/monitor/operaciones/beneficiarios/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id) =>
            {
                if (!AdminAuth.EsValida(ctx, claveAdmin)) return Results.Json(new { mensaje = "Clave inválida." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var beneficiario = await db.Beneficiarios.FindAsync(id);
                if (beneficiario == null) return Results.Json(new { mensaje = "Beneficiario no encontrado." }, statusCode: 404);

                db.Beneficiarios.Remove(beneficiario);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Results.Json(new { mensaje = "No se pudo eliminar: el beneficiario tiene consultas, préstamos o inscripciones asociadas. Desactívalo en su lugar." }, statusCode: 400);
                }
                Logger.Warn($"[Panel de soporte] Beneficiario #{id} eliminado permanentemente.");
                return Results.Json(new { mensaje = "Beneficiario eliminado." });
            });
        }

        private record CambiarActivoRequest(bool Activo);
        private record CambiarActivaRequest(bool Activa);
        private record CambiarRolRequest(int Rol);
        private record AjustarStockRequest(int Delta);

        private record CrearUsuarioRequest(string NombreCompleto, string NombreUsuario, string Correo, string PasswordHash, int Rol, int SucursalId);
        private record EditarUsuarioRequest(string NombreCompleto, string Correo, int SucursalId);

        private record CrearSucursalRequest(string Nombre, string Direccion, string Telefono, string Encargado);
        private record EditarSucursalRequest(string Nombre, string Direccion, string Telefono, string Encargado);

        private record CrearProductoRequest(string Codigo, string Nombre, string Categoria, decimal PrecioVenta, int StockDisponible);
        private record EditarProductoRequest(string Codigo, string Nombre, string Categoria, decimal PrecioVenta);

        private record CrearBeneficiarioRequest(string Nombres, string Apellidos, string Cedula, string Telefono, int SedeAsignada, int CondicionVisual);
        private record EditarBeneficiarioRequest(string Nombres, string Apellidos, string Cedula, string Telefono);

        private const string PaginaHtml = """
<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Operaciones — Patronato.Core</title>
<style>
  :root { color-scheme: dark; }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: "Segoe UI", system-ui, sans-serif; background: #0b1120; color: #e2e8f0; }
  header { display: flex; align-items: center; justify-content: space-between; padding: 14px 22px; background: #111827; border-bottom: 1px solid #1f2937; }
  header h1 { margin: 0; font-size: 16px; }
  header small { color: #94a3b8; }
  main { padding: 20px; max-width: 1200px; margin: 0 auto; }
  .login { max-width: 360px; margin: 80px auto; background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 24px; text-align: center; }
  .login input { width: 100%; padding: 10px; margin: 12px 0; border-radius: 6px; border: 1px solid #334155; background: #0b1120; color: #e2e8f0; }
  .login button, button { background: #2563eb; color: white; border: none; padding: 7px 14px; border-radius: 6px; cursor: pointer; font-size: 12px; }
  .login button:hover, button:hover { background: #1d4ed8; }
  button.peligro { background: #7f1d1d; }
  button.peligro:hover { background: #991b1b; }
  button.exito { background: #065f46; }
  button.exito:hover { background: #047857; }
  .oculto { display: none; }
  .panel { background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 16px; margin-bottom: 20px; }
  .panel h2 { margin: 0 0 12px; font-size: 14px; }
  table { width: 100%; border-collapse: collapse; font-size: 12px; }
  th { text-align: left; padding: 8px; border-bottom: 1px solid #1f2937; color: #94a3b8; font-weight: 600; }
  td { padding: 8px; border-bottom: 1px solid #1a2332; vertical-align: middle; }
  select, input.stock { background: #0b1120; color: #e2e8f0; border: 1px solid #334155; border-radius: 6px; padding: 5px 8px; font-size: 12px; }
  input.stock { width: 70px; }
  .tag { padding: 2px 8px; border-radius: 10px; font-size: 11px; font-weight: 700; }
  .tag-ok { background: #065f46; color: #6ee7b7; }
  .tag-mal { background: #450a0a; color: #f87171; }
  .accion { display: flex; gap: 6px; align-items: center; }
  #mensaje { position: fixed; top: 16px; right: 16px; padding: 10px 16px; border-radius: 8px; font-size: 13px; z-index: 10; }
  #mensaje.ok { background: #065f46; color: #d1fae5; }
  #mensaje.error { background: #450a0a; color: #fecaca; }
</style>
</head>
<body>

<div id="login" class="login">
  <h2>🔒 Operaciones del Core</h2>
  <p style="color:#94a3b8;font-size:13px;">Acceso directo de soporte/mantenimiento. Pega la clave del Gateway (X-Admin-Key).</p>
  <input id="clave" type="password" placeholder="Clave de administración" />
  <button onclick="conectar()">Entrar</button>
  <p id="loginError" style="color:#f87171;font-size:12px;"></p>
</div>

<div id="app" class="oculto">
  <header>
    <div>
      <h1>Patronato.Core · Operaciones</h1>
      <small>Acceso directo, fuera del Gateway — acciones administrativas puntuales</small>
    </div>
    <a href="/monitor" style="color:#60a5fa;text-decoration:none;font-size:13px;">← Monitor de logs</a>
  </header>

  <main>
    <div class="panel">
      <h2>Usuarios / Empleados <button onclick="crearUsuario()">+ Nuevo</button></h2>
      <table><thead><tr><th>Nombre</th><th>Usuario</th><th>Correo</th><th>Rol</th><th>Sucursal</th><th>Estado</th><th>Acciones</th></tr></thead>
      <tbody id="tablaUsuarios"></tbody></table>
    </div>

    <div class="panel">
      <h2>Sucursales <button onclick="crearSucursal()">+ Nueva</button></h2>
      <table><thead><tr><th>Nombre</th><th>Dirección</th><th>Teléfono</th><th>Encargado</th><th>Estado</th><th>Acciones</th></tr></thead>
      <tbody id="tablaSucursales"></tbody></table>
    </div>

    <div class="panel">
      <h2>Inventario <button onclick="crearProducto()">+ Nuevo</button></h2>
      <table><thead><tr><th>Código</th><th>Nombre</th><th>Categoría</th><th>Precio</th><th>Stock</th><th>Ajustar</th><th>Acciones</th></tr></thead>
      <tbody id="tablaProductos"></tbody></table>
    </div>

    <div class="panel">
      <h2>Beneficiarios <button onclick="crearBeneficiario()">+ Nuevo</button></h2>
      <table><thead><tr><th>Nombre</th><th>Cédula</th><th>Teléfono</th><th>Estado</th><th>Acciones</th></tr></thead>
      <tbody id="tablaBeneficiarios"></tbody></table>
    </div>
  </main>
</div>
<div id="mensaje" class="oculto"></div>

<script>
let clave = sessionStorage.getItem('adminKey') || '';
const ROLES = { 1: 'Administrador', 2: 'Cajero', 3: 'MedicoOftalmologo', 4: 'InstructorRehabilitacion', 5: 'Recepcionista' };

if (clave) conectar(true);

function conectar(silencioso) {
  if (!silencioso) clave = document.getElementById('clave').value.trim();
  if (!clave) return;
  fetch('/monitor/operaciones/datos', { headers: { 'X-Admin-Key': clave } }).then(async r => {
    if (r.status === 401) {
      sessionStorage.removeItem('adminKey');
      document.getElementById('loginError').textContent = 'Clave incorrecta.';
      return;
    }
    sessionStorage.setItem('adminKey', clave);
    document.getElementById('login').classList.add('oculto');
    document.getElementById('app').classList.remove('oculto');
    pintar(await r.json());
  }).catch(() => { document.getElementById('loginError').textContent = 'No se pudo conectar al Core.'; });
}

function cargar() {
  fetch('/monitor/operaciones/datos', { headers: { 'X-Admin-Key': clave } }).then(async r => {
    if (r.status === 401) { location.reload(); return; }
    pintar(await r.json());
  });
}

function mostrarMensaje(texto, esError) {
  const el = document.getElementById('mensaje');
  el.textContent = texto;
  el.className = esError ? 'error' : 'ok';
  setTimeout(() => el.classList.add('oculto'), 3000);
}

async function enviar(ruta, body, metodo) {
  const r = await fetch(ruta, {
    method: metodo || 'POST',
    headers: { 'X-Admin-Key': clave, 'Content-Type': 'application/json' },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const datos = await r.json().catch(() => ({}));
  mostrarMensaje(datos.mensaje || (r.ok ? 'Listo.' : 'Error.'), !r.ok);
  if (r.ok) cargar();
  return r.ok;
}

function toggleUsuario(id, activo) { enviar(`/monitor/operaciones/usuarios/${id}/activo`, { activo: !activo }); }
function cambiarRol(id, select) { enviar(`/monitor/operaciones/usuarios/${id}/rol`, { rol: Number(select.value) }); }
function toggleSucursal(id, activa) { enviar(`/monitor/operaciones/sucursales/${id}/activa`, { activa: !activa }); }
function toggleBeneficiario(id, activo) { enviar(`/monitor/operaciones/beneficiarios/${id}/activo`, { activo: !activo }); }
function ajustarStock(id, input) {
  const delta = Number(input.value);
  if (!delta) return mostrarMensaje('Indica una cantidad distinta de cero.', true);
  enviar(`/monitor/operaciones/productos/${id}/ajustar-stock`, { delta });
  input.value = '';
}

// ---- CRUD rápido vía prompt(): este panel es de soporte/mantenimiento puntual, no
// pretende ser una pantalla bonita — prioriza que cualquier campo se pueda arreglar rápido.
function crearUsuario() {
  const nombreCompleto = prompt('Nombre completo:'); if (!nombreCompleto) return;
  const nombreUsuario = prompt('Usuario (login):'); if (!nombreUsuario) return;
  const correo = prompt('Correo:') || '';
  const passwordHash = prompt('Contraseña inicial:'); if (!passwordHash) return;
  const rol = prompt('Rol (1 Administrador, 2 Cajero, 3 MedicoOftalmologo, 4 InstructorRehabilitacion, 5 Recepcionista):', '2');
  const sucursalId = prompt('ID de sucursal:', '1');
  enviar('/monitor/operaciones/usuarios', { nombreCompleto, nombreUsuario, correo, passwordHash, rol: Number(rol), sucursalId: Number(sucursalId) });
}
function editarUsuario(u) {
  const nombreCompleto = prompt('Nombre completo:', u.nombreCompleto); if (!nombreCompleto) return;
  const correo = prompt('Correo:', u.correo) || '';
  const sucursalId = prompt('ID de sucursal:', u.sucursalId);
  enviar(`/monitor/operaciones/usuarios/${u.id}`, { nombreCompleto, correo, sucursalId: Number(sucursalId) }, 'PUT');
}
function eliminarUsuario(id, nombre) {
  if (confirm(`¿Eliminar permanentemente a "${nombre}"? Esta acción no se puede deshacer.`)) {
    enviar(`/monitor/operaciones/usuarios/${id}`, undefined, 'DELETE');
  }
}

function crearSucursal() {
  const nombre = prompt('Nombre de la sucursal:'); if (!nombre) return;
  const direccion = prompt('Dirección:') || '';
  const telefono = prompt('Teléfono:') || '';
  const encargado = prompt('Encargado:') || '';
  enviar('/monitor/operaciones/sucursales', { nombre, direccion, telefono, encargado });
}
function editarSucursal(s) {
  const nombre = prompt('Nombre:', s.nombre); if (!nombre) return;
  const direccion = prompt('Dirección:', s.direccion) || '';
  const telefono = prompt('Teléfono:', s.telefono) || '';
  const encargado = prompt('Encargado:', s.encargado) || '';
  enviar(`/monitor/operaciones/sucursales/${s.id}`, { nombre, direccion, telefono, encargado }, 'PUT');
}
function eliminarSucursal(id, nombre) {
  if (confirm(`¿Eliminar permanentemente la sucursal "${nombre}"?`)) {
    enviar(`/monitor/operaciones/sucursales/${id}`, undefined, 'DELETE');
  }
}

function crearProducto() {
  const codigo = prompt('Código:'); if (!codigo) return;
  const nombre = prompt('Nombre:'); if (!nombre) return;
  const categoria = prompt('Categoría:') || '';
  const precioVenta = prompt('Precio de venta:', '0');
  const stockDisponible = prompt('Stock inicial:', '0');
  enviar('/monitor/operaciones/productos', { codigo, nombre, categoria, precioVenta: Number(precioVenta), stockDisponible: Number(stockDisponible) });
}
function editarProducto(p) {
  const codigo = prompt('Código:', p.codigo); if (!codigo) return;
  const nombre = prompt('Nombre:', p.nombre); if (!nombre) return;
  const categoria = prompt('Categoría:', p.categoria) || '';
  const precioVenta = prompt('Precio de venta:', p.precioVenta);
  enviar(`/monitor/operaciones/productos/${p.id}`, { codigo, nombre, categoria, precioVenta: Number(precioVenta) }, 'PUT');
}
function eliminarProducto(id, nombre) {
  if (confirm(`¿Eliminar permanentemente el producto "${nombre}"?`)) {
    enviar(`/monitor/operaciones/productos/${id}`, undefined, 'DELETE');
  }
}

function crearBeneficiario() {
  const nombres = prompt('Nombres:'); if (!nombres) return;
  const apellidos = prompt('Apellidos:'); if (!apellidos) return;
  const cedula = prompt('Cédula:') || '';
  const telefono = prompt('Teléfono:') || '';
  const sedeAsignada = prompt('Sede (1 Santo Domingo, 2 Santiago, 3 Barahona):', '1');
  const condicionVisual = prompt('Condición visual (1 Baja Visión, 2 Ceguera Congénita, 3 Ceguera Adquirida):', '1');
  enviar('/monitor/operaciones/beneficiarios', { nombres, apellidos, cedula, telefono, sedeAsignada: Number(sedeAsignada), condicionVisual: Number(condicionVisual) });
}
function editarBeneficiario(b) {
  const nombres = prompt('Nombres:', b.nombres); if (!nombres) return;
  const apellidos = prompt('Apellidos:', b.apellidos); if (!apellidos) return;
  const cedula = prompt('Cédula:', b.cedula) || '';
  const telefono = prompt('Teléfono:', b.telefono) || '';
  enviar(`/monitor/operaciones/beneficiarios/${b.id}`, { nombres, apellidos, cedula, telefono }, 'PUT');
}
function eliminarBeneficiario(id, nombre) {
  if (confirm(`¿Eliminar permanentemente a "${nombre}"? Esta acción no se puede deshacer.`)) {
    enviar(`/monitor/operaciones/beneficiarios/${id}`, undefined, 'DELETE');
  }
}

let ultimoDato = { usuarios: [], sucursales: [], productos: [], beneficiarios: [] };

function pintar(d) {
  ultimoDato = d;

  document.getElementById('tablaUsuarios').innerHTML = d.usuarios.map(u => `
    <tr>
      <td>${u.nombreCompleto}</td>
      <td>${u.nombreUsuario}</td>
      <td>${u.correo ?? ''}</td>
      <td><select onchange="cambiarRol(${u.id}, this)">
        ${Object.entries(ROLES).map(([v, t]) => `<option value="${v}" ${Number(v) === u.rol ? 'selected' : ''}>${t}</option>`).join('')}
      </select></td>
      <td>#${u.sucursalId}</td>
      <td><span class="tag ${u.activo ? 'tag-ok' : 'tag-mal'}">${u.activo ? 'Activo' : 'Inactivo'}</span></td>
      <td class="accion">
        <button onclick="editarUsuario(ultimoDato.usuarios.find(x=>x.id===${u.id}))">Editar</button>
        <button class="${u.activo ? 'peligro' : 'exito'}" onclick="toggleUsuario(${u.id}, ${u.activo})">${u.activo ? 'Desactivar' : 'Activar'}</button>
        <button class="peligro" onclick="eliminarUsuario(${u.id}, '${(u.nombreCompleto || '').replace(/'/g, "\\'")}')">Eliminar</button>
      </td>
    </tr>`).join('');

  document.getElementById('tablaSucursales').innerHTML = d.sucursales.map(s => `
    <tr>
      <td>${s.nombre}</td><td>${s.direccion}</td><td>${s.telefono}</td><td>${s.encargado}</td>
      <td><span class="tag ${s.activa ? 'tag-ok' : 'tag-mal'}">${s.activa ? 'Activa' : 'Inactiva'}</span></td>
      <td class="accion">
        <button onclick="editarSucursal(ultimoDato.sucursales.find(x=>x.id===${s.id}))">Editar</button>
        <button class="${s.activa ? 'peligro' : 'exito'}" onclick="toggleSucursal(${s.id}, ${s.activa})">${s.activa ? 'Desactivar' : 'Activar'}</button>
        <button class="peligro" onclick="eliminarSucursal(${s.id}, '${(s.nombre || '').replace(/'/g, "\\'")}')">Eliminar</button>
      </td>
    </tr>`).join('');

  document.getElementById('tablaProductos').innerHTML = d.productos.map(p => `
    <tr>
      <td>${p.codigo}</td><td>${p.nombre}</td><td>${p.categoria}</td>
      <td>RD$ ${p.precioVenta.toFixed(2)}</td><td>${p.stockDisponible}</td>
      <td class="accion"><input class="stock" type="number" placeholder="±cant." id="stock-${p.id}" />
        <button class="exito" onclick="ajustarStock(${p.id}, document.getElementById('stock-${p.id}'))">Ajustar</button></td>
      <td class="accion">
        <button onclick="editarProducto(ultimoDato.productos.find(x=>x.id===${p.id}))">Editar</button>
        <button class="peligro" onclick="eliminarProducto(${p.id}, '${(p.nombre || '').replace(/'/g, "\\'")}')">Eliminar</button>
      </td>
    </tr>`).join('');

  document.getElementById('tablaBeneficiarios').innerHTML = d.beneficiarios.map(b => `
    <tr>
      <td>${b.nombres} ${b.apellidos}</td><td>${b.cedula}</td><td>${b.telefono}</td>
      <td><span class="tag ${b.activo ? 'tag-ok' : 'tag-mal'}">${b.activo ? 'Activo' : 'Inactivo'}</span></td>
      <td class="accion">
        <button onclick="editarBeneficiario(ultimoDato.beneficiarios.find(x=>x.id===${b.id}))">Editar</button>
        <button class="${b.activo ? 'peligro' : 'exito'}" onclick="toggleBeneficiario(${b.id}, ${b.activo})">${b.activo ? 'Desactivar' : 'Activar'}</button>
        <button class="peligro" onclick="eliminarBeneficiario(${b.id}, '${(b.nombres || '').replace(/'/g, "\\'")}')">Eliminar</button>
      </td>
    </tr>`).join('');
}
</script>
</body>
</html>
""";
    }
}
