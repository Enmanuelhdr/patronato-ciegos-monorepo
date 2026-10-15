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
  CoreRole,
  CuadreCaja,
  Donacion,
  EstadoCuenta,
  EstadoWebUser,
  InscripcionRehabilitacion,
  InventarioMovimiento,
  LoginResponse,
  OrdenPedidoCaja,
  PaginaResultado,
  PrestamoEmprendimiento,
  ProductoArticulo,
  SesionMasajeTacto,
  SolicitudCita,
  Sucursal,
  TransaccionCaja,
  Usuario,
  Voluntario,
  WebUser,
  WebUserLoginHistorial,
} from "./types";

// ---------- Autenticación ----------
export const loginApi = (nombreUsuario: string, password: string) =>
  api.post<LoginResponse>(
    `/api/Usuarios/login?nombreUsuario=${encodeURIComponent(nombreUsuario)}&password=${encodeURIComponent(password)}`
  );

export const cambiarPasswordPropia = (passwordActual: string, passwordNueva: string) =>
  api.post<{ mensaje: string }>("/api/Usuarios/cambiar-password", { passwordActual, passwordNueva });

// ---------- Roles del Core (solo lectura) ----------
export const getCoreRoles = () => api.get<CoreRole[]>("/api/CoreRoles");

// ---------- Usuarios / Empleados del Core ----------
export const getUsuarios = () => api.get<Usuario[]>("/api/Usuarios");

export interface CrearUsuarioPayload {
  nombreCompleto: string;
  nombreUsuario: string;
  correo: string;
  passwordTemporal: string;
  roleId: number;
  sucursalId: number;
}
export const crearUsuario = (payload: CrearUsuarioPayload) => api.post<Usuario>("/api/Usuarios", payload);

export const cambiarRolUsuario = (id: number, nuevoRolId: number) =>
  api.put<{ mensaje: string }>(`/api/Usuarios/${id}/cambiar-rol?nuevoRolId=${nuevoRolId}`);

export interface EditarUsuarioPayload {
  nombreCompleto: string;
  correo: string;
  sucursalId: number;
}
export const editarUsuario = (id: number, payload: EditarUsuarioPayload) =>
  api.put<{ mensaje: string }>(`/api/Usuarios/${id}`, payload);

export const cambiarEstadoUsuario = (id: number, nuevoEstado: EstadoCuenta) =>
  api.put<{ mensaje: string }>(`/api/Usuarios/${id}/estado?nuevoEstado=${nuevoEstado}`);

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
export const editarBeneficiario = (id: number, payload: Beneficiario) =>
  api.put<void>(`/api/Beneficiarios/${id}`, payload);
export const desactivarBeneficiario = (id: number) => api.delete<{ mensaje: string }>(`/api/Beneficiarios/${id}`);

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
export const desactivarSucursal = (id: number) => api.delete<{ mensaje: string }>(`/api/Sucursales/${id}`);

// ---------- Citas (solicitudes públicas de agendamiento) ----------
export interface CrearCitaPayload {
  nombreSolicitante: string;
  telefono: string;
  correo: string;
  tipoServicio: number;
  sedePreferida: number;
  fechaPreferida: string;
  comentario?: string;
}
// Pública: cualquier visitante la usa sin haber iniciado sesión (ver CitasController.PostCita).
export const crearSolicitudCita = (payload: CrearCitaPayload) => api.post<SolicitudCita>("/api/Citas", payload);
export const consultarSolicitudes = (termino: string) =>
  api.get<SolicitudCita[]>(`/api/Citas/consultar?termino=${encodeURIComponent(termino)}`);

// Estas requieren sesión de Recepción/Admin/especialista (ver CitasController).
export const getCitas = () => api.get<SolicitudCita[]>("/api/Citas");
export const confirmarCita = (id: number) => api.put<{ mensaje: string }>(`/api/Citas/${id}/confirmar`);
export const cancelarCita = (id: number) => api.put<{ mensaje: string }>(`/api/Citas/${id}/cancelar`);
export const eliminarCita = (id: number) => api.delete<void>(`/api/Citas/${id}`);

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
export const editarConsulta = (id: number, payload: ConsultaOftalmologica) => api.put<void>(`/api/Consultas/${id}`, payload);
export const eliminarConsulta = (id: number) => api.delete<void>(`/api/Consultas/${id}`);

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
export const editarInscripcion = (id: number, payload: InscripcionRehabilitacion) =>
  api.put<void>(`/api/Rehabilitacion/${id}`, payload);
export const eliminarInscripcion = (id: number) => api.delete<void>(`/api/Rehabilitacion/${id}`);

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
export const editarVoluntario = (id: number, payload: Voluntario) => api.put<void>(`/api/Voluntarios/${id}`, payload);
export const desactivarVoluntario = (id: number) => api.delete<{ mensaje: string }>(`/api/Voluntarios/${id}`);

export const getDonaciones = () => api.get<Donacion[]>("/api/Voluntarios/donaciones");

// ---------- Inventario ----------
// Usado por Órdenes de Pedido para armar su selector de productos (necesita la lista
// completa, no paginada); el catálogo real no suele superar unos pocos cientos de ítems.
export const getInventario = async () => {
  const resultado = await api.get<PaginaResultado<ProductoArticulo>>("/api/inventario?tamanoPagina=500");
  return resultado.items;
};

export interface FiltrosInventario {
  categoria?: string;
  activo?: boolean;
  stockBajo?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}
export const getInventarioPaginado = (filtros: FiltrosInventario = {}) => {
  const params = new URLSearchParams();
  if (filtros.categoria) params.set("categoria", filtros.categoria);
  if (filtros.activo !== undefined) params.set("activo", String(filtros.activo));
  if (filtros.stockBajo !== undefined) params.set("stockBajo", String(filtros.stockBajo));
  params.set("pagina", String(filtros.pagina ?? 1));
  params.set("tamanoPagina", String(filtros.tamanoPagina ?? 20));
  return api.get<PaginaResultado<ProductoArticulo>>(`/api/inventario?${params}`);
};

export const getAlertasStock = () => api.get<ProductoArticulo[]>("/api/inventario/alertas");

export interface CrearProductoPayload {
  codigo: string;
  nombre: string;
  categoria: string;
  precioVenta: number;
  stockInicial: number;
  stockMinimo: number;
}
export const crearProducto = (payload: CrearProductoPayload) => api.post<ProductoArticulo>("/api/inventario", payload);

export interface EditarProductoPayload {
  nombre: string;
  categoria: string;
  precioVenta: number;
  stockMinimo: number;
  activo: boolean;
}
export const editarProducto = (id: number, payload: EditarProductoPayload) =>
  api.put<{ mensaje: string }>(`/api/inventario/${id}`, payload);

export const registrarEntradaInventario = (id: number, cantidad: number, motivo?: string) =>
  api.put<{ mensaje: string; nuevoStock: number }>(`/api/inventario/${id}/entrada`, { cantidad, motivo });

export const registrarSalidaInventario = (id: number, cantidad: number, motivo?: string) =>
  api.put<{ mensaje: string; nuevoStock: number }>(`/api/inventario/${id}/salida`, { cantidad, motivo });

export const registrarAjusteInventario = (id: number, stockReal: number, motivo?: string) =>
  api.put<{ mensaje: string; nuevoStock: number }>(`/api/inventario/${id}/ajuste`, { stockReal, motivo });

export const getMovimientosProducto = (id: number) => api.get<InventarioMovimiento[]>(`/api/inventario/${id}/movimientos`);

// ---------- Accesos Web (auditoría/gestión de cuentas de la plataforma web pública) ----------
export interface FiltrosWebUsers {
  estado?: EstadoWebUser;
  busqueda?: string;
}
export const getWebUsers = (filtros: FiltrosWebUsers = {}) => {
  const params = new URLSearchParams();
  if (filtros.estado) params.set("estado", filtros.estado);
  if (filtros.busqueda) params.set("busqueda", filtros.busqueda);
  const query = params.toString();
  return api.get<WebUser[]>(`/api/WebUsers${query ? `?${query}` : ""}`);
};

export const habilitarWebUser = (id: number) => api.put<{ mensaje: string }>(`/api/WebUsers/${id}/habilitar`);
export const deshabilitarWebUser = (id: number) => api.put<{ mensaje: string }>(`/api/WebUsers/${id}/deshabilitar`);
export const resetearPasswordWebUser = (id: number) =>
  api.post<{ mensaje: string; passwordTemporal: string }>(`/api/WebUsers/${id}/resetear-password`);
export const forzarCierreSesionWebUser = (id: number) =>
  api.post<{ mensaje: string }>(`/api/WebUsers/${id}/forzar-cierre-sesion`);
export const getHistorialWebUser = (id: number) =>
  api.get<WebUserLoginHistorial[]>(`/api/WebUsers/${id}/historial-accesos`);

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
  api.post<{ mensaje: string; sesion: SesionMasajeTacto }>("/api/recepcion/masajes", payload);

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
  api.post<{ mensaje: string; prestamoId: number }>("/api/recepcion/prestamos", payload);

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
  api.post<{ mensaje: string; orden: OrdenPedidoCaja }>("/api/recepcion/ordenes", payload);

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
