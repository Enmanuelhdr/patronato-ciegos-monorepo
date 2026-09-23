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
    }

}

