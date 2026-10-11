import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearSucursal, getSucursales } from "../api/endpoints";
import { ApiError } from "../api/client";
import type { Sucursal } from "../api/types";

export function SucursalesPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["sucursales"], queryFn: getSucursales });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ nombre: "", direccion: "", telefono: "", encargado: "" });

  const crear = useMutation({
    mutationFn: () => crearSucursal({ ...form, activa: true }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sucursales"] });
      setMostrarForm(false);
      setForm({ nombre: "", direccion: "", telefono: "", encargado: "" });
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo registrar la sucursal."),
  });

  const columnas: Columna<Sucursal>[] = [
    { encabezado: "Nombre", render: (s) => s.nombre },
    { encabezado: "Dirección", render: (s) => s.direccion },
    { encabezado: "Teléfono", render: (s) => s.telefono },
    { encabezado: "Encargado", render: (s) => s.encargado },
    { encabezado: "Estado", render: (s) => <span className={`tag ${s.activa ? "tag-verde" : "tag-gris"}`}>{s.activa ? "Activa" : "Inactiva"}</span> },
  ];

  return (
    <>
      <PageHeader
        titulo="Sucursales"
        subtitulo="Sedes del Patronato a nivel nacional."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Nueva sucursal"}</button>}
      />

      {mostrarForm && (
        <div className="panel">
          <h2>Nueva sucursal</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form onSubmit={(e) => { e.preventDefault(); crear.mutate(); }}>
            <div className="form-grid">
              <div className="campo">
                <label>Nombre</label>
                <input required value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
              </div>
              <div className="campo">
                <label>Dirección</label>
                <input required value={form.direccion} onChange={(e) => setForm({ ...form, direccion: e.target.value })} />
              </div>
              <div className="campo">
                <label>Teléfono</label>
                <input required value={form.telefono} onChange={(e) => setForm({ ...form, telefono: e.target.value })} />
              </div>
              <div className="campo">
                <label>Encargado</label>
                <input required value={form.encargado} onChange={(e) => setForm({ ...form, encargado: e.target.value })} />
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending}>
              {crear.isPending ? "Guardando..." : "Registrar sucursal"}
            </button>
          </form>
        </div>
      )}

      <DataTable columnas={columnas} filas={data ?? []} claveFila={(s) => s.id} cargando={isLoading} />
    </>
  );
}
