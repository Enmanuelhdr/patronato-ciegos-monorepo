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

        // True cuando este turno se abrió sin conexión con el Gateway, validando contra las
        // credenciales guardadas del último login exitoso (ver OfflineCredentialsService).
        public bool EsSesionOffline { get; set; } = false;
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
    // 3.1. CONSULTAS OFTALMOLÓGICAS
    // ==========================================
    // Las cubiertas por ARS/SENASA nunca aparecen aquí: el Core las excluye de
    // consultas-pendientes porque no hay nada que cobrar en efectivo en la Caja.
    public class ConsultaItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("beneficiarioId")]
        public int BeneficiarioId { get; set; }

        [JsonPropertyName("beneficiario")]
        public BeneficiarioInfo? Beneficiario { get; set; }

        [JsonPropertyName("sede")]
        public int Sede { get; set; }

        [JsonPropertyName("oftalmologo")]
        public string Oftalmologo { get; set; } = string.Empty;

        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("diagnosticoOcular")]
        public string DiagnosticoOcular { get; set; } = string.Empty;

        [JsonPropertyName("tarifaConsulta")]
        public decimal TarifaConsulta { get; set; }

        [JsonPropertyName("cubiertoPorARS")]
        public bool CubiertoPorARS { get; set; }

        [JsonPropertyName("estaFacturadaEnCaja")]
        public bool EstaFacturadaEnCaja { get; set; }

        public string NombrePaciente => Beneficiario?.NombreCompleto ?? $"Beneficiario #{BeneficiarioId}";
    }

    // ==========================================
    // 4. PRÉSTAMOS DE EMPRENDIMIENTO
    // ==========================================
    // Viene embebido en PrestamoItem.Beneficiario (el Core lo incluye en prestamos-activos).
    // Otorgar un préstamo nuevo no es función de esta terminal de cobro.
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

        [JsonPropertyName("esAnulada")]
        public bool EsAnulada { get; set; }

        [JsonPropertyName("motivoAnulacion")]
        public string? MotivoAnulacion { get; set; }

        [JsonPropertyName("fechaHoraAnulacion")]
        public DateTime? FechaHoraAnulacion { get; set; }

        [JsonPropertyName("anuladoPor")]
        public string? AnuladoPor { get; set; }

        [JsonPropertyName("turnoId")]
        public int? TurnoId { get; set; }
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

        [JsonPropertyName("consultas")]
        public decimal Consultas { get; set; }
    }

    public class CuadreCajaDto
    {
        [JsonPropertyName("fecha")]
        public string Fecha { get; set; } = string.Empty;

        [JsonPropertyName("cajero")]
        public string Cajero { get; set; } = string.Empty;

        [JsonPropertyName("tieneTurnoActivo")]
        public bool TieneTurnoActivo { get; set; }

        [JsonPropertyName("turnoId")]
        public int? TurnoId { get; set; }

        [JsonPropertyName("cantidadTransacciones")]
        public int CantidadTransacciones { get; set; }

        [JsonPropertyName("totalGeneral")]
        public decimal TotalGeneral { get; set; }

        [JsonPropertyName("fondoInicial")]
        public decimal FondoInicial { get; set; }

        [JsonPropertyName("efectivoEsperadoEnCaja")]
        public decimal EfectivoEsperadoEnCaja { get; set; }

        [JsonPropertyName("cantidadAnuladas")]
        public int CantidadAnuladas { get; set; }

        [JsonPropertyName("totalAnulado")]
        public decimal TotalAnulado { get; set; }

        [JsonPropertyName("porMetodo")]
        public CuadreMetodoDto PorMetodo { get; set; } = new();

        [JsonPropertyName("porConcepto")]
        public CuadreConceptoDto PorConcepto { get; set; } = new();

        [JsonPropertyName("movimientos")]
        public List<TransaccionCajaDto> Movimientos { get; set; } = new();
    }

    // ==========================================
    // 8. SUCURSALES (nombre real desde el Core, no hardcodeado en la Caja)
    // ==========================================
    public class SucursalDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
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

    // ==========================================
    // 10. ÓRDENES DE PEDIDO PENDIENTES DE COBRO
    // ==========================================
    // La orden se genera fuera de esta terminal (Recepción/Web); la Caja solo la
    // carga al carrito para cobrarla (ver BtnCargarOrden_Click en MainWindow).
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

    public class OrdenItemDto
    {
        public int ProductoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    // ==========================================
    // 11. GESTIÓN DE TURNOS Y CONTEO DE EFECTIVO
    // ==========================================
    public class TurnoCajaDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("cajero")]
        public string Cajero { get; set; } = string.Empty;

        [JsonPropertyName("sucursal")]
        public string Sucursal { get; set; } = string.Empty;

        [JsonPropertyName("fechaHoraApertura")]
        public DateTime FechaHoraApertura { get; set; }

        [JsonPropertyName("fondoInicial")]
        public decimal FondoInicial { get; set; }

        [JsonPropertyName("estado")]
        public string Estado { get; set; } = "Abierto";

        [JsonPropertyName("fechaHoraCierre")]
        public DateTime? FechaHoraCierre { get; set; }

        [JsonPropertyName("totalVentasEfectivo")]
        public decimal TotalVentasEfectivo { get; set; }

        [JsonPropertyName("totalVentasTarjeta")]
        public decimal TotalVentasTarjeta { get; set; }

        [JsonPropertyName("totalVentasTransferencia")]
        public decimal TotalVentasTransferencia { get; set; }

        [JsonPropertyName("totalGeneralVentas")]
        public decimal TotalGeneralVentas { get; set; }

        [JsonPropertyName("efectivoEsperado")]
        public decimal EfectivoEsperado { get; set; }

        [JsonPropertyName("efectivoContado")]
        public decimal? EfectivoContado { get; set; }

        [JsonPropertyName("diferencia")]
        public decimal? Diferencia { get; set; }

        [JsonPropertyName("desgloseDenominacionesJson")]
        public string? DesgloseDenominacionesJson { get; set; }

        [JsonPropertyName("observacionesApertura")]
        public string? ObservacionesApertura { get; set; }

        [JsonPropertyName("observacionesCierre")]
        public string? ObservacionesCierre { get; set; }

        [JsonPropertyName("montoEntregadoBoveda")]
        public decimal MontoEntregadoBoveda { get; set; }

        [JsonPropertyName("fondoRemanenteCaja")]
        public decimal FondoRemanenteCaja { get; set; }

        [JsonPropertyName("destinoFondos")]
        public string DestinoFondos { get; set; } = "RetiroTotalBoveda";
    }

    public class TurnoActivoResponseDto
    {
        [JsonPropertyName("tieneTurnoActivo")]
        public bool TieneTurnoActivo { get; set; }

        [JsonPropertyName("turno")]
        public TurnoCajaDto? Turno { get; set; }

        [JsonPropertyName("mensaje")]
        public string? Mensaje { get; set; }
    }

    public class AbrirTurnoRequest
    {
        [JsonPropertyName("fondoInicial")]
        public decimal FondoInicial { get; set; }

        [JsonPropertyName("cajero")]
        public string Cajero { get; set; } = string.Empty;

        [JsonPropertyName("sucursal")]
        public string Sucursal { get; set; } = string.Empty;

        [JsonPropertyName("observaciones")]
        public string? Observaciones { get; set; }
    }

    public class CerrarTurnoRequest
    {
        [JsonPropertyName("turnoId")]
        public int TurnoId { get; set; }

        [JsonPropertyName("efectivoContado")]
        public decimal EfectivoContado { get; set; }

        [JsonPropertyName("desgloseDenominacionesJson")]
        public string DesgloseDenominacionesJson { get; set; } = "{}";

        [JsonPropertyName("observaciones")]
        public string? Observaciones { get; set; }

        [JsonPropertyName("montoEntregadoBoveda")]
        public decimal? MontoEntregadoBoveda { get; set; }

        [JsonPropertyName("fondoRemanenteCaja")]
        public decimal? FondoRemanenteCaja { get; set; }

        [JsonPropertyName("destinoFondos")]
        public string? DestinoFondos { get; set; }
    }

    public class AnularTransaccionRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("usuarioSupervisor")]
        public string UsuarioSupervisor { get; set; } = string.Empty;

        [JsonPropertyName("passwordSupervisor")]
        public string PasswordSupervisor { get; set; } = string.Empty;

        [JsonPropertyName("motivo")]
        public string Motivo { get; set; } = string.Empty;
    }

    public class DenominacionItem : INotifyPropertyChanged
    {
        private int _cantidad;

        public decimal Valor { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool EsBillete { get; set; } = true;

        public int Cantidad
        {
            get => _cantidad;
            set
            {
                if (_cantidad != value)
                {
                    _cantidad = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public decimal Total => Valor * Cantidad;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
