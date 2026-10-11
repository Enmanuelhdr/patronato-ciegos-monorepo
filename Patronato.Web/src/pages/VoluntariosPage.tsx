import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearVoluntario, getVoluntarios } from "../api/endpoints";
import { ApiError } from "../api/client";
import type { Voluntario } from "../api/types";

export function VoluntariosPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["voluntarios"], queryFn: getVoluntarios });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ nombreCompleto: "", cedula: "", telefono: "", correo: "", areaApoyo: "" });

  const crear = useMutation({
    mutationFn: () => crearVoluntario({ ...form, activo: true }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["voluntarios"] });
      setMostrarForm(false);
      setForm({ nombreCompleto: "", cedula: "", telefono: "", correo: "", areaApoyo: "" });
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo registrar al voluntario."),
  });

  const columnas: Columna<Voluntario>[] = [
    { encabezado: "Nombre completo", render: (v) => v.nombreCompleto },
    { encabezado: "Cédula", render: (v) => v.cedula },
    { encabezado: "Teléfono", render: (v) => v.telefono },
    { encabezado: "Área de apoyo", render: (v) => v.areaApoyo },
    { encabezado: "Estado", render: (v) => <span className={`tag ${v.activo ? "tag-verde" : "tag-gris"}`}>{v.activo ? "Activo" : "Inactivo"}</span> },
  ];

  return (
    <>
      <PageHeader
        titulo="Voluntarios"
        subtitulo="Personas solidarias que apoyan en lectura, docencia Braille, logística de eventos, entre otros."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Registrar voluntario"}</button>}
      />

      {mostrarForm && (
        <div className="panel">
          <h2>Nuevo voluntario</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form onSubmit={(e) => { e.preventDefault(); crear.mutate(); }}>
            <div className="form-grid">
              <div className="campo">
                <label>Nombre completo</label>
                <input required value={form.nombreCompleto} onChange={(e) => setForm({ ...form, nombreCompleto: e.target.value })} />
              </div>
              <div className="campo">
                <label>Cédula</label>
                <input required value={form.cedula} onChange={(e) => setForm({ ...form, cedula: e.target.value })} />
              </div>
              <div className="campo">
                <label>Teléfono</label>
                <input required value={form.telefono} onChange={(e) => setForm({ ...form, telefono: e.target.value })} />
              </div>
              <div className="campo">
                <label>Correo</label>
                <input type="email" value={form.correo} onChange={(e) => setForm({ ...form, correo: e.target.value })} />
              </div>
              <div className="campo">
                <label>Área de apoyo</label>
                <input required placeholder="Ej: Lectura y Acompañamiento" value={form.areaApoyo} onChange={(e) => setForm({ ...form, areaApoyo: e.target.value })} />
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending}>
              {crear.isPending ? "Guardando..." : "Registrar voluntario"}
            </button>
          </form>
        </div>
      )}

      <DataTable columnas={columnas} filas={data ?? []} claveFila={(v) => v.id} cargando={isLoading} />
    </>
  );
}
