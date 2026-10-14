using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using Patronato.Core.Servicios;

namespace Patronato.Core.Data
{
    public static class DbInitializer
    {
        // ---------- Helpers de migración liviana para SQLite + EnsureCreated ----------
        // EnsureCreated() (ver Program.cs) solo crea tablas nuevas en una base de datos
        // que no existía; no agrega tablas ni columnas a una base ya creada. Por eso,
        // igual que ya se hacía para Logs/OrdenesPedidoCaja/SolicitudesCita, cada tabla
        // o columna nueva se "parcha" aquí con SQL crudo idempotente.
        private static bool ColumnaExiste(ApplicationDbContext context, string tabla, string columna)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info(\"{tabla}\");";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columna, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void EnsureColumn(ApplicationDbContext context, string tabla, string columna, string definicionSql)
        {
            if (!ColumnaExiste(context, tabla, columna))
            {
                // tabla/columna/definicionSql son siempre literales fijos del propio código
                // (nunca entrada del usuario), así que la concatenación aquí es segura.
                var sql = "ALTER TABLE \"" + tabla + "\" ADD COLUMN \"" + columna + "\" " + definicionSql + ";";
                context.Database.ExecuteSqlRaw(sql);
            }
        }

        private static void AplicarParchesDeEsquema(ApplicationDbContext context)
        {
            // Tablas nuevas del RBAC del Core y de Accesos Web/Inventario.
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""CoreRoles"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CoreRoles"" PRIMARY KEY AUTOINCREMENT,
                    ""Nombre"" TEXT NOT NULL,
                    ""Descripcion"" TEXT NOT NULL,
                    ""EsSistema"" INTEGER NOT NULL
                );");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""CorePermissions"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CorePermissions"" PRIMARY KEY AUTOINCREMENT,
                    ""Codigo"" TEXT NOT NULL,
                    ""Modulo"" TEXT NOT NULL,
                    ""Descripcion"" TEXT NOT NULL
                );");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""CoreRolePermissions"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CoreRolePermissions"" PRIMARY KEY AUTOINCREMENT,
                    ""RoleId"" INTEGER NOT NULL,
                    ""PermissionId"" INTEGER NOT NULL
                );");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""WebUsers"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WebUsers"" PRIMARY KEY AUTOINCREMENT,
                    ""NombreCompleto"" TEXT NOT NULL,
                    ""Correo"" TEXT NOT NULL,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""Telefono"" TEXT NULL,
                    ""Estado"" INTEGER NOT NULL,
                    ""FechaRegistro"" TEXT NOT NULL,
                    ""UltimoLogin"" TEXT NULL,
                    ""DebeCambiarPassword"" INTEGER NOT NULL
                );");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""WebUserLoginHistorial"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WebUserLoginHistorial"" PRIMARY KEY AUTOINCREMENT,
                    ""WebUserId"" INTEGER NOT NULL,
                    ""FechaHora"" TEXT NOT NULL,
                    ""Exitoso"" INTEGER NOT NULL,
                    ""Ip"" TEXT NULL,
                    ""UserAgent"" TEXT NULL
                );");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""WebUserSessions"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WebUserSessions"" PRIMARY KEY AUTOINCREMENT,
                    ""WebUserId"" INTEGER NOT NULL,
                    ""Jti"" TEXT NOT NULL,
                    ""CreadoEn"" TEXT NOT NULL,
                    ""RevocadoEn"" TEXT NULL
                );");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""InventarioMovimientos"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_InventarioMovimientos"" PRIMARY KEY AUTOINCREMENT,
                    ""ProductoId"" INTEGER NOT NULL,
                    ""Tipo"" INTEGER NOT NULL,
                    ""Cantidad"" INTEGER NOT NULL,
                    ""StockResultante"" INTEGER NOT NULL,
                    ""Motivo"" TEXT NULL,
                    ""CoreUserId"" INTEGER NOT NULL,
                    ""Fecha"" TEXT NOT NULL
                );");

            // Columnas nuevas en tablas que ya existían antes del RBAC.
            EnsureColumn(context, "Usuarios", "RoleId", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(context, "Usuarios", "EstadoCuenta", "INTEGER NOT NULL DEFAULT 1");
            EnsureColumn(context, "Usuarios", "DebeCambiarPassword", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(context, "Usuarios", "UltimoLogin", "TEXT NULL");
            EnsureColumn(context, "Usuarios", "IntentosFallidos", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(context, "Usuarios", "BloqueadoHasta", "TEXT NULL");

            EnsureColumn(context, "ProductosArticulos", "StockMinimo", "INTEGER NOT NULL DEFAULT 5");
            EnsureColumn(context, "ProductosArticulos", "Activo", "INTEGER NOT NULL DEFAULT 1");
        }

        // Mapeo del viejo enum RolUsuario (ya eliminado de la entidad Usuario) a los
        // nombres de CoreRole, para migrar cuentas creadas antes del RBAC granular.
        private static readonly Dictionary<int, string> RolLegadoANombre = new()
        {
            [1] = PermisosCore.RolAdministrador,
            [2] = "Cajero",
            [3] = "MedicoOftalmologo",
            [4] = "InstructorRehabilitacion",
            [5] = "Recepcionista",
        };

        private static void MigrarUsuariosLegados(ApplicationDbContext context, PasswordHasher<Usuario> hasher)
        {
            // Solo existe si la base venía de antes del RBAC granular; en una base nueva
            // la tabla Usuarios ya se crea sin esta columna.
            if (ColumnaExiste(context, "Usuarios", "Rol"))
            {
                var usuariosSinMigrar = context.Usuarios.Where(u => u.RoleId == 0).ToList();
                var rolesPorNombre = context.CoreRoles.ToDictionary(r => r.Nombre, r => r.Id);

                var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open) connection.Open();

                foreach (var usuario in usuariosSinMigrar)
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT \"Rol\" FROM \"Usuarios\" WHERE \"Id\" = @id;";
                    var parametro = cmd.CreateParameter();
                    parametro.ParameterName = "@id";
                    parametro.Value = usuario.Id;
                    cmd.Parameters.Add(parametro);

                    var rolLegado = Convert.ToInt32(cmd.ExecuteScalar());
                    var nombreRol = RolLegadoANombre.GetValueOrDefault(rolLegado, PermisosCore.RolAdministrador);
                    usuario.RoleId = rolesPorNombre.GetValueOrDefault(nombreRol, rolesPorNombre[PermisosCore.RolAdministrador]);

                    // Las contraseñas sembradas antes del RBAC estaban en texto plano
                    // (ej. "admin123"); un hash real siempre es mucho más largo que eso.
                    if (usuario.PasswordHash.Length < 40)
                    {
                        usuario.PasswordHash = hasher.HashPassword(usuario, usuario.PasswordHash);
                    }
                }

                context.SaveChanges();
            }

            // Ya no hacen falta: todo el acceso ahora pasa por RoleId/EstadoCuenta. Si se
            // dejan, su NOT NULL legado rompe cualquier INSERT nuevo (EF ya no las conoce).
            // Van aparte del "if" de arriba: deben quitarse aunque Rol ya se haya
            // eliminado en una corrida anterior y solo falte Activo (o viceversa).
            if (ColumnaExiste(context, "Usuarios", "Rol"))
                context.Database.ExecuteSqlRaw("ALTER TABLE \"Usuarios\" DROP COLUMN \"Rol\";");
            if (ColumnaExiste(context, "Usuarios", "Activo"))
                context.Database.ExecuteSqlRaw("ALTER TABLE \"Usuarios\" DROP COLUMN \"Activo\";");
        }

        private static void SembrarRolesYPermisos(ApplicationDbContext context)
        {
            if (!context.CorePermissions.Any())
            {
                context.CorePermissions.AddRange(
                    PermisosCore.Catalogo.Select(p => new CorePermission { Codigo = p.Codigo, Modulo = p.Modulo, Descripcion = p.Descripcion })
                );
                context.SaveChanges();
            }

            if (context.CoreRoles.Any()) return;

            var permisosPorCodigo = context.CorePermissions.ToDictionary(p => p.Codigo, p => p.Id);

            var roles = new (string Nombre, string Descripcion, string[] Permisos)[]
            {
                (PermisosCore.RolAdministrador, "Acceso total al sistema: configuración, gestión de usuarios del Core, inventario y accesos web (SuperAdmin / IT).", PermisosCore.PermisosAdministrador),
                (PermisosCore.RolAdminOperativo, "Gestiona inventario y accesos a la plataforma web; no puede crear ni administrar otros usuarios del Core.", PermisosCore.PermisosAdminOperativo),
                (PermisosCore.RolAuditor, "Solo lectura: visualiza todas las pantallas y reportes, sin permisos de creación o edición.", PermisosCore.PermisosAuditor),
                (PermisosCore.RolOperadorLogistico, "Lectura y actualización exclusiva del módulo de Inventario.", PermisosCore.PermisosOperadorLogistico),
                ("Cajero", "Opera la Caja y los cobros.", Array.Empty<string>()),
                ("MedicoOftalmologo", "Atiende consultas oftalmológicas de prevención.", Array.Empty<string>()),
                ("InstructorRehabilitacion", "Gestiona los talleres de rehabilitación (Braille, bastón, oficios).", Array.Empty<string>()),
                ("Recepcionista", "Registra pacientes y agenda citas.", Array.Empty<string>()),
            };

            foreach (var (nombre, descripcion, permisos) in roles)
            {
                var role = new CoreRole { Nombre = nombre, Descripcion = descripcion, EsSistema = true };
                context.CoreRoles.Add(role);
                context.SaveChanges(); // necesitamos role.Id para las filas de CoreRolePermission

                foreach (var codigoPermiso in permisos)
                {
                    context.CoreRolePermissions.Add(new CoreRolePermission { RoleId = role.Id, PermissionId = permisosPorCodigo[codigoPermiso] });
                }
                context.SaveChanges();
            }
        }

        // Corre siempre (a diferencia de SembrarRolesYPermisos, que solo siembra una vez
        // sobre una base vacía): agrega cualquier permiso nuevo del catálogo y sus vínculos
        // de rol que todavía falten en una base ya sembrada antes de que ese permiso
        // existiera (ej. PermisosCore.MonitorVer/MonitorGestionar, agregados después).
        private static void SincronizarCatalogoDePermisos(ApplicationDbContext context)
        {
            if (!context.CoreRoles.Any() || !context.CorePermissions.Any()) return;

            var permisosExistentes = context.CorePermissions.Select(p => p.Codigo).ToHashSet();
            var nuevosPermisos = PermisosCore.Catalogo.Where(p => !permisosExistentes.Contains(p.Codigo)).ToList();
            if (nuevosPermisos.Count > 0)
            {
                context.CorePermissions.AddRange(nuevosPermisos.Select(p => new CorePermission { Codigo = p.Codigo, Modulo = p.Modulo, Descripcion = p.Descripcion }));
                context.SaveChanges();
            }

            var permisosPorCodigo = context.CorePermissions.ToDictionary(p => p.Codigo, p => p.Id);
            var rolesPorNombre = context.CoreRoles.ToDictionary(r => r.Nombre, r => r.Id);
            var vinculosExistentes = context.CoreRolePermissions.Select(rp => new { rp.RoleId, rp.PermissionId }).ToList();

            var mapaRolPermisos = new (string Rol, string[] Permisos)[]
            {
                (PermisosCore.RolAdministrador, PermisosCore.PermisosAdministrador),
                (PermisosCore.RolAdminOperativo, PermisosCore.PermisosAdminOperativo),
                (PermisosCore.RolAuditor, PermisosCore.PermisosAuditor),
                (PermisosCore.RolOperadorLogistico, PermisosCore.PermisosOperadorLogistico),
            };

            foreach (var (rolNombre, permisos) in mapaRolPermisos)
            {
                if (!rolesPorNombre.TryGetValue(rolNombre, out var roleId)) continue;
                foreach (var codigoPermiso in permisos)
                {
                    var permissionId = permisosPorCodigo[codigoPermiso];
                    if (!vinculosExistentes.Any(v => v.RoleId == roleId && v.PermissionId == permissionId))
                    {
                        context.CoreRolePermissions.Add(new CoreRolePermission { RoleId = roleId, PermissionId = permissionId });
                    }
                }
            }
            context.SaveChanges();
        }

        public static void Initialize(ApplicationDbContext context)
        {
            AplicarParchesDeEsquema(context);

            var hasher = new PasswordHasher<Usuario>();

            // Garantizar que la tabla OrdenesPedidoCaja exista en SQLite
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS OrdenesPedidoCaja (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    NumeroOrden TEXT NOT NULL,
                    FechaHora TEXT NOT NULL,
                    ClienteNombre TEXT NOT NULL,
                    ClienteDocumento TEXT,
                    CreadoPor TEXT NOT NULL,
                    Estado TEXT NOT NULL,
                    Total NUMERIC NOT NULL,
                    ItemsJson TEXT NOT NULL,
                    Notas TEXT
                );
            ");

            // Igual que arriba: EnsureCreated() (ver Program.cs) no agrega tablas nuevas a una
            // base de datos que ya existía, así que SolicitudesCita necesita el mismo parche.
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS SolicitudesCita (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    NombreSolicitante TEXT NOT NULL,
                    Telefono TEXT NOT NULL,
                    Correo TEXT NOT NULL,
                    TipoServicio INTEGER NOT NULL,
                    SedePreferida INTEGER NOT NULL,
                    FechaPreferida TEXT NOT NULL,
                    Comentario TEXT,
                    Estado INTEGER NOT NULL,
                    FechaSolicitud TEXT NOT NULL
                );
            ");

            // Si no hay órdenes de pedido pendientes, sembrar una orden de muestra creada por Recepción
            if (!context.OrdenesPedidoCaja.Any())
            {
                context.OrdenesPedidoCaja.Add(new OrdenPedidoCaja
                {
                    NumeroOrden = "ORD-20260924-001",
                    FechaHora = DateTime.Now.AddMinutes(-15),
                    ClienteNombre = "Hospital Salvador B. Gautier",
                    ClienteDocumento = "4-01-00234-5",
                    CreadoPor = "Laura Sánchez (Recepcionista)",
                    Estado = "Pendiente",
                    Total = 1550m,
                    ItemsJson = "[{\"ProductoId\":1,\"Codigo\":\"BAST-01\",\"Nombre\":\"Bastón Plegable Blanco para Ciegos (4 Tramos)\",\"Cantidad\":2,\"PrecioUnitario\":650.00},{\"ProductoId\":2,\"Codigo\":\"SUAP-01\",\"Nombre\":\"Suaper Institucional de Algodón\",\"Cantidad\":1,\"PrecioUnitario\":250.00}]",
                    Notas = "Cotización de insumos generada por recepción. Cliente pasa a Caja para abonar/pagar."
                });
                context.SaveChanges();
            }

            // RBAC: roles/permisos siempre primero (los necesita la migración de usuarios legados
            // y el alta de los 3 empleados de ejemplo nuevos, exista o no una base previa).
            SembrarRolesYPermisos(context);
            SincronizarCatalogoDePermisos(context);
            MigrarUsuariosLegados(context, hasher);

            // Si ya hay sucursales y usuarios, el resto de la base de datos ya fue inicializada
            if (context.Sucursales.Any() && context.Usuarios.Any())
            {
                SembrarEmpleadosNuevosDeRbac(context, hasher);
                SembrarUsuariosWebDeEjemplo(context);
                return;
            }

            // 1. Sucursales
            if (!context.Sucursales.Any())
            {
                context.Sucursales.AddRange(
                    new Sucursal
                    {
                        Nombre = "Sede Central Santo Domingo",
                        Direccion = "Calle Huáscar Tejeda #54, Zona Universitaria",
                        Telefono = "809-533-2833",
                        Encargado = "Lic. Pedro Martínez",
                        Activa = true
                    },
                    new Sucursal
                    {
                        Nombre = "Regional Norte Santiago",
                        Direccion = "Av. Bartolomé Colón, Los Jardines, Santiago",
                        Telefono = "809-582-1234",
                        Encargado = "Licda. Carmen Rosa",
                        Activa = true
                    },
                    new Sucursal
                    {
                        Nombre = "Regional Sur Barahona",
                        Direccion = "Calle Jaime Mota #12, Barahona",
                        Telefono = "809-524-5678",
                        Encargado = "Lic. Rafael Dotel",
                        Activa = true
                    }
                );
                context.SaveChanges();
            }

            // 2. Usuarios
            if (!context.Usuarios.Any())
            {
                var sucursalCentral = context.Sucursales.First();
                var rolesPorNombre = context.CoreRoles.ToDictionary(r => r.Nombre, r => r.Id);

                context.Usuarios.AddRange(
                    new Usuario
                    {
                        NombreCompleto = "Administrador General",
                        NombreUsuario = "admin",
                        Correo = "admin@patronatociegos.org.do",
                        PasswordHash = hasher.HashPassword(default!, "admin123"),
                        RoleId = rolesPorNombre[PermisosCore.RolAdministrador],
                        SucursalId = sucursalCentral.Id,
                        EstadoCuenta = EstadoCuentaCore.Activo
                    },
                    new Usuario
                    {
                        NombreCompleto = "Ana Rosario",
                        NombreUsuario = "cajero",
                        Correo = "caja1@patronatociegos.org.do",
                        PasswordHash = hasher.HashPassword(default!, "cajero123"),
                        RoleId = rolesPorNombre["Cajero"],
                        SucursalId = sucursalCentral.Id,
                        EstadoCuenta = EstadoCuentaCore.Activo
                    },
                    new Usuario
                    {
                        NombreCompleto = "Laura Sánchez",
                        NombreUsuario = "recepcion",
                        Correo = "recepcion@patronatociegos.org.do",
                        PasswordHash = hasher.HashPassword(default!, "recepcion123"),
                        RoleId = rolesPorNombre["Recepcionista"],
                        SucursalId = sucursalCentral.Id,
                        EstadoCuenta = EstadoCuentaCore.Activo
                    },
                    new Usuario
                    {
                        NombreCompleto = "Dra. Carmen Féliz",
                        NombreUsuario = "medico",
                        Correo = "oftalmologia@patronatociegos.org.do",
                        PasswordHash = hasher.HashPassword(default!, "medico123"),
                        RoleId = rolesPorNombre["MedicoOftalmologo"],
                        SucursalId = sucursalCentral.Id,
                        EstadoCuenta = EstadoCuentaCore.Activo
                    },
                    new Usuario
                    {
                        NombreCompleto = "Rafael Núñez",
                        NombreUsuario = "instructor",
                        Correo = "rehabilitacion@patronatociegos.org.do",
                        PasswordHash = hasher.HashPassword(default!, "instructor123"),
                        RoleId = rolesPorNombre["InstructorRehabilitacion"],
                        SucursalId = sucursalCentral.Id,
                        EstadoCuenta = EstadoCuentaCore.Activo
                    }
                );
                context.SaveChanges();
            }

            SembrarEmpleadosNuevosDeRbac(context, hasher);
            SembrarUsuariosWebDeEjemplo(context);

            // 3. Beneficiarios
            if (!context.Beneficiarios.Any())
            {
                context.Beneficiarios.AddRange(
                    new Beneficiario
                    {
                        Nombres = "Juan",
                        Apellidos = "Bautista Rosario",
                        Cedula = "001-1234567-8",
                        Telefono = "809-555-0101",
                        SedeAsignada = SedeEnum_REVIEW.SantoDomingo,
                        CondicionVisual = TipoDiscapacidadVisual.CegueraTotalCongenita,
                        Activo = true
                    },
                    new Beneficiario
                    {
                        Nombres = "María Altagracia",
                        Apellidos = "Cruz Morales",
                        Cedula = "031-9876543-2",
                        Telefono = "809-555-0202",
                        SedeAsignada = SedeEnum_REVIEW.Santiago,
                        CondicionVisual = TipoDiscapacidadVisual.BajaVision,
                        Activo = true
                    },
                    new Beneficiario
                    {
                        Nombres = "Manuel Antonio",
                        Apellidos = "Reyes Medina",
                        Cedula = "018-4567890-1",
                        Telefono = "809-555-0303",
                        SedeAsignada = SedeEnum_REVIEW.Barahona,
                        CondicionVisual = TipoDiscapacidadVisual.CegueraTotalAdquirida,
                        Activo = true
                    }
                );
                context.SaveChanges();
            }

            // 4. Productos de Inventario
            if (!context.ProductosArticulos.Any())
            {
                context.ProductosArticulos.AddRange(
                    new ProductoArticulo
                    {
                        Codigo = "SUAP-01",
                        Nombre = "Suaper Institucional de Algodón",
                        Categoria = "Limpieza",
                        PrecioVenta = 250m,
                        StockDisponible = 50,
                        StockMinimo = 10
                    },
                    new ProductoArticulo
                    {
                        Codigo = "SUAP-02",
                        Nombre = "Suaper Industrial Pesado",
                        Categoria = "Limpieza",
                        PrecioVenta = 380m,
                        StockDisponible = 30,
                        StockMinimo = 10
                    },
                    new ProductoArticulo
                    {
                        Codigo = "BAST-01",
                        Nombre = "Bastón Plegable Blanco para Ciegos (4 Tramos)",
                        Categoria = "Accesibilidad",
                        PrecioVenta = 650m,
                        StockDisponible = 25,
                        StockMinimo = 8
                    },
                    new ProductoArticulo
                    {
                        Codigo = "BAST-02",
                        Nombre = "Bastón Verde (Baja Visión)",
                        Categoria = "Accesibilidad",
                        PrecioVenta = 750m,
                        StockDisponible = 15,
                        StockMinimo = 5
                    },
                    new ProductoArticulo
                    {
                        Codigo = "QUIM-01",
                        Nombre = "Desinfectante Multiusos Lavanda (1 Galón)",
                        Categoria = "Limpieza",
                        PrecioVenta = 320m,
                        StockDisponible = 40,
                        StockMinimo = 10
                    },
                    new ProductoArticulo
                    {
                        Codigo = "QUIM-02",
                        Nombre = "Cloro Concentrado Institucional (1 Galón)",
                        Categoria = "Limpieza",
                        PrecioVenta = 210m,
                        StockDisponible = 45,
                        StockMinimo = 10
                    },
                    new ProductoArticulo
                    {
                        Codigo = "ART-01",
                        Nombre = "Portavasos Artesanal en Macramé (Set de 4)",
                        Categoria = "Artesanías",
                        PrecioVenta = 200m,
                        StockDisponible = 20,
                        StockMinimo = 15 // a propósito por debajo: demuestra la alerta de stock bajo
                    }
                );
                context.SaveChanges();
            }

            // 5. Sesiones de Masaje TACTO (pendientes de facturar en Caja)
            if (!context.SesionesMasaje.Any())
            {
                context.SesionesMasaje.AddRange(
                    new SesionMasajeTacto
                    {
                        NombreCliente = "Carlos Gómez",
                        TelefonoCliente = "809-777-1234",
                        NombreMasajista = "David Silverio (Terapeuta Invidente)",
                        Sede = SedeEnum_REVIEW.SantoDomingo,
                        FechaHora = DateTime.Now.AddHours(-1),
                        DuracionMinutos = 45,
                        PrecioTarifa = 500m,
                        EstaFacturadoEnCaja = false
                    },
                    new SesionMasajeTacto
                    {
                        NombreCliente = "Elena Jiménez",
                        TelefonoCliente = "829-444-5678",
                        NombreMasajista = "Claribel Batista (Terapeuta Invidente)",
                        Sede = SedeEnum_REVIEW.SantoDomingo,
                        FechaHora = DateTime.Now.AddHours(-2),
                        DuracionMinutos = 60,
                        PrecioTarifa = 700m,
                        EstaFacturadoEnCaja = false
                    },
                    new SesionMasajeTacto
                    {
                        NombreCliente = "Roberto Ventura",
                        TelefonoCliente = "809-333-9012",
                        NombreMasajista = "José Medina (Terapeuta Invidente)",
                        Sede = SedeEnum_REVIEW.Santiago,
                        FechaHora = DateTime.Now.AddMinutes(-30),
                        DuracionMinutos = 30,
                        PrecioTarifa = 350m,
                        EstaFacturadoEnCaja = false
                    }
                );
                context.SaveChanges();
            }

            // 6. Préstamos de Emprendimiento (activos para cobro de cuotas)
            if (!context.PrestamosEmprendimiento.Any())
            {
                var primerBeneficiario = context.Beneficiarios.FirstOrDefault();
                var segundoBeneficiario = context.Beneficiarios.Skip(1).FirstOrDefault() ?? primerBeneficiario;

                if (primerBeneficiario != null)
                {
                    context.PrestamosEmprendimiento.Add(
                        new PrestamoEmprendimiento
                        {
                            BeneficiarioId = primerBeneficiario.Id,
                            MontoAprobado = 25000m,
                            SaldoPendiente = 15000m,
                            PlazoMeses = 12,
                            Rubro = RubroMicroemprendimiento.Colmado,
                            EstudioFactibilidadAprobado = true,
                            InvolucraFamilia = true,
                            Estado = EstadoPrestamo.Activo
                        }
                    );

                    if (segundoBeneficiario != null && segundoBeneficiario.Id != primerBeneficiario.Id)
                    {
                        context.PrestamosEmprendimiento.Add(
                            new PrestamoEmprendimiento
                            {
                                BeneficiarioId = segundoBeneficiario.Id,
                                MontoAprobado = 15000m,
                                SaldoPendiente = 6500m,
                                PlazoMeses = 6,
                                Rubro = RubroMicroemprendimiento.Paletera,
                                EstudioFactibilidadAprobado = true,
                                InvolucraFamilia = false,
                                Estado = EstadoPrestamo.Activo
                            }
                        );
                    }
                    context.SaveChanges();
                }
            }
        }

        // Un empleado de ejemplo por cada nivel nuevo de RBAC (Administrador Operativo,
        // Auditor, Operador Logístico), con clave temporal que fuerza cambio al primer
        // inicio de sesión. Se agrega por nombre de usuario individual para que también
        // aparezca en bases que ya tenían los 5 empleados originales de antes del RBAC.
        private static void SembrarEmpleadosNuevosDeRbac(ApplicationDbContext context, PasswordHasher<Usuario> hasher)
        {
            var sucursalCentral = context.Sucursales.FirstOrDefault();
            if (sucursalCentral == null) return;

            var rolesPorNombre = context.CoreRoles.ToDictionary(r => r.Nombre, r => r.Id);

            void SembrarSiNoExiste(string nombreUsuario, string nombreCompleto, string correo, string rol)
            {
                if (context.Usuarios.Any(u => u.NombreUsuario == nombreUsuario)) return;

                context.Usuarios.Add(new Usuario
                {
                    NombreCompleto = nombreCompleto,
                    NombreUsuario = nombreUsuario,
                    Correo = correo,
                    PasswordHash = hasher.HashPassword(default!, "Temporal#2026"),
                    RoleId = rolesPorNombre[rol],
                    SucursalId = sucursalCentral.Id,
                    EstadoCuenta = EstadoCuentaCore.Activo,
                    DebeCambiarPassword = true
                });
            }

            SembrarSiNoExiste("admin_operativo", "Marisol Objío", "operaciones@patronatociegos.org.do", PermisosCore.RolAdminOperativo);
            SembrarSiNoExiste("auditor", "Héctor Paulino", "auditoria@patronatociegos.org.do", PermisosCore.RolAuditor);
            SembrarSiNoExiste("logistica", "Yoel Castillo", "logistica@patronatociegos.org.do", PermisosCore.RolOperadorLogistico);

            context.SaveChanges();
        }

        // Datos de ejemplo para que la pantalla de Accesos Web tenga algo que mostrar/gestionar
        // desde ya. El registro/login público real de clientes queda para una iteración futura.
        private static void SembrarUsuariosWebDeEjemplo(ApplicationDbContext context)
        {
            if (context.WebUsers.Any()) return;

            var webUsers = new[]
            {
                new WebUser { NombreCompleto = "Juan Bautista Rosario", Correo = "juan.bautista@example.com", PasswordHash = "seed-sin-login-real", Estado = EstadoWebUser.Activo, FechaRegistro = DateTime.UtcNow.AddMonths(-3), UltimoLogin = DateTime.UtcNow.AddDays(-2) },
                new WebUser { NombreCompleto = "María Altagracia Cruz", Correo = "maria.cruz@example.com", PasswordHash = "seed-sin-login-real", Estado = EstadoWebUser.Activo, FechaRegistro = DateTime.UtcNow.AddMonths(-2), UltimoLogin = DateTime.UtcNow.AddDays(-10) },
                new WebUser { NombreCompleto = "Manuel Antonio Reyes", Correo = "manuel.reyes@example.com", PasswordHash = "seed-sin-login-real", Estado = EstadoWebUser.Suspendido, FechaRegistro = DateTime.UtcNow.AddMonths(-5), UltimoLogin = DateTime.UtcNow.AddMonths(-1) },
            };
            context.WebUsers.AddRange(webUsers);
            context.SaveChanges();

            foreach (var webUser in webUsers)
            {
                context.WebUserLoginHistorial.Add(new WebUserLoginHistory { WebUserId = webUser.Id, FechaHora = webUser.FechaRegistro, Exitoso = true, Ip = "190.111.0.10" });
                if (webUser.UltimoLogin.HasValue)
                {
                    context.WebUserLoginHistorial.Add(new WebUserLoginHistory { WebUserId = webUser.Id, FechaHora = webUser.UltimoLogin.Value, Exitoso = true, Ip = "190.111.0.11" });
                }
            }
            context.SaveChanges();
        }
    }
}
