import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearBeneficiario, desactivarBeneficiario, editarBeneficiario, getBeneficiarios } from "../api/endpoints";
import { ApiError } from "../api/client";
import { SedeTexto, TipoDiscapacidadTexto, type Beneficiario } from "../api/types";

export function BeneficiariosPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editando, setEditando] = useState<Beneficiario | null>(null);

  const [form, setForm] = useState({ nombres: "", apellidos: "", cedula: "", telefono: "", sedeAsignada: 1, condicionVisual: 1 });

  const invalidar = () => qc.invalidateQueries({ queryKey: ["beneficiarios"] });

  const crear = useMutation({
    mutationFn: () => crearBeneficiario({ ...form, activo: true }),
    onSuccess: () => {
      invalidar();
      setMostrarForm(false);
      setForm({ nombres: "", apellidos: "", cedula: "", telefono: "", sedeAsignada: 1, condicionVisual: 1 });
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo registrar el beneficiario."),
  });

  const editar = useMutation({
    mutationFn: (b: Beneficiario) => editarBeneficiario(b.id, b),
    onSuccess: () => {
      invalidar();
      setEditando(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo editar el beneficiario."),
  });

  const desactivar = useMutation({ mutationFn: desactivarBeneficiario, onSuccess: invalidar });

  const columnas: Columna<Beneficiario>[] = [
    { encabezado: "Nombre completo", render: (b) => `${b.nombres} ${b.apellidos}` },
    { encabezado: "Cédula", render: (b) => b.cedula },
    { encabezado: "Teléfono", render: (b) => b.telefono },
    { encabezado: "Sede", render: (b) => SedeTexto[b.sedeAsignada] },
    { encabezado: "Condición visual", render: (b) => TipoDiscapacidadTexto[b.condicionVisual] },
    { encabezado: "Estado", render: (b) => <span className={`tag ${b.activo ? "tag-verde" : "tag-gris"}`}>{b.activo ? "Activo" : "Inactivo"}</span> },
    {
      encabezado: "Acciones",
      render: (b) => (
        <div style={{ display: "flex", gap: 6 }}>
          <button className="btn btn-secundario" onClick={() => setEditando(b)}>Editar</button>
          {b.activo && (
            <button
              className="btn btn-peligro"
              disabled={desactivar.isPending}
              onClick={() => {
                if (confirm(`¿Desactivar a "${b.nombres} ${b.apellidos}"?`)) desactivar.mutate(b.id);
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

      {editando && (
        <div className="panel">
          <h2>Editar beneficiario</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              editar.mutate(editando);
            }}
          >
            <div className="form-grid">
              <div className="campo">
                <label>Nombres</label>
                <input required value={editando.nombres} onChange={(e) => setEditando({ ...editando, nombres: e.target.value })} />
              </div>
              <div className="campo">
                <label>Apellidos</label>
                <input required value={editando.apellidos} onChange={(e) => setEditando({ ...editando, apellidos: e.target.value })} />
              </div>
              <div className="campo">
                <label>Cédula</label>
                <input required value={editando.cedula} onChange={(e) => setEditando({ ...editando, cedula: e.target.value })} />
              </div>
              <div className="campo">
                <label>Teléfono</label>
                <input required value={editando.telefono} onChange={(e) => setEditando({ ...editando, telefono: e.target.value })} />
              </div>
              <div className="campo">
                <label>Sede asignada</label>
                <select value={editando.sedeAsignada} onChange={(e) => setEditando({ ...editando, sedeAsignada: Number(e.target.value) as Beneficiario["sedeAsignada"] })}>
                  {Object.entries(SedeTexto).map(([v, t]) => (
                    <option key={v} value={v}>{t}</option>
                  ))}
                </select>
              </div>
              <div className="campo">
                <label>Condición visual</label>
                <select
                  value={editando.condicionVisual}
                  onChange={(e) => setEditando({ ...editando, condicionVisual: Number(e.target.value) as Beneficiario["condicionVisual"] })}
                >
                  {Object.entries(TipoDiscapacidadTexto).map(([v, t]) => (
                    <option key={v} value={v}>{t}</option>
                  ))}
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

      <DataTable columnas={columnas} filas={data ?? []} claveFila={(b) => b.id} cargando={isLoading} />
    </>
  );
}
