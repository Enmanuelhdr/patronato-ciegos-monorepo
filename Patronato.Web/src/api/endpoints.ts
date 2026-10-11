// Todas las rutas son exactamente las del Core (ver Patronato.Core/Controllers): la Web
// las llama contra el Gateway, que las reenvía transparentemente (ver
// Patronato.Gateway.Services.ReverseProxyService + Program.cs -> MapFallback).
//
// La Web NUNCA cobra dinero (eso es exclusivo de la Caja, vía los códigos de transacción
// del socket): aquí solo viven las pantallas de registro/consulta de Recepción y Admin
// que se sacaron de la Caja porque el propio Core las documenta como tareas de Recepción.
import { api } from "./client";
import type {
  Beneficiario,
  ConsultaOftalmologica,
  CuadreCaja,
  Donacion,
  InscripcionRehabilitacion,
  LoginResponse,
  OrdenPedidoCaja,
  PrestamoEmprendimiento,
  ProductoArticulo,
  SesionMasajeTacto,
  Sucursal,
  TransaccionCaja,
  Usuario,
  Voluntario,
} from "./types";

// ---------- Autenticación ----------
export const loginApi = (nombreUsuario: string, password: string) =>
  api.post<LoginResponse>(
    `/api/Usuarios/login?nombreUsuario=${encodeURIComponent(nombreUsuario)}&password=${encodeURIComponent(password)}`
  );

// ---------- Usuarios / Empleados ----------
export const getUsuarios = () => api.get<Usuario[]>("/api/Usuarios");

export interface CrearUsuarioPayload {
  nombreCompleto: string;
  nombreUsuario: string;
  correo: string;
  passwordHash: string;
  rol: number;
  sucursalId: number;
  activo: boolean;
}
export const crearUsuario = (payload: CrearUsuarioPayload) => api.post<Usuario>("/api/Usuarios", payload);

export const cambiarRolUsuario = (id: number, nuevoRol: number) =>
  api.put<{ mensaje: string }>(`/api/Usuarios/${id}/cambiar-rol?nuevoRol=${nuevoRol}`);

// ---------- Beneficiarios ----------
export const getBeneficiarios = () => api.get<Beneficiario[]>("/api/Beneficiarios");

export interface CrearBeneficiarioPayload {
  nombres: string;
  apellidos: string;
  cedula: string;
  telefono: string;
  sedeAsignada: number;
  condicionVisual: number;
  activo: boolean;
}
export const crearBeneficiario = (payload: CrearBeneficiarioPayload) =>
  api.post<Beneficiario>("/api/Beneficiarios", payload);

// ---------- Sucursales ----------
export const getSucursales = () => api.get<Sucursal[]>("/api/Sucursales");

export interface CrearSucursalPayload {
  nombre: string;
  direccion: string;
  telefono: string;
  encargado: string;
  activa: boolean;
}
export const crearSucursal = (payload: CrearSucursalPayload) => api.post<Sucursal>("/api/Sucursales", payload);
export const editarSucursal = (id: number, payload: Sucursal) => api.put<void>(`/api/Sucursales/${id}`, payload);

// ---------- Consultas Oftalmológicas ----------
export const getConsultas = () => api.get<ConsultaOftalmologica[]>("/api/Consultas");

export interface CrearConsultaPayload {
  beneficiarioId: number;
  sede: number;
  oftalmologo: string;
  diagnosticoOcular: string;
  tratamientoPrescrito: string;
  tarifaConsulta: number;
  cubiertoPorARS: boolean;
}
export const crearConsulta = (payload: CrearConsultaPayload) => api.post<ConsultaOftalmologica>("/api/Consultas", payload);

export const registrarDiagnostico = (id: number, diagnostico: string, tratamiento: string) =>
  api.put<{ mensaje: string }>(
    `/api/Consultas/${id}/diagnostico?diagnostico=${encodeURIComponent(diagnostico)}&tratamiento=${encodeURIComponent(tratamiento)}`
  );

// ---------- Rehabilitación ----------
export const getInscripciones = () => api.get<InscripcionRehabilitacion[]>("/api/Rehabilitacion");

export interface CrearInscripcionPayload {
  beneficiarioId: number;
  sede: number;
  etapa: number;
  tallerMateria: string;
}
export const crearInscripcion = (payload: CrearInscripcionPayload) =>
  api.post<InscripcionRehabilitacion>("/api/Rehabilitacion", payload);

export const graduarAlumno = (id: number) => api.put<{ mensaje: string }>(`/api/Rehabilitacion/${id}/graduar`);

// ---------- Voluntarios y Donaciones (reporte) ----------
export const getVoluntarios = () => api.get<Voluntario[]>("/api/Voluntarios");

export interface CrearVoluntarioPayload {
  nombreCompleto: string;
  cedula: string;
  telefono: string;
  correo: string;
  areaApoyo: string;
  activo: boolean;
}
export const crearVoluntario = (payload: CrearVoluntarioPayload) => api.post<Voluntario>("/api/Voluntarios", payload);

export const getDonaciones = () => api.get<Donacion[]>("/api/Voluntarios/donaciones");

// ---------- Inventario (solo lectura, para armar órdenes de pedido) ----------
export const getInventario = () => api.get<ProductoArticulo[]>("/api/inventario");

// ---------- Masajes TACTO (agendar; el cobro es exclusivo de Caja) ----------
export const getMasajesPendientes = () => api.get<SesionMasajeTacto[]>("/api/caja/masajes-pendientes");

export interface CrearMasajePayload {
  nombreCliente: string;
  telefonoCliente: string;
  nombreMasajista: string;
  sede: number;
  duracionMinutos: number;
  precioTarifa: number;
}
export const crearMasaje = (payload: CrearMasajePayload) =>
  api.post<{ mensaje: string; sesion: SesionMasajeTacto }>("/api/caja/crear-masaje", payload);

// ---------- Préstamos de Emprendimiento (otorgar; el abono es exclusivo de Caja) ----------
export const getPrestamosActivos = (filtro?: string) =>
  api.get<PrestamoEmprendimiento[]>(`/api/caja/prestamos-activos${filtro ? `?filtro=${encodeURIComponent(filtro)}` : ""}`);

export interface CrearPrestamoPayload {
  beneficiarioId: number;
  montoAprobado: number;
  plazoMeses: number;
  rubro: number;
  involucraFamilia: boolean;
}
export const crearPrestamo = (payload: CrearPrestamoPayload) =>
  api.post<{ mensaje: string; prestamoId: number }>("/api/caja/crear-prestamo", payload);

// ---------- Órdenes de Pedido (Recepción genera; Caja cobra) ----------
export const getOrdenesPendientes = () => api.get<OrdenPedidoCaja[]>("/api/caja/ordenes-pendientes");

export interface CrearOrdenItemPayload {
  productoId: number;
  codigo: string;
  nombre: string;
  cantidad: number;
  precioUnitario: number;
}
export interface CrearOrdenPayload {
  clienteNombre: string;
  clienteDocumento?: string;
  creadoPor: string;
  notas?: string;
  items: CrearOrdenItemPayload[];
}
export const crearOrden = (payload: CrearOrdenPayload) =>
  api.post<{ mensaje: string; orden: OrdenPedidoCaja }>("/api/caja/crear-orden", payload);

// ---------- Reportes de Caja (solo lectura, visibilidad administrativa) ----------
export const getCuadreCaja = (fecha?: string, cajero?: string) => {
  const params = new URLSearchParams();
  if (fecha) params.set("fecha", fecha);
  if (cajero) params.set("cajero", cajero);
  return api.get<CuadreCaja>(`/api/caja/cuadre-caja${params.toString() ? `?${params}` : ""}`);
};

export const getTransacciones = (fecha?: string, cajero?: string) => {
  const params = new URLSearchParams();
  if (fecha) params.set("fecha", fecha);
  if (cajero) params.set("cajero", cajero);
  return api.get<TransaccionCaja[]>(`/api/caja/transacciones${params.toString() ? `?${params}` : ""}`);
};
