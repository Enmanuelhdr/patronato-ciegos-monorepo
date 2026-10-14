using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Entities;

namespace Patronato.Core.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        // TABLAS DEL PATRONATO EN LA BASE DE DATOS
        public DbSet<Beneficiario> Beneficiarios { get; set; }
        public DbSet<PrestamoEmprendimiento> PrestamosEmprendimiento { get; set; }
        public DbSet<SesionMasajeTacto> SesionesMasaje { get; set; }
        public DbSet<ConsultaOftalmologica> ConsultasOftalmologicas { get; set; }
        public DbSet<InscripcionRehabilitacion> InscripcionesRehabilitacion { get; set; }
        public DbSet<ProductoArticulo> ProductosArticulos { get; set; }
        public DbSet<EmpresaAliada> EmpresasAliadas { get; set; }
        public DbSet<Donacion> Donaciones { get; set; }
        public DbSet<Voluntario> Voluntarios { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<TransaccionCaja> TransaccionesCaja { get; set; }
        public DbSet<OrdenPedidoCaja> OrdenesPedidoCaja { get; set; }
        public DbSet<SolicitudCita> SolicitudesCita { get; set; }

        // RBAC del Core (usuarios internos) y gestión de accesos
        public DbSet<CoreRole> CoreRoles { get; set; }
        public DbSet<CorePermission> CorePermissions { get; set; }
        public DbSet<CoreRolePermission> CoreRolePermissions { get; set; }

        // Usuarios de la plataforma Web pública (clientes/beneficiarios externos),
        // completamente separados de Usuario/CoreRole.
        public DbSet<WebUser> WebUsers { get; set; }
        public DbSet<WebUserLoginHistory> WebUserLoginHistorial { get; set; }
        public DbSet<WebUserSession> WebUserSessions { get; set; }

        // Bitácora de movimientos de Inventario
        public DbSet<InventarioMovimiento> InventarioMovimientos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CoreRole>()
                .HasIndex(r => r.Nombre)
                .IsUnique();

            modelBuilder.Entity<CorePermission>()
                .HasIndex(p => p.Codigo)
                .IsUnique();

            modelBuilder.Entity<CoreRolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            modelBuilder.Entity<WebUser>()
                .HasIndex(w => w.Correo)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.NombreUsuario)
                .IsUnique();
        }
    }

}

