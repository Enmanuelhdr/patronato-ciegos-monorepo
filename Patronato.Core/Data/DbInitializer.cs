using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Entities;
using Patronato.Core.Enums;

namespace Patronato.Core.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
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

            // Si ya hay sucursales y usuarios, el resto de la base de datos ya fue inicializada
            if (context.Sucursales.Any() && context.Usuarios.Any())
            {
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
                context.Usuarios.AddRange(
                    new Usuario
                    {
                        NombreCompleto = "Administrador General",
                        NombreUsuario = "admin",
                        Correo = "admin@patronatociegos.org.do",
                        PasswordHash = "admin123",
                        Rol = RolUsuario.Administrador,
                        SucursalId = sucursalCentral.Id,
                        Activo = true
                    },
                    new Usuario
                    {
                        NombreCompleto = "Ana Rosario",
                        NombreUsuario = "cajero",
                        Correo = "caja1@patronatociegos.org.do",
                        PasswordHash = "cajero123",
                        Rol = RolUsuario.Cajero,
                        SucursalId = sucursalCentral.Id,
                        Activo = true
                    },
                    new Usuario
                    {
                        NombreCompleto = "Laura Sánchez",
                        NombreUsuario = "recepcion",
                        Correo = "recepcion@patronatociegos.org.do",
                        PasswordHash = "recepcion123",
                        Rol = RolUsuario.Recepcionista,
                        SucursalId = sucursalCentral.Id,
                        Activo = true
                    }
                );
                context.SaveChanges();
            }

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
                        StockDisponible = 50
                    },
                    new ProductoArticulo
                    {
                        Codigo = "SUAP-02",
                        Nombre = "Suaper Industrial Pesado",
                        Categoria = "Limpieza",
                        PrecioVenta = 380m,
                        StockDisponible = 30
                    },
                    new ProductoArticulo
                    {
                        Codigo = "BAST-01",
                        Nombre = "Bastón Plegable Blanco para Ciegos (4 Tramos)",
                        Categoria = "Accesibilidad",
                        PrecioVenta = 650m,
                        StockDisponible = 25
                    },
                    new ProductoArticulo
                    {
                        Codigo = "BAST-02",
                        Nombre = "Bastón Verde (Baja Visión)",
                        Categoria = "Accesibilidad",
                        PrecioVenta = 750m,
                        StockDisponible = 15
                    },
                    new ProductoArticulo
                    {
                        Codigo = "QUIM-01",
                        Nombre = "Desinfectante Multiusos Lavanda (1 Galón)",
                        Categoria = "Limpieza",
                        PrecioVenta = 320m,
                        StockDisponible = 40
                    },
                    new ProductoArticulo
                    {
                        Codigo = "QUIM-02",
                        Nombre = "Cloro Concentrado Institucional (1 Galón)",
                        Categoria = "Limpieza",
                        PrecioVenta = 210m,
                        StockDisponible = 45
                    },
                    new ProductoArticulo
                    {
                        Codigo = "ART-01",
                        Nombre = "Portavasos Artesanal en Macramé (Set de 4)",
                        Categoria = "Artesanías",
                        PrecioVenta = 200m,
                        StockDisponible = 20
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
    }
}
