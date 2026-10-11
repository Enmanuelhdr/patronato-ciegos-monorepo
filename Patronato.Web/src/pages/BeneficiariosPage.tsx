import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearBeneficiario, getBeneficiarios } from "../api/endpoints";
import { ApiError } from "../api/client";
import { SedeTexto, TipoDiscapacidadTexto, type Beneficiario } from "../api/types";

export function BeneficiariosPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({ nombres: "", apellidos: "", cedula: "", telefono: "", sedeAsignada: 1, condicionVisual: 1 });

  const crear = useMutation({
    mutationFn: () => crearBeneficiario({ ...form, activo: true }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["beneficiarios"] });
      setMostrarForm(false);
      setForm({ nombres: "", apellidos: "", cedula: "", telefono: "", sedeAsignada: 1, condicionVisual: 1 });
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo registrar el beneficiario."),
  });

  const columnas: Columna<Beneficiario>[] = [
    { encabezado: "Nombre completo", render: (b) => `${b.nombres} ${b.apellidos}` },
    { encabezado: "Cédula", render: (b) => b.cedula },
    { encabezado: "Teléfono", render: (b) => b.telefono },
    { encabezado: "Sede", render: (b) => SedeTexto[b.sedeAsignada] },
    { encabezado: "Condición visual", render: (b) => TipoDiscapacidadTexto[b.condicionVisual] },
    { encabezado: "Estado", render: (b) => <span className={`tag ${b.activo ? "tag-verde" : "tag-gris"}`}>{b.activo ? "Activo" : "Inactivo"}</span> },
  ];

  return (
    <>
      <PageHeader
        titulo="Beneficiarios"
        subtitulo="Personas invidentes o de baja visión registradas en el Patronato."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Registrar beneficiario"}</button>}
      />

      {mostrarForm && (
        <div className="panel">
          <h2>Nuevo beneficiario</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              crear.mutate();
            }}
          >
            <div className="form-grid">
              <div className="campo">
                <label>Nombres</label>
                <input required value={form.nombres} onChange={(e) => setForm({ ...form, nombres: e.target.value })} />
              </div>
              <div className="campo">
                <label>Apellidos</label>
                <input required value={form.apellidos} onChange={(e) => setForm({ ...form, apellidos: e.target.value })} />
              </div>
              <div className="campo">
                <label>Cédula</label>
                <input required placeholder="001-0000000-0" value={form.cedula} onChange={(e) => setForm({ ...form, cedula: e.target.value })} />
              </div>
              <div className="campo">
                <label>Teléfono</label>
                <input required value={form.telefono} onChange={(e) => setForm({ ...form, telefono: e.target.value })} />
              </div>
              <div className="campo">
                <label>Sede asignada</label>
                <select value={form.sedeAsignada} onChange={(e) => setForm({ ...form, sedeAsignada: Number(e.target.value) })}>
                  {Object.entries(SedeTexto).map(([v, t]) => (
                    <option key={v} value={v}>{t}</option>
                  ))}
                </select>
              </div>
              <div className="campo">
                <label>Condición visual</label>
                <select value={form.condicionVisual} onChange={(e) => setForm({ ...form, condicionVisual: Number(e.target.value) })}>
                  {Object.entries(TipoDiscapacidadTexto).map(([v, t]) => (
                    <option key={v} value={v}>{t}</option>
                  ))}
                </select>
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending}>
              {crear.isPending ? "Guardando..." : "Registrar beneficiario"}
            </button>
          </form>
        </div>
      )}

      <DataTable columnas={columnas} filas={data ?? []} claveFila={(b) => b.id} cargando={isLoading} />
    </>
  );
}
