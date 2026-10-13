import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { cambiarRolUsuario, crearUsuario, desactivarUsuario, editarUsuario, getSucursales, getUsuarios } from "../api/endpoints";
import { ApiError } from "../api/client";
import { RolUsuarioTexto, type Usuario } from "../api/types";

export function UsuariosPage() {
  const qc = useQueryClient();
  const usuarios = useQuery({ queryKey: ["usuarios"], queryFn: getUsuarios });
  const sucursales = useQuery({ queryKey: ["sucursales"], queryFn: getSucursales });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editando, setEditando] = useState<Usuario | null>(null);

  const [form, setForm] = useState({ nombreCompleto: "", nombreUsuario: "", correo: "", passwordHash: "", rol: 2, sucursalId: 1 });

  const invalidar = () => qc.invalidateQueries({ queryKey: ["usuarios"] });

  const crear = useMutation({
    mutationFn: () => crearUsuario({ ...form, activo: true }),
    onSuccess: () => {
      invalidar();
      setMostrarForm(false);
      setForm({ nombreCompleto: "", nombreUsuario: "", correo: "", passwordHash: "", rol: 2, sucursalId: 1 });
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo registrar el empleado."),
  });

  const cambiarRol = useMutation({
    mutationFn: (p: { id: number; rol: number }) => cambiarRolUsuario(p.id, p.rol),
    onSuccess: invalidar,
  });

  const editar = useMutation({
    mutationFn: (u: Usuario) => editarUsuario(u.id, u),
    onSuccess: () => {
      invalidar();
      setEditando(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo editar el empleado."),
  });

  const desactivar = useMutation({ mutationFn: desactivarUsuario, onSuccess: invalidar });

  const columnas: Columna<Usuario>[] = [
    { encabezado: "Nombre completo", render: (u) => u.nombreCompleto },
    { encabezado: "Usuario", render: (u) => u.nombreUsuario },
    { encabezado: "Correo", render: (u) => u.correo },
    { encabezado: "Sede", render: (u) => sucursales.data?.find((s) => s.id === u.sucursalId)?.nombre ?? `#${u.sucursalId}` },
    { encabezado: "Estado", render: (u) => <span className={`tag ${u.activo ? "tag-verde" : "tag-gris"}`}>{u.activo ? "Activo" : "Inactivo"}</span> },
    {
      encabezado: "Rol (tipo de acceso)",
      render: (u) => (
        <select value={u.rol} onChange={(e) => cambiarRol.mutate({ id: u.id, rol: Number(e.target.value) })} disabled={cambiarRol.isPending}>
          {Object.entries(RolUsuarioTexto).map(([v, t]) => (
            <option key={v} value={v}>{t}</option>
          ))}
        </select>
      ),
    },
    {
      encabezado: "Acciones",
      render: (u) => (
        <div style={{ display: "flex", gap: 6 }}>
          <button className="btn btn-secundario" onClick={() => setEditando(u)}>Editar</button>
          {u.activo && (
            <button
              className="btn btn-peligro"
              disabled={desactivar.isPending}
              onClick={() => {
                if (confirm(`¿Desactivar la cuenta de "${u.nombreCompleto}"?`)) desactivar.mutate(u.id);
              }}
            >
              Desactivar
            </button>
          )}
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Empleados y Usuarios"
        subtitulo="Altas de personal y asignación de roles. Esta sección es exclusiva de Administración."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Registrar empleado"}</button>}
      />

      {mostrarForm && (
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
                <label>Contraseña inicial</label>
                <input required value={form.passwordHash} onChange={(e) => setForm({ ...form, passwordHash: e.target.value })} />
              </div>
              <div className="campo">
                <label>Rol</label>
                <select value={form.rol} onChange={(e) => setForm({ ...form, rol: Number(e.target.value) })}>
                  {Object.entries(RolUsuarioTexto).map(([v, t]) => <option key={v} value={v}>{t}</option>)}
                </select>
              </div>
              <div className="campo">
                <label>Sucursal</label>
                <select value={form.sucursalId} onChange={(e) => setForm({ ...form, sucursalId: Number(e.target.value) })}>
                  {sucursales.data?.map((s) => <option key={s.id} value={s.id}>{s.nombre}</option>)}
                </select>
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending}>
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
