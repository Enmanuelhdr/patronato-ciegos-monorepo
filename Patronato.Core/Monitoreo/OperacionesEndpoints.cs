using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using Patronato.Core.Servicios;

namespace Patronato.Core.Monitoreo
{
    // Panel de soporte: CRUD completo (crear/editar/eliminar, además de los toggles de
    // estado que ya había) sobre las entidades que más mantenimiento manual necesitan,
    // directo sobre el Core, fuera de /api (no pasa por el Gateway ni por el JWT de la
    // Web). Ya no se guarda con una clave compartida: se entra con usuario/contraseña
    // reales (ver /monitor/login en MonitorEndpoints) y MonitorAuth exige los permisos
    // monitor.ver (para leer) / monitor.gestionar (para cualquier cambio).
    public static class OperacionesEndpoints
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Patronato.Core.PanelSoporte");

        // El rol Administrador (SuperAdmin/IT, el único con permiso
        // core-users.crear-superadmin) nunca se puede asignar desde aquí, ni siquiera por
        // quien tenga monitor.gestionar: esa acción sensible siempre pasa por la pantalla
        // "Usuarios del Core" en la Web, con su propio guardia anti-escalamiento granular
        // (ver UsuariosController.PuedeAsignarRolAsync).
        private static async Task<bool> EsRolRestringido(ApplicationDbContext db, int roleId) =>
            await db.CoreRolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.Permission!.Codigo == PermisosCore.CoreUsersCrearSuperAdmin);

        public static void MapOperacionesEndpoints(this WebApplication app)
        {
            app.MapGet("/monitor/operaciones", () => Results.Content(PaginaHtml, "text/html"));

            app.MapGet("/monitor/operaciones/datos", async (HttpContext ctx, ApplicationDbContext db) =>
            {
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorVer)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";

                var usuarios = await db.Usuarios.OrderBy(u => u.NombreCompleto).Select(u => new
                {
                    u.Id, u.NombreCompleto, u.NombreUsuario, u.Correo, u.SucursalId,
                    estadoCuenta = u.EstadoCuenta.ToString(),
                    rol = u.RoleId, rolTexto = u.Role!.Nombre,
                }).ToListAsync();

                var idsRolesRestringidos = await db.CoreRolePermissions
                    .Where(rp => rp.Permission!.Codigo == PermisosCore.CoreUsersCrearSuperAdmin)
                    .Select(rp => rp.RoleId)
                    .ToListAsync();

                var roles = await db.CoreRoles.OrderBy(r => r.Nombre)
                    .Select(r => new { r.Id, r.Nombre })
                    .ToListAsync();
                var rolesConRestriccion = roles.Select(r => new { r.Id, r.Nombre, restringido = idsRolesRestringidos.Contains(r.Id) });

                var sucursales = await db.Sucursales.OrderBy(s => s.Nombre)
                    .Select(s => new { s.Id, s.Nombre, s.Direccion, s.Telefono, s.Encargado, s.Activa })
                    .ToListAsync();

                var productos = await db.ProductosArticulos.OrderBy(p => p.Nombre)
                    .Select(p => new { p.Id, p.Codigo, p.Nombre, p.Categoria, p.PrecioVenta, p.StockDisponible })
                    .ToListAsync();

                var beneficiarios = await db.Beneficiarios.OrderBy(b => b.Nombres)
                    .Select(b => new { b.Id, b.Nombres, b.Apellidos, b.Cedula, b.Telefono, b.Activo })
                    .ToListAsync();

                return Results.Json(new { usuarios, roles = rolesConRestriccion, sucursales, productos, beneficiarios });
            });

            app.MapPost("/monitor/operaciones/usuarios/{id:int}/estado", async (HttpContext ctx, ApplicationDbContext db, int id, CambiarEstadoRequest body) =>
            {
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                if (!Enum.IsDefined(typeof(EstadoCuentaCore), body.Estado)) return Results.Json(new { mensaje = "Estado inválido." }, statusCode: 400);

                var usuario = await db.Usuarios.FindAsync(id);
                if (usuario == null) return Results.Json(new { mensaje = "Usuario no encontrado." }, statusCode: 404);

                usuario.EstadoCuenta = body.Estado;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Usuario #{id} ({usuario.NombreUsuario}) -> estado {body.Estado}.");
                return Results.Json(new { mensaje = $"Estado actualizado a {body.Estado}." });
            });

            app.MapPost("/monitor/operaciones/usuarios/{id:int}/rol", async (HttpContext ctx, ApplicationDbContext db, int id, CambiarRolRequest body) =>
            {
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var nuevoRol = await db.CoreRoles.FindAsync(body.Rol);
                if (nuevoRol == null) return Results.Json(new { mensaje = "Rol inválido." }, statusCode: 400);
                if (await EsRolRestringido(db, nuevoRol.Id))
                    return Results.Json(new { mensaje = $"El rol '{nuevoRol.Nombre}' (SuperAdmin/IT) no se puede asignar desde este panel de soporte. Usa 'Usuarios del Core' en la Web, con tu propia sesión." }, statusCode: 403);

                var usuario = await db.Usuarios.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
                if (usuario == null) return Results.Json(new { mensaje = "Usuario no encontrado." }, statusCode: 404);

                var rolAnterior = usuario.Role!.Nombre;
                usuario.RoleId = nuevoRol.Id;
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Usuario #{id} ({usuario.NombreUsuario}) cambió de rol {rolAnterior} -> {nuevoRol.Nombre}.");
                return Results.Json(new { mensaje = $"Rol actualizado a {nuevoRol.Nombre}." });
            });

            app.MapPost("/monitor/operaciones/sucursales/{id:int}/activa", async (HttpContext ctx, ApplicationDbContext db, int id, CambiarActivaRequest body) =>
            {
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
            app.MapPost("/monitor/operaciones/usuarios", async (HttpContext ctx, ApplicationDbContext db, IPasswordHasher<Usuario> hasher, CrearUsuarioRequest body) =>
            {
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
                log4net.LogicalThreadContext.Properties["Origen"] = "PanelSoporte";
                var rol = await db.CoreRoles.FindAsync(body.Rol);
                if (rol == null) return Results.Json(new { mensaje = "Rol inválido." }, statusCode: 400);
                if (await EsRolRestringido(db, rol.Id))
                    return Results.Json(new { mensaje = $"El rol '{rol.Nombre}' (SuperAdmin/IT) no se puede asignar desde este panel de soporte. Usa 'Usuarios del Core' en la Web, con tu propia sesión." }, statusCode: 403);

                var usuario = new Usuario
                {
                    NombreCompleto = body.NombreCompleto,
                    NombreUsuario = body.NombreUsuario,
                    Correo = body.Correo,
                    RoleId = rol.Id,
                    SucursalId = body.SucursalId,
                    EstadoCuenta = EstadoCuentaCore.Activo,
                    DebeCambiarPassword = true,
                };
                usuario.PasswordHash = hasher.HashPassword(usuario, body.PasswordHash);
                db.Usuarios.Add(usuario);
                await db.SaveChangesAsync();
                Logger.Warn($"[Panel de soporte] Usuario creado: '{usuario.NombreUsuario}' ({rol.Nombre}).");
                return Results.Json(new { mensaje = "Usuario creado correctamente.", id = usuario.Id });
            });

            app.MapPut("/monitor/operaciones/usuarios/{id:int}", async (HttpContext ctx, ApplicationDbContext db, int id, EditarUsuarioRequest body) =>
            {
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
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
        private record CambiarEstadoRequest(EstadoCuentaCore Estado);
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
  #mensaje { position: fixed; top: 16px; right: 16px; padding: 10px 16px; border-radius: 8px; font-size: 13px; z-index: 1000; }
  #mensaje.ok { background: #065f46; color: #d1fae5; }
  #mensaje.error { background: #450a0a; color: #fecaca; }

  /* Modal Formulario */
  .modal-overlay {
    position: fixed; top: 0; left: 0; width: 100vw; height: 100vh;
    background: rgba(0,0,0,0.72); backdrop-filter: blur(3px);
    display: flex; align-items: center; justify-content: center; z-index: 500;
  }
  .modal-overlay.oculto { display: none; }
  .modal-card {
    background: #111827; border: 1px solid #334155; border-radius: 12px;
    width: 95%; max-width: 520px; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.7);
    overflow: hidden; animation: popIn 0.15s ease-out;
  }
  @keyframes popIn { from { transform: scale(0.95); opacity: 0; } to { transform: scale(1); opacity: 1; } }
  .modal-header {
    display: flex; align-items: center; justify-content: space-between;
    padding: 14px 20px; border-bottom: 1px solid #1f2937; background: #1a2234;
  }
  .btn-cerrar { background: transparent; border: none; font-size: 22px; color: #94a3b8; cursor: pointer; padding: 0 6px; line-height: 1; border-radius: 4px; }
  .btn-cerrar:hover { color: #f87171; background: rgba(239, 68, 68, 0.1); }
  .modal-campos { padding: 18px 20px; display: grid; grid-template-columns: 1fr 1fr; gap: 14px; max-height: 70vh; overflow-y: auto; }
  .modal-campos .col-span-2 { grid-column: span 2; }
  .campo-item { display: flex; flex-direction: column; gap: 5px; }
  .campo-item label { font-size: 11px; font-weight: 600; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.5px; }
  .campo-item input, .campo-item select {
    background: #0b1120; border: 1px solid #334155; color: #e2e8f0;
    padding: 8px 11px; border-radius: 6px; font-size: 13px; outline: none; width: 100%; box-sizing: border-box;
  }
  .campo-item input:focus, .campo-item select:focus { border-color: #3b82f6; box-shadow: 0 0 0 1px #3b82f6; }
  .modal-acciones {
    display: flex; justify-content: flex-end; gap: 10px; padding: 14px 20px;
    border-top: 1px solid #1f2937; background: #0f172a;
  }
</style>
</head>
<body>

<div id="login" class="login">
  <h2>🔒 Operaciones del Core</h2>
  <p style="color:#94a3b8;font-size:13px;">Acceso directo de soporte/mantenimiento. Inicia sesión con tu usuario del Core.</p>
  <input id="usuario" placeholder="Usuario" autocomplete="username" />
  <input id="clave" type="password" placeholder="Contraseña" autocomplete="current-password" />
  <button onclick="conectar()">Entrar</button>
  <p id="loginError" style="color:#f87171;font-size:12px;"></p>
</div>

<div id="app" class="oculto">
  <header>
    <div>
      <h1>Patronato.Core · Operaciones</h1>
      <small>Acceso directo, fuera del Gateway — acciones administrativas puntuales</small>
    </div>
    <div style="display:flex;align-items:center;gap:14px;">
      <span id="sesionTexto" style="color:#94a3b8;font-size:12px;"></span>
      <button onclick="cerrarSesion()" style="background:#374151;font-size:12px;padding:6px 10px;">Salir</button>
      <a href="/monitor" style="color:#60a5fa;text-decoration:none;font-size:13px;">← Monitor de logs</a>
    </div>
  </header>

  <main>
    <div class="panel">
      <h2>Usuarios / Empleados <button class="solo-gestion" onclick="crearUsuario()">+ Nuevo</button></h2>
      <table><thead><tr><th>Nombre</th><th>Usuario</th><th>Correo</th><th>Rol</th><th>Sucursal</th><th>Estado</th><th>Acciones</th></tr></thead>
      <tbody id="tablaUsuarios"></tbody></table>
    </div>

    <div class="panel">
      <h2>Sucursales <button class="solo-gestion" onclick="crearSucursal()">+ Nueva</button></h2>
      <table><thead><tr><th>Nombre</th><th>Dirección</th><th>Teléfono</th><th>Encargado</th><th>Estado</th><th>Acciones</th></tr></thead>
      <tbody id="tablaSucursales"></tbody></table>
    </div>

    <div class="panel">
      <h2>Inventario <button class="solo-gestion" onclick="crearProducto()">+ Nuevo</button></h2>
      <table><thead><tr><th>Código</th><th>Nombre</th><th>Categoría</th><th>Precio</th><th>Stock</th><th>Ajustar</th><th>Acciones</th></tr></thead>
      <tbody id="tablaProductos"></tbody></table>
    </div>

    <div class="panel">
      <h2>Beneficiarios <button class="solo-gestion" onclick="crearBeneficiario()">+ Nuevo</button></h2>
      <table><thead><tr><th>Nombre</th><th>Cédula</th><th>Teléfono</th><th>Estado</th><th>Acciones</th></tr></thead>
      <tbody id="tablaBeneficiarios"></tbody></table>
    </div>
  </main>
</div>
<div id="mensaje" class="oculto"></div>

<!-- Modal Formulario Rápido -->
<div id="modalOverlay" class="modal-overlay oculto" onclick="clickFondoModal(event)">
  <div class="modal-card">
    <div class="modal-header">
      <h3 id="modalTitulo" style="margin:0;font-size:15px;font-weight:600;">Formulario</h3>
      <button type="button" onclick="cerrarModal()" class="btn-cerrar">&times;</button>
    </div>
    <form id="modalForm" onsubmit="enviarModal(event)">
      <div id="modalCampos" class="modal-campos"></div>
      <div class="modal-acciones">
        <button type="button" onclick="cerrarModal()" style="background:#374151;">Cancelar</button>
        <button type="submit" class="exito" id="modalBtnGuardar">Guardar</button>
      </div>
    </form>
  </div>
</div>

<script>
let token = sessionStorage.getItem('monitorToken') || '';
let MI_PERMISOS = new Set();
let ROLES = {}; // se llena con d.roles en cada pintar() — los ids ya no son fijos (ver CoreRole)
let ROLES_RESTRINGIDOS = new Set(); // roles que este panel no puede asignar (ver EsRolRestringido en el servidor)
const ESTADOS = ['Activo', 'Suspendido', 'Revocado'];

if (token) conectar(true);

async function conectar(silencioso) {
  if (!silencioso) {
    const usuario = document.getElementById('usuario').value.trim();
    const clave = document.getElementById('clave').value;
    if (!usuario || !clave) return;
    const r = await fetch('/monitor/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ nombreUsuario: usuario, password: clave }) });
    const datos = await r.json().catch(() => ({}));
    if (!r.ok) {
      document.getElementById('loginError').textContent = datos.mensaje || 'No se pudo iniciar sesión.';
      return;
    }
    token = datos.token;
    sessionStorage.setItem('monitorToken', token);
    sessionStorage.setItem('monitorSesion', JSON.stringify({ nombre: datos.nombre, rol: datos.rol, permisos: datos.permisos }));
  }

  fetch('/monitor/operaciones/datos', { headers: { 'Authorization': 'Bearer ' + token } }).then(async r => {
    if (r.status === 401 || r.status === 403) {
      cerrarSesion();
      document.getElementById('loginError').textContent = 'Sesión inválida o sin acceso. Inicia sesión de nuevo.';
      return;
    }
    const sesion = JSON.parse(sessionStorage.getItem('monitorSesion') || '{}');
    MI_PERMISOS = new Set(sesion.permisos || []);
    document.getElementById('sesionTexto').textContent = sesion.nombre ? `${sesion.nombre} (${sesion.rol})` : '';
    document.getElementById('login').classList.add('oculto');
    document.getElementById('app').classList.remove('oculto');
    document.querySelectorAll('.solo-gestion').forEach(el => el.style.display = MI_PERMISOS.has('monitor.gestionar') ? '' : 'none');
    pintar(await r.json());
  }).catch(() => { document.getElementById('loginError').textContent = 'No se pudo conectar al Core.'; });
}

function cerrarSesion() {
  sessionStorage.removeItem('monitorToken');
  sessionStorage.removeItem('monitorSesion');
  location.reload();
}

function cargar() {
  fetch('/monitor/operaciones/datos', { headers: { 'Authorization': 'Bearer ' + token } }).then(async r => {
    if (r.status === 401 || r.status === 403) { cerrarSesion(); return; }
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
  if (!MI_PERMISOS.has('monitor.gestionar')) {
    mostrarMensaje('Tu usuario solo tiene acceso de auditoría (solo lectura) en este panel.', true);
    return false;
  }
  const r = await fetch(ruta, {
    method: metodo || 'POST',
    headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const datos = await r.json().catch(() => ({}));
  mostrarMensaje(datos.mensaje || (r.ok ? 'Listo.' : 'Error.'), !r.ok);
  if (r.ok) cargar();
  return r.ok;
}

function cambiarEstado(id, select) { enviar(`/monitor/operaciones/usuarios/${id}/estado`, { estado: select.value }); }
function cambiarRol(id, select) {
  const rolId = Number(select.value);
  if (ROLES_RESTRINGIDOS.has(rolId)) {
    mostrarMensaje(`El rol '${ROLES[rolId]}' (SuperAdmin/IT) no se puede asignar desde aquí. Usa 'Usuarios del Core' en la Web.`, true);
    cargar(); // repinta el select de vuelta a su valor anterior
    return;
  }
  enviar(`/monitor/operaciones/usuarios/${id}/rol`, { rol: rolId });
}
function toggleSucursal(id, activa) { enviar(`/monitor/operaciones/sucursales/${id}/activa`, { activa: !activa }); }
function toggleBeneficiario(id, activo) { enviar(`/monitor/operaciones/beneficiarios/${id}/activo`, { activo: !activo }); }
function ajustarStock(id, input) {
  const delta = Number(input.value);
  if (!delta) return mostrarMensaje('Indica una cantidad distinta de cero.', true);
  enviar(`/monitor/operaciones/productos/${id}/ajustar-stock`, { delta });
  input.value = '';
}

// ---- Sistema de Modal Unificado (rápido y sin prompts molestos) ----
let modalAccion = null;

function abrirModal({ titulo, botonTexto = 'Guardar', campos, onGuardar }) {
  document.getElementById('modalTitulo').textContent = titulo;
  document.getElementById('modalBtnGuardar').textContent = botonTexto;
  modalAccion = onGuardar;

  const cont = document.getElementById('modalCampos');
  cont.innerHTML = campos.map(c => {
    const colClass = c.colSpan ? `col-span-${c.colSpan}` : '';
    let controlHtml = '';

    if (c.tipo === 'select') {
      const opciones = (c.opciones || []).map(op =>
        `<option value="${op.valor}" ${String(op.valor) === String(c.valor) ? 'selected' : ''} ${op.disabled ? 'disabled' : ''}>${op.texto}</option>`
      ).join('');
      controlHtml = `<select id="campo_${c.nombre}" name="${c.nombre}" ${c.requerido ? 'required' : ''}>${opciones}</select>`;
    } else {
      controlHtml = `<input id="campo_${c.nombre}" name="${c.nombre}" type="${c.tipo || 'text'}" value="${c.valor ?? ''}" placeholder="${c.placeholder ?? ''}" ${c.step ? `step="${c.step}"` : ''} ${c.min !== undefined ? `min="${c.min}"` : ''} ${c.requerido ? 'required' : ''} autocomplete="off" />`;
    }

    return `
      <div class="campo-item ${colClass}">
        <label for="campo_${c.nombre}">${c.etiqueta}${c.requerido ? ' <span style="color:#f87171;">*</span>' : ''}</label>
        ${controlHtml}
        ${c.ayuda ? `<span style="font-size:11px;color:#94a3b8;margin-top:2px;">${c.ayuda}</span>` : ''}
      </div>
    `;
  }).join('');

  document.getElementById('modalOverlay').classList.remove('oculto');
  const primerInput = cont.querySelector('input, select');
  if (primerInput) setTimeout(() => primerInput.focus(), 50);
}

function cerrarModal() {
  document.getElementById('modalOverlay').classList.add('oculto');
  modalAccion = null;
}

function clickFondoModal(e) {
  if (e.target.id === 'modalOverlay') cerrarModal();
}

window.addEventListener('keydown', e => {
  if (e.key === 'Escape') cerrarModal();
});

async function enviarModal(e) {
  e.preventDefault();
  if (!modalAccion) return;
  const form = e.target;
  const formData = new FormData(form);
  const data = Object.fromEntries(formData.entries());
  const btn = document.getElementById('modalBtnGuardar');
  const textoOriginal = btn.textContent;
  btn.disabled = true;
  btn.textContent = 'Guardando...';
  try {
    const ok = await modalAccion(data);
    if (ok) cerrarModal();
  } finally {
    btn.disabled = false;
    btn.textContent = textoOriginal;
  }
}

// ---- CRUD de Usuarios ----
function crearUsuario() {
  const rolesAsignables = Object.entries(ROLES)
    .filter(([v]) => !ROLES_RESTRINGIDOS.has(Number(v)))
    .map(([v, t]) => ({ valor: v, texto: t }));

  const sucursalesOpciones = (ultimoDato.sucursales || []).map(s => ({
    valor: s.id,
    texto: `${s.nombre} (#${s.id})`
  }));
  if (sucursalesOpciones.length === 0) sucursalesOpciones.push({ valor: 1, texto: 'Sede Principal (#1)' });

  abrirModal({
    titulo: 'Nuevo Usuario / Empleado',
    botonTexto: 'Crear Usuario',
    campos: [
      { nombre: 'nombreCompleto', etiqueta: 'Nombre Completo', requerido: true, colSpan: 2 },
      { nombre: 'nombreUsuario', etiqueta: 'Usuario (Login)', requerido: true },
      { nombre: 'passwordHash', etiqueta: 'Contraseña Inicial', tipo: 'password', requerido: true, ayuda: 'El empleado la cambiará en su primer ingreso' },
      { nombre: 'correo', etiqueta: 'Correo Electrónico', tipo: 'email', colSpan: 2 },
      { nombre: 'rol', etiqueta: 'Rol', tipo: 'select', opciones: rolesAsignables, valor: rolesAsignables[0]?.valor },
      { nombre: 'sucursalId', etiqueta: 'Sucursal', tipo: 'select', opciones: sucursalesOpciones, valor: sucursalesOpciones[0]?.valor }
    ],
    onGuardar: async (datos) => {
      return await enviar('/monitor/operaciones/usuarios', {
        nombreCompleto: datos.nombreCompleto.trim(),
        nombreUsuario: datos.nombreUsuario.trim(),
        correo: datos.correo.trim(),
        passwordHash: datos.passwordHash,
        rol: Number(datos.rol),
        sucursalId: Number(datos.sucursalId)
      });
    }
  });
}

function editarUsuario(u) {
  const sucursalesOpciones = (ultimoDato.sucursales || []).map(s => ({
    valor: s.id,
    texto: `${s.nombre} (#${s.id})`
  }));
  if (sucursalesOpciones.length === 0) sucursalesOpciones.push({ valor: u.sucursalId, texto: `#${u.sucursalId}` });

  abrirModal({
    titulo: `Editar Usuario: ${u.nombreUsuario}`,
    botonTexto: 'Guardar Cambios',
    campos: [
      { nombre: 'nombreCompleto', etiqueta: 'Nombre Completo', valor: u.nombreCompleto, requerido: true, colSpan: 2 },
      { nombre: 'correo', etiqueta: 'Correo Electrónico', tipo: 'email', valor: u.correo || '', colSpan: 2 },
      { nombre: 'sucursalId', etiqueta: 'Sucursal', tipo: 'select', opciones: sucursalesOpciones, valor: u.sucursalId, colSpan: 2 }
    ],
    onGuardar: async (datos) => {
      return await enviar(`/monitor/operaciones/usuarios/${u.id}`, {
        nombreCompleto: datos.nombreCompleto.trim(),
        correo: datos.correo.trim(),
        sucursalId: Number(datos.sucursalId)
      }, 'PUT');
    }
  });
}

function eliminarUsuario(id, nombre) {
  if (confirm(`¿Eliminar permanentemente a "${nombre}"? Esta acción no se puede deshacer.`)) {
    enviar(`/monitor/operaciones/usuarios/${id}`, undefined, 'DELETE');
  }
}

// ---- CRUD de Sucursales ----
function crearSucursal() {
  abrirModal({
    titulo: 'Nueva Sucursal',
    botonTexto: 'Crear Sucursal',
    campos: [
      { nombre: 'nombre', etiqueta: 'Nombre de Sucursal', requerido: true, colSpan: 2 },
      { nombre: 'direccion', etiqueta: 'Dirección', colSpan: 2 },
      { nombre: 'telefono', etiqueta: 'Teléfono' },
      { nombre: 'encargado', etiqueta: 'Encargado' }
    ],
    onGuardar: async (datos) => {
      return await enviar('/monitor/operaciones/sucursales', {
        nombre: datos.nombre.trim(),
        direccion: datos.direccion.trim(),
        telefono: datos.telefono.trim(),
        encargado: datos.encargado.trim()
      });
    }
  });
}

function editarSucursal(s) {
  abrirModal({
    titulo: `Editar Sucursal: ${s.nombre}`,
    botonTexto: 'Guardar Cambios',
    campos: [
      { nombre: 'nombre', etiqueta: 'Nombre de Sucursal', valor: s.nombre, requerido: true, colSpan: 2 },
      { nombre: 'direccion', etiqueta: 'Dirección', valor: s.direccion || '', colSpan: 2 },
      { nombre: 'telefono', etiqueta: 'Teléfono', valor: s.telefono || '' },
      { nombre: 'encargado', etiqueta: 'Encargado', valor: s.encargado || '' }
    ],
    onGuardar: async (datos) => {
      return await enviar(`/monitor/operaciones/sucursales/${s.id}`, {
        nombre: datos.nombre.trim(),
        direccion: datos.direccion.trim(),
        telefono: datos.telefono.trim(),
        encargado: datos.encargado.trim()
      }, 'PUT');
    }
  });
}

function eliminarSucursal(id, nombre) {
  if (confirm(`¿Eliminar permanentemente la sucursal "${nombre}"?`)) {
    enviar(`/monitor/operaciones/sucursales/${id}`, undefined, 'DELETE');
  }
}

// ---- CRUD de Inventario / Productos ----
function crearProducto() {
  abrirModal({
    titulo: 'Nuevo Producto / Artículo',
    botonTexto: 'Crear Producto',
    campos: [
      { nombre: 'codigo', etiqueta: 'Código (ej. PROD-001)', requerido: true },
      { nombre: 'nombre', etiqueta: 'Nombre del Producto', requerido: true },
      { nombre: 'categoria', etiqueta: 'Categoría (ej. Óptica, Bastones, Accesorios)', colSpan: 2 },
      { nombre: 'precioVenta', etiqueta: 'Precio de Venta (RD$)', tipo: 'number', step: '0.01', min: '0', valor: '0.00', requerido: true },
      { nombre: 'stockDisponible', etiqueta: 'Stock Inicial', tipo: 'number', min: '0', valor: '0', requerido: true }
    ],
    onGuardar: async (datos) => {
      return await enviar('/monitor/operaciones/productos', {
        codigo: datos.codigo.trim(),
        nombre: datos.nombre.trim(),
        categoria: datos.categoria.trim(),
        precioVenta: Number(datos.precioVenta),
        stockDisponible: Number(datos.stockDisponible)
      });
    }
  });
}

function editarProducto(p) {
  abrirModal({
    titulo: `Editar Producto: ${p.nombre}`,
    botonTexto: 'Guardar Cambios',
    campos: [
      { nombre: 'codigo', etiqueta: 'Código', valor: p.codigo, requerido: true },
      { nombre: 'nombre', etiqueta: 'Nombre', valor: p.nombre, requerido: true },
      { nombre: 'categoria', etiqueta: 'Categoría', valor: p.categoria || '', colSpan: 2 },
      { nombre: 'precioVenta', etiqueta: 'Precio de Venta (RD$)', tipo: 'number', step: '0.01', min: '0', valor: p.precioVenta, requerido: true, colSpan: 2 }
    ],
    onGuardar: async (datos) => {
      return await enviar(`/monitor/operaciones/productos/${p.id}`, {
        codigo: datos.codigo.trim(),
        nombre: datos.nombre.trim(),
        categoria: datos.categoria.trim(),
        precioVenta: Number(datos.precioVenta)
      }, 'PUT');
    }
  });
}

function eliminarProducto(id, nombre) {
  if (confirm(`¿Eliminar permanentemente el producto "${nombre}"?`)) {
    enviar(`/monitor/operaciones/productos/${id}`, undefined, 'DELETE');
  }
}

// ---- CRUD de Beneficiarios ----
function crearBeneficiario() {
  abrirModal({
    titulo: 'Nuevo Beneficiario',
    botonTexto: 'Crear Beneficiario',
    campos: [
      { nombre: 'nombres', etiqueta: 'Nombres', requerido: true },
      { nombre: 'apellidos', etiqueta: 'Apellidos', requerido: true },
      { nombre: 'cedula', etiqueta: 'Cédula / Identificación' },
      { nombre: 'telefono', etiqueta: 'Teléfono' },
      {
        nombre: 'sedeAsignada',
        etiqueta: 'Sede Asignada',
        tipo: 'select',
        opciones: [
          { valor: 1, texto: 'Santo Domingo' },
          { valor: 2, texto: 'Santiago' },
          { valor: 3, texto: 'Barahona' }
        ],
        valor: 1
      },
      {
        nombre: 'condicionVisual',
        etiqueta: 'Condición Visual',
        tipo: 'select',
        opciones: [
          { valor: 1, texto: 'Baja Visión' },
          { valor: 2, texto: 'Ceguera Congénita' },
          { valor: 3, texto: 'Ceguera Adquirida' }
        ],
        valor: 1
      }
    ],
    onGuardar: async (datos) => {
      return await enviar('/monitor/operaciones/beneficiarios', {
        nombres: datos.nombres.trim(),
        apellidos: datos.apellidos.trim(),
        cedula: datos.cedula.trim(),
        telefono: datos.telefono.trim(),
        sedeAsignada: Number(datos.sedeAsignada),
        condicionVisual: Number(datos.condicionVisual)
      });
    }
  });
}

function editarBeneficiario(b) {
  abrirModal({
    titulo: `Editar Beneficiario: ${b.nombres} ${b.apellidos}`,
    botonTexto: 'Guardar Cambios',
    campos: [
      { nombre: 'nombres', etiqueta: 'Nombres', valor: b.nombres, requerido: true },
      { nombre: 'apellidos', etiqueta: 'Apellidos', valor: b.apellidos, requerido: true },
      { nombre: 'cedula', etiqueta: 'Cédula', valor: b.cedula || '' },
      { nombre: 'telefono', etiqueta: 'Teléfono', valor: b.telefono || '' }
    ],
    onGuardar: async (datos) => {
      return await enviar(`/monitor/operaciones/beneficiarios/${b.id}`, {
        nombres: datos.nombres.trim(),
        apellidos: datos.apellidos.trim(),
        cedula: datos.cedula.trim(),
        telefono: datos.telefono.trim()
      }, 'PUT');
    }
  });
}

function eliminarBeneficiario(id, nombre) {
  if (confirm(`¿Eliminar permanentemente a "${nombre}"? Esta acción no se puede deshacer.`)) {
    enviar(`/monitor/operaciones/beneficiarios/${id}`, undefined, 'DELETE');
  }
}

let ultimoDato = { usuarios: [], sucursales: [], productos: [], beneficiarios: [] };

function pintar(d) {
  ultimoDato = d;
  ROLES = Object.fromEntries((d.roles || []).map(r => [r.id, r.nombre]));
  ROLES_RESTRINGIDOS = new Set((d.roles || []).filter(r => r.restringido).map(r => r.id));
  const puedeGestionar = MI_PERMISOS.has('monitor.gestionar');
  const dis = puedeGestionar ? '' : 'disabled';

  document.getElementById('tablaUsuarios').innerHTML = d.usuarios.map(u => `
    <tr>
      <td>${u.nombreCompleto}</td>
      <td>${u.nombreUsuario}</td>
      <td>${u.correo ?? ''}</td>
      <td><select ${dis} onchange="cambiarRol(${u.id}, this)">
        ${Object.entries(ROLES).map(([v, t]) => `<option value="${v}" ${Number(v) === u.rol ? 'selected' : ''}>${t}${ROLES_RESTRINGIDOS.has(Number(v)) ? ' 🔒' : ''}</option>`).join('')}
      </select></td>
      <td>#${u.sucursalId}</td>
      <td><select ${dis} onchange="cambiarEstado(${u.id}, this)">
        ${ESTADOS.map(e => `<option value="${e}" ${e === u.estadoCuenta ? 'selected' : ''}>${e}</option>`).join('')}
      </select></td>
      <td class="accion">${!puedeGestionar ? '—' : `
        <button onclick="editarUsuario(ultimoDato.usuarios.find(x=>x.id===${u.id}))">Editar</button>
        <button class="peligro" onclick="eliminarUsuario(${u.id}, '${(u.nombreCompleto || '').replace(/'/g, "\\'")}')">Eliminar</button>
      `}</td>
    </tr>`).join('');

  document.getElementById('tablaSucursales').innerHTML = d.sucursales.map(s => `
    <tr>
      <td>${s.nombre}</td><td>${s.direccion}</td><td>${s.telefono}</td><td>${s.encargado}</td>
      <td><span class="tag ${s.activa ? 'tag-ok' : 'tag-mal'}">${s.activa ? 'Activa' : 'Inactiva'}</span></td>
      <td class="accion">${!puedeGestionar ? '—' : `
        <button onclick="editarSucursal(ultimoDato.sucursales.find(x=>x.id===${s.id}))">Editar</button>
        <button class="${s.activa ? 'peligro' : 'exito'}" onclick="toggleSucursal(${s.id}, ${s.activa})">${s.activa ? 'Desactivar' : 'Activar'}</button>
        <button class="peligro" onclick="eliminarSucursal(${s.id}, '${(s.nombre || '').replace(/'/g, "\\'")}')">Eliminar</button>
      `}</td>
    </tr>`).join('');

  document.getElementById('tablaProductos').innerHTML = d.productos.map(p => `
    <tr>
      <td>${p.codigo}</td><td>${p.nombre}</td><td>${p.categoria}</td>
      <td>RD$ ${p.precioVenta.toFixed(2)}</td><td>${p.stockDisponible}</td>
      <td class="accion">${!puedeGestionar ? '—' : `<input class="stock" type="number" placeholder="±cant." id="stock-${p.id}" />
        <button class="exito" onclick="ajustarStock(${p.id}, document.getElementById('stock-${p.id}'))">Ajustar</button>`}</td>
      <td class="accion">${!puedeGestionar ? '—' : `
        <button onclick="editarProducto(ultimoDato.productos.find(x=>x.id===${p.id}))">Editar</button>
        <button class="peligro" onclick="eliminarProducto(${p.id}, '${(p.nombre || '').replace(/'/g, "\\'")}')">Eliminar</button>
      `}</td>
    </tr>`).join('');

  document.getElementById('tablaBeneficiarios').innerHTML = d.beneficiarios.map(b => `
    <tr>
      <td>${b.nombres} ${b.apellidos}</td><td>${b.cedula}</td><td>${b.telefono}</td>
      <td><span class="tag ${b.activo ? 'tag-ok' : 'tag-mal'}">${b.activo ? 'Activo' : 'Inactivo'}</span></td>
      <td class="accion">${!puedeGestionar ? '—' : `
        <button onclick="editarBeneficiario(ultimoDato.beneficiarios.find(x=>x.id===${b.id}))">Editar</button>
        <button class="${b.activo ? 'peligro' : 'exito'}" onclick="toggleBeneficiario(${b.id}, ${b.activo})">${b.activo ? 'Desactivar' : 'Activar'}</button>
        <button class="peligro" onclick="eliminarBeneficiario(${b.id}, '${(b.nombres || '').replace(/'/g, "\\'")}')">Eliminar</button>
      `}</td>
    </tr>`).join('');
}
</script>
</body>
</html>
""";
    }
}
