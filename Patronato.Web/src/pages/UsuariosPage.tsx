import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import {
  cambiarEstadoUsuario,
  cambiarRolUsuario,
  crearUsuario,
  editarUsuario,
  getCoreRoles,
  getSucursales,
  getUsuarios,
} from "../api/endpoints";
import { ApiError } from "../api/client";
import { useAuth } from "../context/AuthContext";
import type { EstadoCuenta, Usuario } from "../api/types";

const ESTADOS: EstadoCuenta[] = ["Activo", "Suspendido", "Revocado"];
const TAG_POR_ESTADO: Record<EstadoCuenta, string> = {
  Activo: "tag-verde",
  Suspendido: "tag-ambar",
  Revocado: "tag-gris",
};

export function UsuariosPage() {
  const qc = useQueryClient();
  const { tienePermiso } = useAuth();
  const puedeCrear = tienePermiso("core-users.crear");
  const puedeEditar = tienePermiso("core-users.editar");
  const puedeCambiarEstado = tienePermiso("core-users.estado");
  const usuarios = useQuery({ queryKey: ["usuarios"], queryFn: getUsuarios });
  const sucursales = useQuery({ queryKey: ["sucursales"], queryFn: getSucursales });
  const roles = useQuery({ queryKey: ["core-roles"], queryFn: getCoreRoles });

  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editando, setEditando] = useState<Usuario | null>(null);

  const [form, setForm] = useState({ nombreCompleto: "", nombreUsuario: "", correo: "", passwordTemporal: "", roleId: 0, sucursalId: 1 });

  const invalidar = () => qc.invalidateQueries({ queryKey: ["usuarios"] });

  const crear = useMutation({
    mutationFn: () => crearUsuario(form),
    onSuccess: () => {
      invalidar();
      setMostrarForm(false);
      setForm({ nombreCompleto: "", nombreUsuario: "", correo: "", passwordTemporal: "", roleId: 0, sucursalId: 1 });
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo registrar el empleado."),
  });

  const cambiarRol = useMutation({
    mutationFn: (p: { id: number; roleId: number }) => cambiarRolUsuario(p.id, p.roleId),
    onSuccess: invalidar,
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo cambiar el rol."),
  });

  const cambiarEstado = useMutation({
    mutationFn: (p: { id: number; estado: EstadoCuenta }) => cambiarEstadoUsuario(p.id, p.estado),
    onSuccess: invalidar,
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo cambiar el estado."),
  });

  const editar = useMutation({
    mutationFn: (u: Usuario) => editarUsuario(u.id, { nombreCompleto: u.nombreCompleto, correo: u.correo, sucursalId: u.sucursalId }),
    onSuccess: () => {
      invalidar();
      setEditando(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo editar el empleado."),
  });

  const columnas: Columna<Usuario>[] = [
    { encabezado: "Nombre completo", render: (u) => u.nombreCompleto },
    { encabezado: "Usuario", render: (u) => u.nombreUsuario },
    { encabezado: "Correo", render: (u) => u.correo },
    { encabezado: "Sede", render: (u) => sucursales.data?.find((s) => s.id === u.sucursalId)?.nombre ?? `#${u.sucursalId}` },
    {
      encabezado: "Estado",
      render: (u) =>
        puedeCambiarEstado ? (
          <select
            value={u.estadoCuenta}
            onChange={(e) => cambiarEstado.mutate({ id: u.id, estado: e.target.value as EstadoCuenta })}
            disabled={cambiarEstado.isPending}
          >
            {ESTADOS.map((estado) => (
              <option key={estado} value={estado}>{estado}</option>
            ))}
          </select>
        ) : (
          <span className={`tag ${TAG_POR_ESTADO[u.estadoCuenta]}`}>{u.estadoCuenta}</span>
        ),
    },
    {
      encabezado: "Rol (tipo de acceso)",
      render: (u) =>
        puedeEditar ? (
          <select
            value={u.roleId}
            onChange={(e) => cambiarRol.mutate({ id: u.id, roleId: Number(e.target.value) })}
            disabled={cambiarRol.isPending}
          >
            {roles.data?.map((r) => (
              <option key={r.id} value={r.id}>{r.nombre}</option>
            ))}
          </select>
        ) : (
          u.roleNombre
        ),
    },
    {
      encabezado: "Acciones",
      render: (u) =>
        puedeEditar ? <button className="btn btn-secundario" onClick={() => setEditando(u)}>Editar</button> : null,
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Personal y Usuarios Institucionales"
        subtitulo="Gestión de colaboradores, asignación de roles y control de acceso del Patronato."
        accion={puedeCrear ? <button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Registrar empleado"}</button> : undefined}
      />

      {mostrarForm && puedeCrear && (
        <div className="panel">
          <h2>Nuevo empleado</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form onSubmit={(e) => { e.preventDefault(); crear.mutate(); }}>
            <div className="form-grid">
              <div className="campo">
                <label>Nombre completo</label>
                <input required value={form.nombreCompleto} onChange={(e) => setForm({ ...form, nombreCompleto: e.target.value })} />
              </div>
              <div className="campo">
                <label>Nombre de usuario (login)</label>
                <input required value={form.nombreUsuario} onChange={(e) => setForm({ ...form, nombreUsuario: e.target.value })} />
              </div>
              <div className="campo">
                <label>Correo</label>
                <input type="email" value={form.correo} onChange={(e) => setForm({ ...form, correo: e.target.value })} />
              </div>
              <div className="campo">
                <label>Contraseña temporal</label>
                <input required value={form.passwordTemporal} onChange={(e) => setForm({ ...form, passwordTemporal: e.target.value })} />
                <small>El empleado deberá cambiarla en su primer inicio de sesión.</small>
              </div>
              <div className="campo">
                <label>Rol</label>
                <select value={form.roleId} onChange={(e) => setForm({ ...form, roleId: Number(e.target.value) })}>
                  <option value={0} disabled>Selecciona un rol...</option>
                  {roles.data?.map((r) => <option key={r.id} value={r.id}>{r.nombre}</option>)}
                </select>
              </div>
              <div className="campo">
                <label>Sucursal</label>
                <select value={form.sucursalId} onChange={(e) => setForm({ ...form, sucursalId: Number(e.target.value) })}>
                  {sucursales.data?.map((s) => <option key={s.id} value={s.id}>{s.nombre}</option>)}
                </select>
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending || form.roleId === 0}>
              {crear.isPending ? "Guardando..." : "Registrar empleado"}
            </button>
          </form>
        </div>
      )}

      {editando && (
        <div className="panel">
          <h2>Editar empleado</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              editar.mutate(editando);
            }}
          >
            <div className="form-grid">
              <div className="campo">
                <label>Nombre completo</label>
                <input required value={editando.nombreCompleto} onChange={(e) => setEditando({ ...editando, nombreCompleto: e.target.value })} />
              </div>
              <div className="campo">
                <label>Correo</label>
                <input type="email" value={editando.correo} onChange={(e) => setEditando({ ...editando, correo: e.target.value })} />
              </div>
              <div className="campo">
                <label>Sucursal</label>
                <select value={editando.sucursalId} onChange={(e) => setEditando({ ...editando, sucursalId: Number(e.target.value) })}>
                  {sucursales.data?.map((s) => <option key={s.id} value={s.id}>{s.nombre}</option>)}
                </select>
              </div>
            </div>
            <div style={{ display: "flex", gap: 10, marginTop: 14 }}>
              <button className="btn btn-exito" type="submit" disabled={editar.isPending}>Guardar cambios</button>
              <button className="btn btn-secundario" type="button" onClick={() => setEditando(null)}>Cancelar</button>
            </div>
          </form>
        </div>
      )}

      <DataTable columnas={columnas} filas={usuarios.data ?? []} claveFila={(u) => u.id} cargando={usuarios.isLoading} />
    </>
  );
}
