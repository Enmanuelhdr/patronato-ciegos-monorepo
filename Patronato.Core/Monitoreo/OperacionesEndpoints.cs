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

                var nombreUsuarioLimpio = body.NombreUsuario.Trim();
                if (await db.Usuarios.AnyAsync(u => u.NombreUsuario.ToLower() == nombreUsuarioLimpio.ToLower()))
                    return Results.Json(new { mensaje = $"El nombre de usuario '{nombreUsuarioLimpio}' ya está registrado." }, statusCode: 400);

                var usuario = new Usuario
                {
                    NombreCompleto = body.NombreCompleto.Trim(),
                    NombreUsuario = nombreUsuarioLimpio,
                    Correo = body.Correo?.Trim() ?? string.Empty,
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

                if (!string.IsNullOrWhiteSpace(body.Correo))
                {
                    var correoLimpio = body.Correo.Trim().ToLower();
                    if (await db.Usuarios.AnyAsync(u => u.Correo.ToLower() == correoLimpio && u.Id != id))
                        return Results.Json(new { mensaje = $"El correo '{body.Correo.Trim()}' ya pertenece a otro usuario." }, statusCode: 400);
                }

                usuario.NombreCompleto = body.NombreCompleto.Trim();
                usuario.Correo = body.Correo?.Trim() ?? string.Empty;
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

                var nombreSucursal = body.Nombre.Trim();
                if (await db.Sucursales.AnyAsync(s => s.Nombre.ToLower() == nombreSucursal.ToLower()))
                    return Results.Json(new { mensaje = $"Ya existe una sucursal con el nombre '{nombreSucursal}'." }, statusCode: 400);

                var sucursal = new Sucursal
                {
                    Nombre = nombreSucursal,
                    Direccion = body.Direccion?.Trim() ?? string.Empty,
                    Telefono = body.Telefono?.Trim() ?? string.Empty,
                    Encargado = body.Encargado?.Trim() ?? string.Empty,
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

                var nombreSucursal = body.Nombre.Trim();
                if (await db.Sucursales.AnyAsync(s => s.Nombre.ToLower() == nombreSucursal.ToLower() && s.Id != id))
                    return Results.Json(new { mensaje = $"Ya existe otra sucursal con el nombre '{nombreSucursal}'." }, statusCode: 400);

                sucursal.Nombre = nombreSucursal;
                sucursal.Direccion = body.Direccion?.Trim() ?? string.Empty;
                sucursal.Telefono = body.Telefono?.Trim() ?? string.Empty;
                sucursal.Encargado = body.Encargado?.Trim() ?? string.Empty;
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

                var codigoLimpio = body.Codigo.Trim();
                if (await db.ProductosArticulos.AnyAsync(p => p.Codigo.ToLower() == codigoLimpio.ToLower()))
                    return Results.Json(new { mensaje = $"El código de producto '{codigoLimpio}' ya existe." }, statusCode: 400);

                var producto = new ProductoArticulo
                {
                    Codigo = codigoLimpio,
                    Nombre = body.Nombre.Trim(),
                    Categoria = body.Categoria?.Trim() ?? "",
                    PrecioVenta = Math.Max(0, body.PrecioVenta),
                    StockDisponible = Math.Max(0, body.StockDisponible)
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

                var codigoLimpio = body.Codigo.Trim();
                if (await db.ProductosArticulos.AnyAsync(p => p.Codigo.ToLower() == codigoLimpio.ToLower() && p.Id != id))
                    return Results.Json(new { mensaje = $"El código de producto '{codigoLimpio}' ya pertenece a otro artículo." }, statusCode: 400);

                producto.Codigo = codigoLimpio;
                producto.Nombre = body.Nombre.Trim();
                producto.Categoria = body.Categoria?.Trim() ?? "";
                producto.PrecioVenta = Math.Max(0, body.PrecioVenta);
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

                if (!string.IsNullOrWhiteSpace(body.Cedula))
                {
                    var cedulaLimpia = body.Cedula.Trim();
                    if (await db.Beneficiarios.AnyAsync(b => b.Cedula == cedulaLimpia))
                        return Results.Json(new { mensaje = $"Ya existe un beneficiario registrado con la cédula '{cedulaLimpia}'." }, statusCode: 400);
                }

                var beneficiario = new Beneficiario
                {
                    Nombres = body.Nombres.Trim(),
                    Apellidos = body.Apellidos.Trim(),
                    Cedula = body.Cedula?.Trim() ?? string.Empty,
                    Telefono = body.Telefono?.Trim() ?? string.Empty,
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

                if (!string.IsNullOrWhiteSpace(body.Cedula))
                {
                    var cedulaLimpia = body.Cedula.Trim();
                    if (await db.Beneficiarios.AnyAsync(b => b.Cedula == cedulaLimpia && b.Id != id))
                        return Results.Json(new { mensaje = $"La cédula '{cedulaLimpia}' ya pertenece a otro beneficiario." }, statusCode: 400);
                }

                beneficiario.Nombres = body.Nombres.Trim();
                beneficiario.Apellidos = body.Apellidos.Trim();
                beneficiario.Cedula = body.Cedula?.Trim() ?? string.Empty;
                beneficiario.Telefono = body.Telefono?.Trim() ?? string.Empty;
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

            app.MapPost("/monitor/operaciones/sembrar-datos", (HttpContext ctx, ApplicationDbContext db) =>
            {
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorGestionar)) return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);
                var (agregados, detalle) = DbMassiveSeeder.SembrarDatosSiFaltan(db);
                return Results.Json(new
                {
                    mensaje = agregados > 0 
                        ? $"Se poblaron {agregados} registros masivos reales exitosamente ({detalle})." 
                        : "La base de datos ya cuenta con más de 350 registros.",
                    agregados,
                    detalle
                });
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
<title>Centro de Operaciones — Patronato Nacional de Ciegos</title>
<style>
  :root {
    --bg-base: #090d16;
    --bg-surface: #0f172a;
    --bg-card: #1e293b;
    --bg-card-hover: #27354a;
    --border-color: #334155;
    --border-subtle: #1e293b;
    --text-primary: #f8fafc;
    --text-muted: #94a3b8;
    --text-dim: #64748b;
    --brand-blue: #2563eb;
    --brand-blue-hover: #1d4ed8;
    --brand-cyan: #0284c7;
    --brand-emerald: #059669;
    --brand-emerald-hover: #047857;
    --brand-rose: #dc2626;
    --brand-rose-hover: #b91c1c;
    --brand-amber: #d97706;
    color-scheme: dark;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", sans-serif;
    background: var(--bg-base);
    color: var(--text-primary);
    -webkit-font-smoothing: antialiased;
  }

  /* Top Navigation Bar */
  header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 14px 28px;
    background: rgba(15, 23, 42, 0.85);
    backdrop-filter: blur(12px);
    border-bottom: 1px solid var(--border-color);
    position: sticky;
    top: 0;
    z-index: 100;
  }
  .header-brand {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .brand-badge {
    background: linear-gradient(135deg, #034ea2, #2563eb);
    color: white;
    font-weight: 800;
    font-size: 13px;
    padding: 6px 10px;
    border-radius: 8px;
    box-shadow: 0 4px 10px rgba(37, 99, 235, 0.35);
  }
  .header-titles h1 {
    margin: 0;
    font-size: 15px;
    font-weight: 700;
    color: var(--text-primary);
    letter-spacing: -0.2px;
  }
  .header-titles small {
    display: block;
    font-size: 11px;
    color: var(--text-muted);
    margin-top: 2px;
  }
  .header-actions {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .session-pill {
    background: rgba(30, 41, 59, 0.7);
    border: 1px solid var(--border-color);
    padding: 5px 12px;
    border-radius: 20px;
    font-size: 12px;
    color: #cbd5e1;
  }

  /* Main Container */
  main {
    padding: 24px 28px 48px;
    max-width: 1400px;
    margin: 0 auto;
  }

  /* Login Screen */
  .login-card {
    max-width: 380px;
    margin: 90px auto;
    background: var(--bg-surface);
    border: 1px solid var(--border-color);
    border-radius: 16px;
    padding: 32px 28px;
    box-shadow: 0 20px 40px rgba(0, 0, 0, 0.6);
    text-align: center;
  }
  .login-card h2 {
    margin: 0 0 8px;
    font-size: 18px;
    font-weight: 700;
  }
  .login-card p {
    color: var(--text-muted);
    font-size: 13px;
    margin: 0 0 20px;
    line-height: 1.4;
  }
  .login-card input {
    width: 100%;
    padding: 11px 14px;
    margin-bottom: 12px;
    border-radius: 8px;
    border: 1px solid var(--border-color);
    background: var(--bg-base);
    color: var(--text-primary);
    font-size: 13px;
    outline: none;
  }
  .login-card input:focus {
    border-color: var(--brand-blue);
    box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.25);
  }
  .login-card button:not(.btn-eye) {
    width: 100%;
    padding: 11px;
    border-radius: 8px;
    font-weight: 600;
    font-size: 13px;
  }
  .btn-eye {
    position: absolute !important;
    right: 12px !important;
    top: 50% !important;
    transform: translateY(-50%) !important;
    width: 32px !important;
    height: 32px !important;
    min-width: 0 !important;
    background: transparent !important;
    border: none !important;
    box-shadow: none !important;
    padding: 0 !important;
    margin: 0 !important;
    font-size: 18px !important;
    line-height: 1 !important;
    cursor: pointer !important;
    color: #94a3b8 !important;
    display: inline-flex !important;
    align-items: center !important;
    justify-content: center !important;
    z-index: 10 !important;
  }
  .btn-eye:hover {
    background: transparent !important;
    color: #38bdf8 !important;
    transform: translateY(-50%) scale(1.1) !important;
  }

  /* KPI Stat Counters Row */
  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 16px;
    margin-bottom: 24px;
  }
  .stat-card {
    background: var(--bg-surface);
    border: 1px solid var(--border-color);
    border-radius: 12px;
    padding: 16px 20px;
    display: flex;
    align-items: center;
    gap: 16px;
    transition: transform 0.15s ease, border-color 0.15s ease;
  }
  .stat-card:hover {
    transform: translateY(-2px);
    border-color: #475569;
  }
  .stat-icon {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 20px;
    flex-shrink: 0;
  }
  .stat-icon.blue { background: rgba(37, 99, 235, 0.15); color: #60a5fa; }
  .stat-icon.emerald { background: rgba(5, 150, 105, 0.15); color: #34d399; }
  .stat-icon.amber { background: rgba(217, 119, 6, 0.15); color: #fbbf24; }
  .stat-icon.cyan { background: rgba(2, 132, 199, 0.15); color: #38bdf8; }
  .stat-info { display: flex; flex-direction: column; }
  .stat-label { font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: var(--text-muted); }
  .stat-value { font-size: 22px; font-weight: 800; color: var(--text-primary); margin: 2px 0; }
  .stat-sub { font-size: 10.5px; color: var(--text-dim); }

  /* Toolbar: Search, Filters & Actions */
  .toolbar {
    background: var(--bg-surface);
    border: 1px solid var(--border-color);
    border-radius: 12px;
    padding: 14px 18px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 14px;
    flex-wrap: wrap;
    margin-bottom: 20px;
  }
  .toolbar-search {
    flex: 1;
    min-width: 260px;
    position: relative;
  }
  .toolbar-search input {
    width: 100%;
    padding: 10px 14px 10px 38px;
    background: var(--bg-base);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    color: var(--text-primary);
    font-size: 13px;
    outline: none;
    transition: border-color 0.15s ease;
  }
  .toolbar-search input:focus {
    border-color: var(--brand-blue);
    box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.2);
  }
  .search-icon {
    position: absolute;
    left: 12px;
    top: 50%;
    transform: translateY(-50%);
    color: var(--text-dim);
    font-size: 13px;
    pointer-events: none;
  }
  .toolbar-actions {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
  }

  /* Interactive Tab Switcher */
  .tab-nav {
    display: flex;
    gap: 8px;
    border-bottom: 1px solid var(--border-color);
    margin-bottom: 20px;
    overflow-x: auto;
  }
  .tab-btn {
    background: transparent;
    border: none;
    color: var(--text-muted);
    font-size: 13px;
    font-weight: 600;
    padding: 12px 18px;
    border-radius: 8px 8px 0 0;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
    position: relative;
    transition: color 0.15s ease;
  }
  .tab-btn:hover {
    color: var(--text-primary);
    background: rgba(30, 41, 59, 0.5);
  }
  .tab-btn.activo {
    color: #60a5fa;
    background: rgba(30, 41, 59, 0.85);
  }
  .tab-btn.activo::after {
    content: "";
    position: absolute;
    bottom: -1px;
    left: 0;
    right: 0;
    height: 2px;
    background: #3b82f6;
  }
  .tab-count {
    background: rgba(51, 65, 85, 0.6);
    color: var(--text-muted);
    font-size: 11px;
    font-weight: 700;
    padding: 2px 7px;
    border-radius: 10px;
  }
  .tab-btn.activo .tab-count {
    background: rgba(37, 99, 235, 0.3);
    color: #93c5fd;
  }

  /* Table Cards */
  .panel-tabla {
    background: var(--bg-surface);
    border: 1px solid var(--border-color);
    border-radius: 14px;
    overflow: hidden;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.25);
  }
  .panel-header {
    padding: 16px 20px;
    border-bottom: 1px solid var(--border-color);
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: rgba(30, 41, 59, 0.4);
  }
  .panel-header h2 {
    margin: 0;
    font-size: 14px;
    font-weight: 700;
    color: var(--text-primary);
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .tabla-scroll {
    overflow-x: auto;
  }
  table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12.5px;
    text-align: left;
  }
  thead th {
    background: rgba(15, 23, 42, 0.6);
    color: var(--text-muted);
    font-weight: 600;
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    padding: 12px 16px;
    border-bottom: 1px solid var(--border-color);
    white-space: nowrap;
  }
  tbody td {
    padding: 12px 16px;
    border-bottom: 1px solid #1a2332;
    vertical-align: middle;
    color: var(--text-primary);
  }
  tbody tr:last-child td {
    border-bottom: none;
  }
  tbody tr:hover {
    background: rgba(30, 41, 59, 0.5);
  }

  /* Form Controls in Table */
  select.select-tabla, input.input-stock {
    background: var(--bg-base);
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 5px 8px;
    font-size: 12px;
    outline: none;
  }
  select.select-tabla:focus, input.input-stock:focus {
    border-color: var(--brand-blue);
  }
  input.input-stock {
    width: 75px;
  }

  /* Badges & Tags */
  .tag {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 3px 9px;
    border-radius: 12px;
    font-size: 11px;
    font-weight: 700;
    white-space: nowrap;
  }
  .tag-ok {
    background: rgba(5, 150, 105, 0.15);
    color: #34d399;
    border: 1px solid rgba(5, 150, 105, 0.3);
  }
  .tag-mal {
    background: rgba(220, 38, 38, 0.15);
    color: #f87171;
    border: 1px solid rgba(220, 38, 38, 0.3);
  }
  .tag-warn {
    background: rgba(217, 119, 6, 0.15);
    color: #fbbf24;
    border: 1px solid rgba(217, 119, 6, 0.3);
  }

  /* Action Buttons */
  button {
    background: var(--brand-blue);
    color: white;
    border: none;
    padding: 7px 13px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 12px;
    font-weight: 600;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    transition: background 0.15s ease, transform 0.1s ease;
  }
  button:hover {
    background: var(--brand-blue-hover);
  }
  button:active {
    transform: scale(0.98);
  }
  button.btn-secundario {
    background: #334155;
    color: var(--text-primary);
  }
  button.btn-secundario:hover {
    background: #475569;
  }
  button.peligro {
    background: rgba(220, 38, 38, 0.2);
    color: #f87171;
    border: 1px solid rgba(220, 38, 38, 0.35);
  }
  button.peligro:hover {
    background: var(--brand-rose);
    color: white;
  }
  button.exito {
    background: var(--brand-emerald);
  }
  button.exito:hover {
    background: var(--brand-emerald-hover);
  }
  .btn-seeder {
    background: linear-gradient(135deg, #059669, #0d9488);
    box-shadow: 0 2px 8px rgba(5, 150, 105, 0.3);
  }
  .btn-seeder:hover {
    background: linear-gradient(135deg, #047857, #0f766e);
  }
  .accion {
    display: flex;
    gap: 6px;
    align-items: center;
  }

  /* Notification Toast */
  #mensaje {
    position: fixed;
    top: 20px;
    right: 24px;
    padding: 12px 20px;
    border-radius: 10px;
    font-size: 13px;
    font-weight: 600;
    z-index: 1000;
    box-shadow: 0 10px 25px rgba(0,0,0,0.5);
    display: flex;
    align-items: center;
    gap: 8px;
    animation: slideIn 0.2s ease;
  }
  @keyframes slideIn { from { transform: translateY(-10px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
  #mensaje.ok { background: #065f46; color: #ecfdf5; border: 1px solid #059669; }
  #mensaje.error { background: #7f1d1d; color: #fef2f2; border: 1px solid #dc2626; }
  .oculto { display: none !important; }

  /* Modals */
  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background: rgba(0, 0, 0, 0.75);
    backdrop-filter: blur(5px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 500;
  }
  .modal-card {
    background: var(--bg-surface);
    border: 1px solid var(--border-color);
    border-radius: 14px;
    width: 95%;
    max-width: 540px;
    box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.8);
    overflow: hidden;
    animation: popIn 0.15s ease-out;
  }
  @keyframes popIn { from { transform: scale(0.96); opacity: 0; } to { transform: scale(1); opacity: 1; } }
  .modal-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 16px 22px;
    border-bottom: 1px solid var(--border-color);
    background: rgba(30, 41, 59, 0.6);
  }
  .btn-cerrar {
    background: transparent;
    border: none;
    font-size: 22px;
    color: var(--text-muted);
    cursor: pointer;
    padding: 0 6px;
    line-height: 1;
    border-radius: 6px;
  }
  .btn-cerrar:hover {
    color: #f87171;
    background: rgba(239, 68, 68, 0.1);
  }
  .modal-campos {
    padding: 20px 22px;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
    max-height: 70vh;
    overflow-y: auto;
  }
  .modal-campos .col-span-2 { grid-column: span 2; }
  .campo-item { display: flex; flex-direction: column; gap: 5px; }
  .campo-item label {
    font-size: 11px;
    font-weight: 700;
    color: var(--text-muted);
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
  .campo-item input, .campo-item select {
    background: var(--bg-base);
    border: 1px solid var(--border-color);
    color: var(--text-primary);
    padding: 9px 12px;
    border-radius: 8px;
    font-size: 13px;
    outline: none;
    width: 100%;
  }
  .campo-item input:focus, .campo-item select:focus {
    border-color: var(--brand-blue);
    box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.25);
  }
  .modal-acciones {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    padding: 14px 22px;
    border-top: 1px solid var(--border-color);
    background: rgba(15, 23, 42, 0.8);
  }
</style>
</head>
<body>

<!-- Login Modal -->
<div id="login" class="login-card">
  <div style="margin-bottom:14px;">
    <span class="brand-badge" style="font-size:16px;padding:8px 14px;">PNC</span>
  </div>
  <h2>Centro de Control Operativo</h2>
  <p>Inicia sesión con tu cuenta de personal autorizado para acceder a la gestión institucional.</p>
  <input id="usuario" placeholder="Nombre de usuario" autocomplete="username" />
  <div style="position:relative;width:100%;margin-bottom:12px;">
    <input id="clave" type="password" placeholder="Contraseña de acceso" autocomplete="current-password" style="width:100%;box-sizing:border-box;padding-right:44px;margin-bottom:0;" />
    <button type="button" class="btn-eye" onclick="const c=document.getElementById('clave');c.type=c.type==='password'?'text':'password';this.textContent=c.type==='password'?'👁️':'🙈';" title="Ver u ocultar contraseña">👁️</button>
  </div>
  <button onclick="conectar()">Entrar al Sistema</button>
  <p id="loginError" style="color:#f87171;font-size:12px;margin-top:14px;margin-bottom:0;"></p>
</div>

<!-- Main App Portal -->
<div id="app" class="oculto">
  <header>
    <div class="header-brand">
      <div class="brand-badge">PNC</div>
      <div class="header-titles">
        <h1>Patronato Nacional de Ciegos · Centro de Control Operativo</h1>
        <small>Administración integral en tiempo real de personal, sedes, catálogo y beneficiarios</small>
      </div>
    </div>
    <div class="header-actions">
      <span id="sesionTexto" class="session-pill"></span>
      <a href="/monitor" style="color:#60a5fa;text-decoration:none;font-size:12px;font-weight:600;display:flex;align-items:center;gap:4px;">
        <span>📊</span> Auditoría en Vivo
      </a>
      <button onclick="cerrarSesion()" class="btn-secundario" style="padding:6px 12px;font-size:11px;">Cerrar Sesión</button>
    </div>
  </header>

  <main>
    <!-- KPI Counters Grid -->
    <div class="stats-grid">
      <div class="stat-card" onclick="cambiarPestana('usuarios')" style="cursor:pointer;">
        <div class="stat-icon blue">👥</div>
        <div class="stat-info">
          <span class="stat-label">Personal Registrado</span>
          <span class="stat-value" id="statUsuarios">—</span>
          <span class="stat-sub">Colaboradores con acceso activo</span>
        </div>
      </div>
      <div class="stat-card" onclick="cambiarPestana('sucursales')" style="cursor:pointer;">
        <div class="stat-icon emerald">🏢</div>
        <div class="stat-info">
          <span class="stat-label">Sedes a Nivel Nacional</span>
          <span class="stat-value" id="statSucursales">—</span>
          <span class="stat-sub">Sede Central y filiales regionales</span>
        </div>
      </div>
      <div class="stat-card" onclick="cambiarPestana('productos')" style="cursor:pointer;">
        <div class="stat-icon amber">📦</div>
        <div class="stat-info">
          <span class="stat-label">Artículos en Catálogo</span>
          <span class="stat-value" id="statProductos">—</span>
          <span class="stat-sub">Tiflotecnología, óptica y taller</span>
        </div>
      </div>
      <div class="stat-card" onclick="cambiarPestana('beneficiarios')" style="cursor:pointer;">
        <div class="stat-icon cyan">🤝</div>
        <div class="stat-info">
          <span class="stat-label">Beneficiarios Activos</span>
          <span class="stat-value" id="statBeneficiarios">—</span>
          <span class="stat-sub">Comunidad asistida por el Patronato</span>
        </div>
      </div>
    </div>

    <!-- Unified Toolbar -->
    <div class="toolbar">
      <div class="toolbar-search">
        <span class="search-icon">🔍</span>
        <input id="filtroGlobal" placeholder="Buscar por nombre, código, cédula, cargo o teléfono en la sección activa..." oninput="renderTablas()" />
      </div>
      <div class="toolbar-actions">
        <button id="btnCrearEntidad" class="solo-gestion exito" onclick="accionCrearActual()">
          <span>+</span> <span id="txtBtnCrear">Nuevo Registro</span>
        </button>
        <button class="btn-seeder solo-gestion" onclick="sembrarDatosReales()" title="Poblar catálogo extendido con más de 350 registros de prueba">
          <span>🌱</span> Sembrar 350+ Registros
        </button>
        <button onclick="cargar()" class="btn-secundario">
          <span>🔄</span> Refrescar
        </button>
      </div>
    </div>

    <!-- Navigation Tabs -->
    <div class="tab-nav">
      <button class="tab-btn activo" id="tabBtn_usuarios" onclick="cambiarPestana('usuarios')">
        <span>👥</span> Usuarios y Personal <span class="tab-count" id="countTabUsuarios">0</span>
      </button>
      <button class="tab-btn" id="tabBtn_sucursales" onclick="cambiarPestana('sucursales')">
        <span>🏢</span> Sedes y Sucursales <span class="tab-count" id="countTabSucursales">0</span>
      </button>
      <button class="tab-btn" id="tabBtn_productos" onclick="cambiarPestana('productos')">
        <span>📦</span> Catálogo e Inventario <span class="tab-count" id="countTabProductos">0</span>
      </button>
      <button class="tab-btn" id="tabBtn_beneficiarios" onclick="cambiarPestana('beneficiarios')">
        <span>🤝</span> Beneficiarios <span class="tab-count" id="countTabBeneficiarios">0</span>
      </button>
    </div>

    <!-- Tab Content 1: Usuarios -->
    <div id="panel_usuarios" class="panel-tabla">
      <div class="panel-header">
        <h2>👥 Personal y Colaboradores del Patronato</h2>
        <small style="color:var(--text-muted);">Asignación de roles y estados de cuenta institucionales</small>
      </div>
      <div class="tabla-scroll">
        <table>
          <thead>
            <tr>
              <th>Nombre Completo</th>
              <th>Usuario</th>
              <th>Correo Electrónico</th>
              <th>Rol Institucional</th>
              <th>Sede</th>
              <th>Estado de Cuenta</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody id="tablaUsuarios"></tbody>
        </table>
      </div>
    </div>

    <!-- Tab Content 2: Sucursales -->
    <div id="panel_sucursales" class="panel-tabla oculto">
      <div class="panel-header">
        <h2>🏢 Sedes y Centros Regionales</h2>
        <small style="color:var(--text-muted);">Presencia institucional en el territorio dominicano</small>
      </div>
      <div class="tabla-scroll">
        <table>
          <thead>
            <tr>
              <th>Sede</th>
              <th>Dirección</th>
              <th>Teléfono</th>
              <th>Encargado</th>
              <th>Estado</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody id="tablaSucursales"></tbody>
        </table>
      </div>
    </div>

    <!-- Tab Content 3: Productos / Inventario -->
    <div id="panel_productos" class="panel-tabla oculto">
      <div class="panel-header">
        <h2>📦 Catálogo de Artículos y Taller Eco-Protegido</h2>
        <small style="color:var(--text-muted);">Bastones, Braille, ayudas ópticas y artesanías ecológicas</small>
      </div>
      <div class="tabla-scroll">
        <table>
          <thead>
            <tr>
              <th>Código</th>
              <th>Artículo</th>
              <th>Categoría</th>
              <th>Precio de Venta</th>
              <th>Stock Físico</th>
              <th>Ajustar Stock</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody id="tablaProductos"></tbody>
        </table>
      </div>
    </div>

    <!-- Tab Content 4: Beneficiarios -->
    <div id="panel_beneficiarios" class="panel-tabla oculto">
      <div class="panel-header">
        <h2>🤝 Beneficiarios Asistidos</h2>
        <small style="color:var(--text-muted);">Pacientes y personas con discapacidad visual en seguimiento</small>
      </div>
      <div class="tabla-scroll">
        <table>
          <thead>
            <tr>
              <th>Nombre Completo</th>
              <th>Cédula</th>
              <th>Teléfono</th>
              <th>Estado</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody id="tablaBeneficiarios"></tbody>
        </table>
      </div>
    </div>
  </main>
</div>

<!-- Mensaje Toast -->
<div id="mensaje" class="oculto"></div>

<!-- Modal Formulario Rápido -->
<div id="modalOverlay" class="modal-overlay oculto" onclick="clickFondoModal(event)">
  <div class="modal-card">
    <div class="modal-header">
      <h3 id="modalTitulo" style="margin:0;font-size:15px;font-weight:700;">Formulario</h3>
      <button type="button" onclick="cerrarModal()" class="btn-cerrar">&times;</button>
    </div>
    <form id="modalForm" onsubmit="enviarModal(event)">
      <div id="modalCampos" class="modal-campos"></div>
      <div class="modal-acciones">
        <button type="button" onclick="cerrarModal()" class="btn-secundario">Cancelar</button>
        <button type="submit" class="exito" id="modalBtnGuardar">Guardar</button>
      </div>
    </form>
  </div>
</div>

<!-- Modal de Confirmación para Acciones Destructivas -->
<div id="modalConfirmacion" class="modal-overlay oculto" onclick="clickFondoConfirmacion(event)">
  <div class="modal-card" style="max-width:440px;">
    <div class="modal-header">
      <h3 id="confirmTitulo" style="margin:0;font-size:15px;font-weight:700;color:#f87171;">Confirmar Acción</h3>
      <button type="button" onclick="cerrarConfirmacion()" class="btn-cerrar">&times;</button>
    </div>
    <div style="padding:20px 22px;">
      <p id="confirmMensaje" style="color:#cbd5e1;font-size:13px;line-height:1.5;margin:0 0 20px;"></p>
      <div style="display:flex;gap:10px;justify-content:flex-end;">
        <button type="button" onclick="cerrarConfirmacion()" class="btn-secundario">Cancelar</button>
        <button type="button" id="btnConfirmarAccion" class="peligro">Confirmar y Proceder</button>
      </div>
    </div>
  </div>
</div>

<script>
function esc(s) {
  if (s === null || s === undefined) return '';
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

let token = sessionStorage.getItem('monitorToken') || '';
let MI_PERMISOS = new Set();
let ROLES = {};
let ROLES_RESTRINGIDOS = new Set();
const ESTADOS = ['Activo', 'Suspendido', 'Revocado'];
let pestanaActiva = 'usuarios';
let ultimoDato = { usuarios: [], sucursales: [], productos: [], beneficiarios: [] };

if (token) conectar(true);

function cambiarPestana(p) {
  pestanaActiva = p;
  ['usuarios', 'sucursales', 'productos', 'beneficiarios'].forEach(nombre => {
    const btn = document.getElementById(`tabBtn_${nombre}`);
    const panel = document.getElementById(`panel_${nombre}`);
    if (btn) btn.classList.toggle('activo', nombre === p);
    if (panel) panel.classList.toggle('oculto', nombre !== p);
  });

  const txtCrear = document.getElementById('txtBtnCrear');
  if (txtCrear) {
    txtCrear.textContent = p === 'usuarios' ? 'Nuevo Usuario'
      : (p === 'sucursales' ? 'Nueva Sede'
      : (p === 'productos' ? 'Nuevo Producto' : 'Nuevo Beneficiario'));
  }
}

function accionCrearActual() {
  if (pestanaActiva === 'usuarios') crearUsuario();
  else if (pestanaActiva === 'sucursales') crearSucursal();
  else if (pestanaActiva === 'productos') crearProducto();
  else if (pestanaActiva === 'beneficiarios') crearBeneficiario();
}

async function sembrarDatosReales() {
  if (!confirm('¿Deseas poblar la base de datos con el catálogo extendido y datos masivos de prueba (+350 registros)?')) return;
  mostrarMensaje('Poblando catálogo y registros de prueba...', false);
  const r = await fetch('/monitor/operaciones/sembrar-datos', {
    method: 'POST',
    headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' }
  });
  const res = await r.json().catch(() => ({}));
  mostrarMensaje(res.mensaje || 'Listo.', !r.ok);
  if (r.ok) cargar();
}

async function conectar(silencioso) {
  if (!silencioso) {
    const usuario = document.getElementById('usuario').value.trim();
    const clave = document.getElementById('clave').value;
    if (!usuario || !clave) return;
    const r = await fetch('/monitor/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ nombreUsuario: usuario, password: clave }) });
    const datos = await r.json().catch(() => ({}));
    if (!r.ok) {
      document.getElementById('loginError').textContent = datos.mensaje || 'Credenciales incorrectas.';
      return;
    }
    token = datos.token;
    sessionStorage.setItem('monitorToken', token);
    sessionStorage.setItem('monitorSesion', JSON.stringify({ nombre: datos.nombre, rol: datos.rol, permisos: datos.permisos }));
  }

  fetch('/monitor/operaciones/datos', { headers: { 'Authorization': 'Bearer ' + token } }).then(async r => {
    if (r.status === 401 || r.status === 403) {
      cerrarSesion();
      document.getElementById('loginError').textContent = 'Sesión expirada. Inicie sesión nuevamente.';
      return;
    }
    const sesion = JSON.parse(sessionStorage.getItem('monitorSesion') || '{}');
    MI_PERMISOS = new Set(sesion.permisos || []);
    document.getElementById('sesionTexto').textContent = sesion.nombre ? `${sesion.nombre} • ${sesion.rol}` : '';
    document.getElementById('login').classList.add('oculto');
    document.getElementById('app').classList.remove('oculto');
    document.querySelectorAll('.solo-gestion').forEach(el => el.style.display = MI_PERMISOS.has('monitor.gestionar') ? '' : 'none');
    pintar(await r.json());
  }).catch(() => { document.getElementById('loginError').textContent = 'No se pudo comunicar con el servidor.'; });
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
  el.textContent = (esError ? '⚠️ ' : '✅ ') + texto;
  el.className = esError ? 'error' : 'ok';
  setTimeout(() => el.classList.add('oculto'), 3500);
}

async function enviar(ruta, body, metodo) {
  if (!MI_PERMISOS.has('monitor.gestionar')) {
    mostrarMensaje('Permiso denegado: Tu perfil solo tiene autorización de lectura.', true);
    return false;
  }
  const r = await fetch(ruta, {
    method: metodo || 'POST',
    headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const datos = await r.json().catch(() => ({}));
  mostrarMensaje(datos.mensaje || (r.ok ? 'Operación exitosa.' : 'Error al procesar la solicitud.'), !r.ok);
  if (r.ok) cargar();
  return r.ok;
}

function cambiarEstado(id, select) { enviar(`/monitor/operaciones/usuarios/${id}/estado`, { estado: select.value }); }
function cambiarRol(id, select) {
  const rolId = Number(select.value);
  if (ROLES_RESTRINGIDOS.has(rolId)) {
    mostrarMensaje(`El rol '${ROLES[rolId]}' es de alta seguridad y debe gestionarse desde la plataforma web.`, true);
    cargar();
    return;
  }
  enviar(`/monitor/operaciones/usuarios/${id}/rol`, { rol: rolId });
}
function toggleSucursal(id, activa) { enviar(`/monitor/operaciones/sucursales/${id}/activa`, { activa: !activa }); }
function toggleBeneficiario(id, activo) { enviar(`/monitor/operaciones/beneficiarios/${id}/activo`, { activo: !activo }); }
function ajustarStock(id, input) {
  const delta = Number(input.value);
  if (!delta) return mostrarMensaje('Ingrese una cantidad válida distinta de cero.', true);
  enviar(`/monitor/operaciones/productos/${id}/ajustar-stock`, { delta });
  input.value = '';
}

// Modal System
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
    } else if (c.tipo === 'password') {
      controlHtml = `
        <div style="position:relative;width:100%;">
          <input id="campo_${c.nombre}" name="${c.nombre}" type="password" value="${c.valor ?? ''}" placeholder="${c.placeholder ?? ''}" ${c.requerido ? 'required' : ''} autocomplete="off" style="width:100%;box-sizing:border-box;padding-right:42px;" />
          <button type="button" class="btn-eye" onclick="const inp=document.getElementById('campo_${c.nombre}');inp.type=inp.type==='password'?'text':'password';this.textContent=inp.type==='password'?'👁️':'🙈';" title="Ver u ocultar contraseña">👁️</button>
        </div>`;
    } else {
      controlHtml = `<input id="campo_${c.nombre}" name="${c.nombre}" type="${c.tipo || 'text'}" value="${c.valor ?? ''}" placeholder="${c.placeholder ?? ''}" ${c.step ? `step="${c.step}"` : ''} ${c.min !== undefined ? `min="${c.min}"` : ''} ${c.requerido ? 'required' : ''} autocomplete="off" />`;
    }

    return `
      <div class="campo-item ${colClass}">
        <label for="campo_${c.nombre}">${c.etiqueta}${c.requerido ? ' <span style="color:#f87171;">*</span>' : ''}</label>
        ${controlHtml}
        ${c.ayuda ? `<span style="font-size:10.5px;color:#94a3b8;margin-top:2px;">${c.ayuda}</span>` : ''}
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

function clickFondoConfirmacion(e) {
  if (e.target.id === 'modalConfirmacion') cerrarConfirmacion();
}

let confirmacionAccion = null;

function pedirConfirmacion({ titulo = 'Confirmar Acción', mensaje, onConfirmar }) {
  document.getElementById('confirmTitulo').textContent = titulo;
  document.getElementById('confirmMensaje').textContent = mensaje;
  confirmacionAccion = onConfirmar;
  document.getElementById('modalConfirmacion').classList.remove('oculto');
}

function cerrarConfirmacion() {
  document.getElementById('modalConfirmacion').classList.add('oculto');
  confirmacionAccion = null;
}

document.getElementById('btnConfirmarAccion').addEventListener('click', async () => {
  if (confirmacionAccion) {
    const accion = confirmacionAccion;
    cerrarConfirmacion();
    await accion();
  }
});

window.addEventListener('keydown', e => {
  if (e.key === 'Escape') {
    cerrarModal();
    cerrarConfirmacion();
  }
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

// CRUD Usuarios
function crearUsuario() {
  const rolesAsignables = Object.entries(ROLES)
    .filter(([v]) => !ROLES_RESTRINGIDOS.has(Number(v)))
    .map(([v, t]) => ({ valor: v, texto: t }));

  const sucursalesOpciones = (ultimoDato.sucursales || []).map(s => ({
    valor: s.id,
    texto: `${s.nombre} (#${s.id})`
  }));
  if (sucursalesOpciones.length === 0) sucursalesOpciones.push({ valor: 1, texto: 'Sede Central (#1)' });

  abrirModal({
    titulo: 'Nuevo Colaborador / Usuario',
    botonTexto: 'Crear Usuario',
    campos: [
      { nombre: 'nombreCompleto', etiqueta: 'Nombre Completo', requerido: true, colSpan: 2 },
      { nombre: 'nombreUsuario', etiqueta: 'Usuario (Login)', requerido: true },
      { nombre: 'passwordHash', etiqueta: 'Contraseña Temporal', tipo: 'password', requerido: true, ayuda: 'El usuario podrá actualizarla tras ingresar' },
      { nombre: 'correo', etiqueta: 'Correo Institucional', tipo: 'email', colSpan: 2 },
      { nombre: 'rol', etiqueta: 'Rol', tipo: 'select', opciones: rolesAsignables, valor: rolesAsignables[0]?.valor },
      { nombre: 'sucursalId', etiqueta: 'Sede Asignada', tipo: 'select', opciones: sucursalesOpciones, valor: sucursalesOpciones[0]?.valor }
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
      { nombre: 'sucursalId', etiqueta: 'Sede Asignada', tipo: 'select', opciones: sucursalesOpciones, valor: u.sucursalId, colSpan: 2 }
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

function solicitarEditarUsuario(id) {
  const u = (ultimoDato.usuarios || []).find(x => x.id === id);
  if (u) editarUsuario(u);
}

function solicitarEliminarUsuario(id) {
  const u = (ultimoDato.usuarios || []).find(x => x.id === id);
  const nombre = u ? (u.nombreCompleto || u.nombreUsuario) : `#${id}`;
  pedirConfirmacion({
    titulo: 'Eliminar Usuario',
    mensaje: `¿Confirma la eliminación permanente del usuario "${nombre}"?`,
    onConfirmar: () => enviar(`/monitor/operaciones/usuarios/${id}`, undefined, 'DELETE')
  });
}

// CRUD Sucursales
function crearSucursal() {
  abrirModal({
    titulo: 'Nueva Sede / Sucursal',
    botonTexto: 'Registrar Sede',
    campos: [
      { nombre: 'nombre', etiqueta: 'Nombre de la Sede', requerido: true, colSpan: 2 },
      { nombre: 'direccion', etiqueta: 'Dirección Completa', colSpan: 2 },
      { nombre: 'telefono', etiqueta: 'Teléfono de Contacto' },
      { nombre: 'encargado', etiqueta: 'Director / Encargado' }
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
    titulo: `Editar Sede: ${s.nombre}`,
    botonTexto: 'Guardar Cambios',
    campos: [
      { nombre: 'nombre', etiqueta: 'Nombre de la Sede', valor: s.nombre, requerido: true, colSpan: 2 },
      { nombre: 'direccion', etiqueta: 'Dirección Completa', valor: s.direccion || '', colSpan: 2 },
      { nombre: 'telefono', etiqueta: 'Teléfono', valor: s.telefono || '' },
      { nombre: 'encargado', etiqueta: 'Director / Encargado', valor: s.encargado || '' }
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

function solicitarEditarSucursal(id) {
  const s = (ultimoDato.sucursales || []).find(x => x.id === id);
  if (s) editarSucursal(s);
}

function solicitarEliminarSucursal(id) {
  const s = (ultimoDato.sucursales || []).find(x => x.id === id);
  const nombre = s ? s.nombre : `#${id}`;
  pedirConfirmacion({
    titulo: 'Eliminar Sede',
    mensaje: `¿Desea eliminar la sucursal "${nombre}"?`,
    onConfirmar: () => enviar(`/monitor/operaciones/sucursales/${id}`, undefined, 'DELETE')
  });
}

// CRUD Productos
function crearProducto() {
  abrirModal({
    titulo: 'Nuevo Artículo en Catálogo',
    botonTexto: 'Agregar Producto',
    campos: [
      { nombre: 'codigo', etiqueta: 'Código Identificador (ej. BAST-05)', requerido: true },
      { nombre: 'nombre', etiqueta: 'Descripción del Artículo', requerido: true },
      { nombre: 'categoria', etiqueta: 'Categoría (ej. Movilidad, Óptica, Taller)', colSpan: 2 },
      { nombre: 'precioVenta', etiqueta: 'Precio de Venta (RD$)', tipo: 'number', step: '0.01', min: '0', valor: '0.00', requerido: true },
      { nombre: 'stockDisponible', etiqueta: 'Stock Inicial Físico', tipo: 'number', min: '0', valor: '10', requerido: true }
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
    titulo: `Editar Artículo: ${p.nombre}`,
    botonTexto: 'Guardar Cambios',
    campos: [
      { nombre: 'codigo', etiqueta: 'Código', valor: p.codigo, requerido: true },
      { nombre: 'nombre', etiqueta: 'Descripción', valor: p.nombre, requerido: true },
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

function solicitarEditarProducto(id) {
  const p = (ultimoDato.productos || []).find(x => x.id === id);
  if (p) editarProducto(p);
}

function solicitarEliminarProducto(id) {
  const p = (ultimoDato.productos || []).find(x => x.id === id);
  const nombre = p ? p.nombre : `#${id}`;
  pedirConfirmacion({
    titulo: 'Eliminar Artículo',
    mensaje: `¿Confirma la eliminación del producto "${nombre}" del catálogo?`,
    onConfirmar: () => enviar(`/monitor/operaciones/productos/${id}`, undefined, 'DELETE')
  });
}

// CRUD Beneficiarios
function crearBeneficiario() {
  abrirModal({
    titulo: 'Nuevo Beneficiario',
    botonTexto: 'Registrar Beneficiario',
    campos: [
      { nombre: 'nombres', etiqueta: 'Nombres', requerido: true },
      { nombre: 'apellidos', etiqueta: 'Apellidos', requerido: true },
      { nombre: 'cedula', etiqueta: 'Cédula de Identidad (ej. 001-XXXXXXX-X)' },
      { nombre: 'telefono', etiqueta: 'Teléfono de Contacto' },
      {
        nombre: 'sedeAsignada',
        etiqueta: 'Sede Asignada',
        tipo: 'select',
        opciones: [
          { valor: 1, texto: 'Santo Domingo (Sede Central)' },
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
          { valor: 2, texto: 'Ceguera Total Congénita' },
          { valor: 3, texto: 'Ceguera Total Adquirida' }
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

function solicitarEditarBeneficiario(id) {
  const b = (ultimoDato.beneficiarios || []).find(x => x.id === id);
  if (b) editarBeneficiario(b);
}

function solicitarEliminarBeneficiario(id) {
  const b = (ultimoDato.beneficiarios || []).find(x => x.id === id);
  const nombre = b ? `${b.nombres} ${b.apellidos}` : `#${id}`;
  pedirConfirmacion({
    titulo: 'Eliminar Beneficiario',
    mensaje: `¿Confirma la eliminación del beneficiario "${nombre}"? Si cuenta con historial de consultas o préstamos no podrá eliminarse, pero podrá desactivarse.`,
    onConfirmar: () => enviar(`/monitor/operaciones/beneficiarios/${id}`, undefined, 'DELETE')
  });
}

function pintar(d) {
  ultimoDato = d;
  ROLES = Object.fromEntries((d.roles || []).map(r => [r.id, r.nombre]));
  ROLES_RESTRINGIDOS = new Set((d.roles || []).filter(r => r.restringido).map(r => r.id));

  // Actualizar contadores KPI
  document.getElementById('statUsuarios').textContent = (d.usuarios || []).length;
  document.getElementById('statSucursales').textContent = (d.sucursales || []).length;
  document.getElementById('statProductos').textContent = (d.productos || []).length;
  document.getElementById('statBeneficiarios').textContent = (d.beneficiarios || []).length;

  document.getElementById('countTabUsuarios').textContent = (d.usuarios || []).length;
  document.getElementById('countTabSucursales').textContent = (d.sucursales || []).length;
  document.getElementById('countTabProductos').textContent = (d.productos || []).length;
  document.getElementById('countTabBeneficiarios').textContent = (d.beneficiarios || []).length;

  renderTablas();
}

function renderTablas() {
  const d = ultimoDato;
  const puedeGestionar = MI_PERMISOS.has('monitor.gestionar');
  const dis = puedeGestionar ? '' : 'disabled';
  const q = (document.getElementById('filtroGlobal')?.value || '').toLowerCase().trim();

  // 1. Usuarios
  const usuarios = q
    ? (d.usuarios || []).filter(u =>
        (u.nombreCompleto && u.nombreCompleto.toLowerCase().includes(q)) ||
        (u.nombreUsuario && u.nombreUsuario.toLowerCase().includes(q)) ||
        (u.correo && u.correo.toLowerCase().includes(q)) ||
        (ROLES[u.rol] && ROLES[u.rol].toLowerCase().includes(q)))
    : (d.usuarios || []);

  const tbodyUsuarios = document.getElementById('tablaUsuarios');
  if (usuarios.length === 0) {
    tbodyUsuarios.innerHTML = '<tr><td colspan="7" style="text-align:center;padding:24px;color:var(--text-muted);">No se encontraron usuarios registrados.</td></tr>';
  } else {
    tbodyUsuarios.innerHTML = usuarios.map(u => `
      <tr>
        <td><strong>${esc(u.nombreCompleto)}</strong></td>
        <td><code>${esc(u.nombreUsuario)}</code></td>
        <td>${esc(u.correo || '—')}</td>
        <td><select class="select-tabla" ${dis} onchange="cambiarRol(${u.id}, this)">
          ${Object.entries(ROLES).map(([v, t]) => `<option value="${v}" ${Number(v) === u.rol ? 'selected' : ''}>${esc(t)}${ROLES_RESTRINGIDOS.has(Number(v)) ? ' 🔒' : ''}</option>`).join('')}
        </select></td>
        <td>Sede #${u.sucursalId}</td>
        <td><select class="select-tabla" ${dis} onchange="cambiarEstado(${u.id}, this)">
          ${ESTADOS.map(e => `<option value="${e}" ${e === u.estadoCuenta ? 'selected' : ''}>${e}</option>`).join('')}
        </select></td>
        <td class="accion">${!puedeGestionar ? '—' : `
          <button class="btn-secundario" onclick="solicitarEditarUsuario(${u.id})">✏️ Editar</button>
          <button class="peligro" onclick="solicitarEliminarUsuario(${u.id})">🗑️ Eliminar</button>
        `}</td>
      </tr>`).join('');
  }

  // 2. Sucursales
  const sucursales = q
    ? (d.sucursales || []).filter(s =>
        (s.nombre && s.nombre.toLowerCase().includes(q)) ||
        (s.direccion && s.direccion.toLowerCase().includes(q)) ||
        (s.telefono && s.telefono.toLowerCase().includes(q)) ||
        (s.encargado && s.encargado.toLowerCase().includes(q)))
    : (d.sucursales || []);

  const tbodySucursales = document.getElementById('tablaSucursales');
  if (sucursales.length === 0) {
    tbodySucursales.innerHTML = '<tr><td colspan="6" style="text-align:center;padding:24px;color:var(--text-muted);">No se encontraron sedes.</td></tr>';
  } else {
    tbodySucursales.innerHTML = sucursales.map(s => `
      <tr>
        <td><strong>${esc(s.nombre)}</strong></td>
        <td>${esc(s.direccion || '—')}</td>
        <td>${esc(s.telefono || '—')}</td>
        <td>${esc(s.encargado || '—')}</td>
        <td><span class="tag ${s.activa ? 'tag-ok' : 'tag-mal'}">${s.activa ? '● Activa' : '○ Inactiva'}</span></td>
        <td class="accion">${!puedeGestionar ? '—' : `
          <button class="btn-secundario" onclick="solicitarEditarSucursal(${s.id})">✏️ Editar</button>
          <button class="${s.activa ? 'peligro' : 'exito'}" onclick="toggleSucursal(${s.id}, ${s.activa})">${s.activa ? 'Desactivar' : 'Activar'}</button>
          <button class="peligro" onclick="solicitarEliminarSucursal(${s.id})">🗑️</button>
        `}</td>
      </tr>`).join('');
  }

  // 3. Productos
  const productos = q
    ? (d.productos || []).filter(p =>
        (p.codigo && p.codigo.toLowerCase().includes(q)) ||
        (p.nombre && p.nombre.toLowerCase().includes(q)) ||
        (p.categoria && p.categoria.toLowerCase().includes(q)))
    : (d.productos || []);

  const tbodyProductos = document.getElementById('tablaProductos');
  if (productos.length === 0) {
    tbodyProductos.innerHTML = '<tr><td colspan="7" style="text-align:center;padding:24px;color:var(--text-muted);">No se encontraron productos en catálogo.</td></tr>';
  } else {
    tbodyProductos.innerHTML = productos.map(p => `
      <tr>
        <td><span class="tag tag-ok"><code>${esc(p.codigo)}</code></span></td>
        <td><strong>${esc(p.nombre)}</strong></td>
        <td><span class="tag" style="background:#334155;color:#e2e8f0;">${esc(p.categoria || 'General')}</span></td>
        <td style="font-weight:700;color:#38bdf8;">RD$ ${p.precioVenta.toFixed(2)}</td>
        <td>
          <span style="font-weight:700;color:${p.stockDisponible <= 5 ? '#f87171' : '#34d399'};">
            ${p.stockDisponible} uds
          </span>
          ${p.stockDisponible <= 5 ? '<span class="tag tag-mal" style="margin-left:6px;font-size:9.5px;">Bajo</span>' : ''}
        </td>
        <td class="accion">${!puedeGestionar ? '—' : `
          <input class="input-stock" type="number" placeholder="± cant." id="stock-${p.id}" />
          <button class="exito" onclick="ajustarStock(${p.id}, document.getElementById('stock-${p.id}'))">Ajustar</button>
        `}</td>
        <td class="accion">${!puedeGestionar ? '—' : `
          <button class="btn-secundario" onclick="solicitarEditarProducto(${p.id})">✏️ Editar</button>
          <button class="peligro" onclick="solicitarEliminarProducto(${p.id})">🗑️</button>
        `}</td>
      </tr>`).join('');
  }

  // 4. Beneficiarios
  const beneficiarios = q
    ? (d.beneficiarios || []).filter(b =>
        (b.nombres && b.nombres.toLowerCase().includes(q)) ||
        (b.apellidos && b.apellidos.toLowerCase().includes(q)) ||
        (b.cedula && b.cedula.toLowerCase().includes(q)) ||
        (b.telefono && b.telefono.toLowerCase().includes(q)))
    : (d.beneficiarios || []);

  const tbodyBeneficiarios = document.getElementById('tablaBeneficiarios');
  if (beneficiarios.length === 0) {
    tbodyBeneficiarios.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:24px;color:var(--text-muted);">No se encontraron beneficiarios.</td></tr>';
  } else {
    tbodyBeneficiarios.innerHTML = beneficiarios.map(b => `
      <tr>
        <td><strong>${esc(b.nombres)} ${esc(b.apellidos)}</strong></td>
        <td><code>${esc(b.cedula || '—')}</code></td>
        <td>${esc(b.telefono || '—')}</td>
        <td><span class="tag ${b.activo ? 'tag-ok' : 'tag-mal'}">${b.activo ? '● Activo' : '○ Inactivo'}</span></td>
        <td class="accion">${!puedeGestionar ? '—' : `
          <button class="btn-secundario" onclick="solicitarEditarBeneficiario(${b.id})">✏️ Editar</button>
          <button class="${b.activo ? 'peligro' : 'exito'}" onclick="toggleBeneficiario(${b.id}, ${b.activo})">${b.activo ? 'Desactivar' : 'Activar'}</button>
          <button class="peligro" onclick="solicitarEliminarBeneficiario(${b.id})">🗑️</button>
        `}</td>
      </tr>`).join('');
  }
}
</script>
</body>
</html>
""";
    }
}
