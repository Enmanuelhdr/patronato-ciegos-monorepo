import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { cambiarRolUsuario, crearUsuario, getSucursales, getUsuarios } from "../api/endpoints";
import { ApiError } from "../api/client";
import { RolUsuarioTexto, type Usuario } from "../api/types";

export function UsuariosPage() {
  const qc = useQueryClient();
  const usuarios = useQuery({ queryKey: ["usuarios"], queryFn: getUsuarios });
  const sucursales = useQuery({ queryKey: ["sucursales"], queryFn: getSucursales });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({ nombreCompleto: "", nombreUsuario: "", correo: "", passwordHash: "", rol: 2, sucursalId: 1 });

  const crear = useMutation({
    mutationFn: () => crearUsuario({ ...form, activo: true }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["usuarios"] });
      setMostrarForm(false);
      setForm({ nombreCompleto: "", nombreUsuario: "", correo: "", passwordHash: "", rol: 2, sucursalId: 1 });
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo registrar el empleado."),
  });

  const cambiarRol = useMutation({
    mutationFn: (p: { id: number; rol: number }) => cambiarRolUsuario(p.id, p.rol),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["usuarios"] }),
  });

  const columnas: Columna<Usuario>[] = [
    { encabezado: "Nombre completo", render: (u) => u.nombreCompleto },
    { encabezado: "Usuario", render: (u) => u.nombreUsuario },
    { encabezado: "Correo", render: (u) => u.correo },
    { encabezado: "Sede", render: (u) => sucursales.data?.find((s) => s.id === u.sucursalId)?.nombre ?? `#${u.sucursalId}` },
    { encabezado: "Estado", render: (u) => <span className={`tag ${u.activo ? "tag-verde" : "tag-gris"}`}>{u.activo ? "Activo" : "Inactivo"}</span> },
    {
      encabezado: "Rol",
      render: (u) => (
        <select value={u.rol} onChange={(e) => cambiarRol.mutate({ id: u.id, rol: Number(e.target.value) })} disabled={cambiarRol.isPending}>
          {Object.entries(RolUsuarioTexto).map(([v, t]) => (
            <option key={v} value={v}>{t}</option>
          ))}
        </select>
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

      <DataTable columnas={columnas} filas={usuarios.data ?? []} claveFila={(u) => u.id} cargando={usuarios.isLoading} />
    </>
  );
}
