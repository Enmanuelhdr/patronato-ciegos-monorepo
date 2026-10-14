// Tipos que reflejan las entidades reales del Core (Patronato.Core/Entities y Enums).
// Los nombres de campo son camelCase porque ASP.NET Core serializa así por defecto
// (AddControllers() usa JsonNamingPolicy.CamelCase) — confirmado contra el Core en vivo.

// El rol ya no es un enum fijo: viene del Core (tabla CoreRole) vía /api/CoreRoles,
// así que admite tantos roles como el Core tenga sembrados (ver CoreRolesController).
export interface CoreRole {
  id: number;
  nombre: string;
  descripcion: string;
  esSistema: boolean;
}

export type EstadoCuenta = "Activo" | "Suspendido" | "Revocado";
export type EstadoWebUser = "Activo" | "Suspendido";

export type SedeEnum = 1 | 2 | 3;
export const SedeTexto: Record<SedeEnum, string> = {
  1: "Santo Domingo (Sede Central)",
  2: "Santiago (Regional Norte)",
  3: "Barahona (Regional Sur)",
};

export type TipoDiscapacidadVisual = 1 | 2 | 3;
export const TipoDiscapacidadTexto: Record<TipoDiscapacidadVisual, string> = {
  1: "Baja Visión",
  2: "Ceguera Total Congénita",
  3: "Ceguera Total Adquirida",
};

export type RubroMicroemprendimiento = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10;
export const RubroTexto: Record<RubroMicroemprendimiento, string> = {
  1: "Colmado",
  2: "Paletera",
  3: "Venta de Ropa de Paca",
  4: "Masajes Itinerantes",
  5: "Elaboración de Detergentes y Suapers",
  6: "Crianza de Animales y Aves",
  7: "Siembras Diversas",
  8: "Venta de Tarjetas de Llamadas",
  9: "Artesanía y Bisutería",
  10: "Ventas Diversas",
};

export type EstadoPrestamo = 1 | 2 | 3 | 4;
export const EstadoPrestamoTexto: Record<EstadoPrestamo, string> = {
  1: "Activo",
  2: "Pagado",
  3: "En Mora",
  4: "Exonerado",
};

export type EtapaRehabilitacion = 1 | 2;
export const EtapaTexto: Record<EtapaRehabilitacion, string> = {
  1: "Funcional (independencia personal)",
  2: "Profesional (inserción laboral)",
};

export type TipoServicioCita = 1 | 2 | 3 | 4;
export const TipoServicioCitaTexto: Record<TipoServicioCita, string> = {
  1: "Consulta Oftalmológica",
  2: "Rehabilitación (Braille / bastón / oficios)",
  3: "Masaje TACTO",
  4: "Otro",
};

export type EstadoCita = 1 | 2 | 3;
export const EstadoCitaTexto: Record<EstadoCita, string> = {
  1: "Pendiente",
  2: "Confirmada",
  3: "Cancelada",
};

export interface Beneficiario {
  id: number;
  nombres: string;
  apellidos: string;
  cedula: string;
  telefono: string;
  sedeAsignada: SedeEnum;
  condicionVisual: TipoDiscapacidadVisual;
  fechaIngreso: string;
  activo: boolean;
}

export interface Sucursal {
  id: number;
  nombre: string;
  direccion: string;
  telefono: string;
  encargado: string;
  activa: boolean;
}

export interface Usuario {
  id: number;
  nombreCompleto: string;
  nombreUsuario: string;
  correo: string;
  roleId: number;
  roleNombre: string;
  sucursalId: number;
  estadoCuenta: EstadoCuenta;
  debeCambiarPassword: boolean;
  ultimoLogin: string | null;
  fechaCreacion: string;
}

export interface LoginResponse {
  mensaje: string;
  usuarioId: number;
  nombre: string;
  rol: string;
  sucursalId: number;
  token: string;
  permisos: string[];
  debeCambiarPassword: boolean;
}

export interface ConsultaOftalmologica {
  id: number;
  beneficiarioId: number;
  beneficiario?: Beneficiario | null;
  sede: SedeEnum;
  oftalmologo: string;
  fecha: string;
  diagnosticoOcular: string;
  tratamientoPrescrito: string;
  tarifaConsulta: number;
  cubiertoPorARS: boolean;
  estaFacturadaEnCaja: boolean;
}

export interface InscripcionRehabilitacion {
  id: number;
  beneficiarioId: number;
  beneficiario?: Beneficiario | null;
  sede: SedeEnum;
  etapa: EtapaRehabilitacion;
  tallerMateria: string;
  fechaInicio: string;
  completado: boolean;
}

export interface Voluntario {
  id: number;
  nombreCompleto: string;
  cedula: string;
  telefono: string;
  correo: string;
  areaApoyo: string;
  fechaRegistro: string;
  activo: boolean;
}

export interface Donacion {
  id: number;
  donante: string;
  rncCedula: string;
  monto: number;
  fecha: string;
  metodoPago: string;
  requiereComprobanteFiscal: boolean;
  estaRegistradaEnCaja: boolean;
}

export interface ProductoArticulo {
  id: number;
  codigo: string;
  nombre: string;
  categoria: string;
  precioVenta: number;
  stockDisponible: number;
  stockMinimo: number;
  activo: boolean;
  stockBajo: boolean;
}

export interface PaginaResultado<T> {
  total: number;
  pagina: number;
  tamanoPagina: number;
  items: T[];
}

export interface InventarioMovimiento {
  id: number;
  productoId: number;
  tipo: "Entrada" | "Salida" | "Ajuste";
  cantidad: number;
  stockResultante: number;
  motivo?: string | null;
  coreUserNombre: string;
  fecha: string;
}

export interface WebUser {
  id: number;
  nombreCompleto: string;
  correo: string;
  telefono?: string | null;
  estado: EstadoWebUser;
  fechaRegistro: string;
  ultimoLogin?: string | null;
  debeCambiarPassword: boolean;
}

export interface WebUserLoginHistorial {
  id: number;
  fechaHora: string;
  exitoso: boolean;
  ip?: string | null;
  userAgent?: string | null;
}

export interface SesionMasajeTacto {
  id: number;
  nombreCliente: string;
  telefonoCliente: string;
  nombreMasajista: string;
  sede: SedeEnum;
  fechaHora: string;
  duracionMinutos: number;
  precioTarifa: number;
  estaFacturadoEnCaja: boolean;
}

export interface PrestamoEmprendimiento {
  id: number;
  beneficiarioId: number;
  beneficiario?: Beneficiario | null;
  montoAprobado: number;
  saldoPendiente: number;
  plazoMeses: number;
  rubro: RubroMicroemprendimiento;
  estudioFactibilidadAprobado: boolean;
  involucraFamilia: boolean;
  estado: EstadoPrestamo;
  justificacionExoneracion: string;
}

export interface OrdenPedidoCaja {
  id: number;
  numeroOrden: string;
  fechaHora: string;
  clienteNombre: string;
  clienteDocumento?: string | null;
  creadoPor: string;
  estado: string;
  total: number;
  itemsJson: string;
  notas?: string | null;
}

export interface TransaccionCaja {
  id: number;
  numeroRecibo: string;
  fechaHora: string;
  tipoTransaccion: string;
  concepto: string;
  montoTotal: number;
  metodoPago: string;
  montoRecibido: number;
  devuelta: number;
  cajero: string;
  sucursal: string;
  clienteOBeneficiario?: string | null;
  identificacionCliente?: string | null;
  detalleLineas?: string | null;
  referenciaId?: number | null;
}

export interface SolicitudCita {
  id: number;
  nombreSolicitante: string;
  telefono: string;
  correo: string;
  tipoServicio: TipoServicioCita;
  sedePreferida: SedeEnum;
  fechaPreferida: string;
  comentario?: string | null;
  estado: EstadoCita;
  fechaSolicitud: string;
}

export interface CuadreCaja {
  fecha: string;
  cajero: string;
  cantidadTransacciones: number;
  totalGeneral: number;
  porMetodo: { efectivo: number; tarjeta: number; transferencia: number };
  porConcepto: {
    ventasProductos: number;
    masajesTacto: number;
    abonosPrestamos: number;
    donaciones: number;
    consultas: number;
  };
  movimientos: TransaccionCaja[];
}
