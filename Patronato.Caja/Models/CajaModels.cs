using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Patronato.Caja.Models
{
    // ==========================================
    // 1. SESIÓN Y AUTENTICACIÓN
    // ==========================================
    public class UsuarioSession
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = "Sede Central Santo Domingo";
        public DateTime HoraInicioTurno { get; set; } = DateTime.Now;
    }

    public class LoginResponse
    {
        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("usuarioId")]
        public int UsuarioId { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public string Rol { get; set; } = string.Empty;

        [JsonPropertyName("sucursalId")]
        public int SucursalId { get; set; }
    }

    // ==========================================
    // 2. PRODUCTOS Y CARRITO (VENTA POS)
    // ==========================================
    public class ProductoItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("categoria")]
        public string Categoria { get; set; } = string.Empty;

        [JsonPropertyName("precioVenta")]
        public decimal PrecioVenta { get; set; }

        [JsonPropertyName("stockDisponible")]
        public int StockDisponible { get; set; }
    }

    public class CarritoItem : INotifyPropertyChanged
    {
        private int _cantidad;

        public int ProductoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int MaxStock { get; set; }

        public int Cantidad
        {
            get => _cantidad;
            set
            {
                if (_cantidad != value)
                {
                    _cantidad = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        public decimal Subtotal => PrecioUnitario * Cantidad;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class VentaCarritoItemPayload
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    public class VentaCarritoPayload
    {
        public List<VentaCarritoItemPayload> Items { get; set; } = new();
        public string MetodoPago { get; set; } = "Efectivo";
        public decimal MontoRecibido { get; set; }
        public string Cajero { get; set; } = "Caja Principal";
        public string Sucursal { get; set; } = "Sede Central";
        public string? Cliente { get; set; }
        public int? OrdenId { get; set; }
    }

    // ==========================================
    // 3. MASAJES TACTO
    // ==========================================
    public class SesionMasajeItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombreCliente")]
        public string NombreCliente { get; set; } = string.Empty;

        [JsonPropertyName("telefonoCliente")]
        public string TelefonoCliente { get; set; } = string.Empty;

        [JsonPropertyName("nombreMasajista")]
        public string NombreMasajista { get; set; } = string.Empty;

        [JsonPropertyName("sede")]
        public int Sede { get; set; }

        [JsonPropertyName("fechaHora")]
        public DateTime FechaHora { get; set; }

        [JsonPropertyName("duracionMinutos")]
        public int DuracionMinutos { get; set; }

        [JsonPropertyName("precioTarifa")]
        public decimal PrecioTarifa { get; set; }

        [JsonPropertyName("estaFacturadoEnCaja")]
        public bool EstaFacturadoEnCaja { get; set; }
    }

    // ==========================================
    // 4. PRÉSTAMOS DE EMPRENDIMIENTO
    // ==========================================
    public class BeneficiarioInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombres")]
        public string Nombres { get; set; } = string.Empty;

        [JsonPropertyName("apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [JsonPropertyName("cedula")]
        public string Cedula { get; set; } = string.Empty;

        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = string.Empty;

        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    }

    public class PrestamoItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("beneficiarioId")]
        public int BeneficiarioId { get; set; }

        [JsonPropertyName("beneficiario")]
        public BeneficiarioInfo? Beneficiario { get; set; }

        [JsonPropertyName("montoAprobado")]
        public decimal MontoAprobado { get; set; }

        [JsonPropertyName("saldoPendiente")]
        public decimal SaldoPendiente { get; set; }

        [JsonPropertyName("plazoMeses")]
        public int PlazoMeses { get; set; }

        [JsonPropertyName("rubro")]
        public int Rubro { get; set; }

        [JsonPropertyName("estado")]
        public int Estado { get; set; }

        public string RubroTexto => Rubro switch
        {
            1 => "Colmado",
            2 => "Paletera",
            3 => "Venta de Ropa de Paca",
            4 => "Masajes Itinerantes",
            5 => "Elaboración de Detergentes y Suapers",
            6 => "Crianza de Animales y Aves",
            7 => "Siembras Diversas",
            8 => "Venta de Tarjetas de Llamadas",
            9 => "Artesanía y Bisutería",
            _ => "Ventas Diversas"
        };

        public string EstadoTexto => Estado switch
        {
            1 => "Activo",
            2 => "Pagado",
            3 => "En Mora",
            4 => "Exonerado",
            _ => "Desconocido"
        };
    }

    // ==========================================
    // 5. DONACIONES
    // ==========================================
    public class DonacionRequest
    {
        [JsonPropertyName("donante")]
        public string Donante { get; set; } = string.Empty;

        [JsonPropertyName("rncCedula")]
        public string RncCedula { get; set; } = string.Empty;

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("metodoPago")]
        public string MetodoPago { get; set; } = "Efectivo";

        [JsonPropertyName("requiereComprobanteFiscal")]
        public bool RequiereComprobanteFiscal { get; set; }
    }

    // ==========================================
    // 6. TRANSACCIONES Y RECIBOS
    // ==========================================
    public class TransaccionCajaDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("numeroRecibo")]
        public string NumeroRecibo { get; set; } = string.Empty;

        [JsonPropertyName("fechaHora")]
        public DateTime FechaHora { get; set; }

        [JsonPropertyName("tipoTransaccion")]
        public string TipoTransaccion { get; set; } = string.Empty;

        [JsonPropertyName("concepto")]
        public string Concepto { get; set; } = string.Empty;

        [JsonPropertyName("montoTotal")]
        public decimal MontoTotal { get; set; }

        [JsonPropertyName("metodoPago")]
        public string MetodoPago { get; set; } = "Efectivo";

        [JsonPropertyName("montoRecibido")]
        public decimal MontoRecibido { get; set; }

        [JsonPropertyName("devuelta")]
        public decimal Devuelta { get; set; }

        [JsonPropertyName("cajero")]
        public string Cajero { get; set; } = string.Empty;

        [JsonPropertyName("sucursal")]
        public string Sucursal { get; set; } = string.Empty;

        [JsonPropertyName("clienteOBeneficiario")]
        public string? ClienteOBeneficiario { get; set; }

        [JsonPropertyName("identificacionCliente")]
        public string? IdentificacionCliente { get; set; }

        [JsonPropertyName("detalleLineas")]
        public string? DetalleLineas { get; set; }

        [JsonPropertyName("referenciaId")]
        public int? ReferenciaId { get; set; }
    }

    // ==========================================
    // 7. CUADRE Y ARQUEO DE CAJA
    // ==========================================
    public class CuadreMetodoDto
    {
        [JsonPropertyName("efectivo")]
        public decimal Efectivo { get; set; }

        [JsonPropertyName("tarjeta")]
        public decimal Tarjeta { get; set; }

        [JsonPropertyName("transferencia")]
        public decimal Transferencia { get; set; }
    }

    public class CuadreConceptoDto
    {
        [JsonPropertyName("ventasProductos")]
        public decimal VentasProductos { get; set; }

        [JsonPropertyName("masajesTacto")]
        public decimal MasajesTacto { get; set; }

        [JsonPropertyName("abonosPrestamos")]
        public decimal AbonosPrestamos { get; set; }

        [JsonPropertyName("donaciones")]
        public decimal Donaciones { get; set; }
    }

    public class CuadreCajaDto
    {
        [JsonPropertyName("fecha")]
        public string Fecha { get; set; } = string.Empty;

        [JsonPropertyName("cajero")]
        public string Cajero { get; set; } = string.Empty;

        [JsonPropertyName("cantidadTransacciones")]
        public int CantidadTransacciones { get; set; }

        [JsonPropertyName("totalGeneral")]
        public decimal TotalGeneral { get; set; }

        [JsonPropertyName("porMetodo")]
        public CuadreMetodoDto PorMetodo { get; set; } = new();

        [JsonPropertyName("porConcepto")]
        public CuadreConceptoDto PorConcepto { get; set; } = new();

        [JsonPropertyName("movimientos")]
        public List<TransaccionCajaDto> Movimientos { get; set; } = new();
    }

    // ==========================================
    // 8. ADMINISTRACIÓN DE USUARIOS Y CAJEROS
    // ==========================================
    public class UsuarioDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombreCompleto")]
        public string NombreCompleto { get; set; } = string.Empty;

        [JsonPropertyName("nombreUsuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;

        [JsonPropertyName("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public int Rol { get; set; }

        [JsonPropertyName("sucursalId")]
        public int SucursalId { get; set; }

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        public string RolTexto => Rol switch
        {
            1 => "Administrador",
            2 => "Cajero",
            3 => "Médico Oftalmólogo",
            4 => "Instructor Rehabilitación",
            5 => "Recepcionista",
            _ => "Empleado"
        };

        public string SucursalTexto => SucursalId switch
        {
            1 => "Sede Central Santo Domingo",
            2 => "Regional Norte Santiago",
            3 => "Regional Sur Barahona",
            _ => "Sede General"
        };
    }

    // ==========================================
    // 9. CLIENTES FRECUENTES Y REGISTROS
    // ==========================================
    public class ClienteFrecuenteDto
    {
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("documento")]
        public string Documento { get; set; } = string.Empty;

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        public string DisplayTexto => string.IsNullOrWhiteSpace(Documento) 
            ? $"{Nombre} ({Tipo})" 
            : $"{Nombre} - {Documento} ({Tipo})";
    }

    public class CrearMasajeDto
    {
        public string NombreCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string NombreMasajista { get; set; } = "David Silverio (Terapeuta Invidente)";
        public int Sede { get; set; } = 1;
        public int DuracionMinutos { get; set; } = 45;
        public decimal PrecioTarifa { get; set; } = 500m;
    }

    public class CrearPrestamoDto
    {
        public int BeneficiarioId { get; set; }
        public decimal MontoAprobado { get; set; }
        public int PlazoMeses { get; set; } = 12;
        public int Rubro { get; set; } = 1;
        public bool InvolucraFamilia { get; set; } = true;
    }

    // ==========================================
    // 10. ÓRDENES DE PEDIDO Y COTIZACIONES (RECEPCIÓN Y CAJA)
    // ==========================================
    public class OrdenPedidoCajaDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("numeroOrden")]
        public string NumeroOrden { get; set; } = string.Empty;

        [JsonPropertyName("fechaHora")]
        public DateTime FechaHora { get; set; }

        [JsonPropertyName("clienteNombre")]
        public string ClienteNombre { get; set; } = string.Empty;

        [JsonPropertyName("clienteDocumento")]
        public string? ClienteDocumento { get; set; }

        [JsonPropertyName("creadoPor")]
        public string CreadoPor { get; set; } = string.Empty;

        [JsonPropertyName("estado")]
        public string Estado { get; set; } = "Pendiente";

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("itemsJson")]
        public string ItemsJson { get; set; } = "[]";

        [JsonPropertyName("notas")]
        public string? Notas { get; set; }

        public string ResumenTexto => $"{NumeroOrden} | {ClienteNombre} (RD$ {Total:N2}) - {CreadoPor}";
    }

    public class CrearOrdenDto
    {
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteDocumento { get; set; }
        public string CreadoPor { get; set; } = "Recepción";
        public string? Notas { get; set; }
        public List<OrdenItemDto> Items { get; set; } = new();
    }

    public class OrdenItemDto
    {
        public int ProductoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}
