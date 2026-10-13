import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearInscripcion, editarInscripcion, eliminarInscripcion, getBeneficiarios, getInscripciones, graduarAlumno } from "../api/endpoints";
import { ApiError } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { EtapaTexto, SedeTexto, type InscripcionRehabilitacion } from "../api/types";

export function RehabilitacionPage() {
  const qc = useQueryClient();
  const { sesion } = useAuth();
  const inscripciones = useQuery({ queryKey: ["rehabilitacion"], queryFn: getInscripciones });
  const beneficiarios = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editando, setEditando] = useState<InscripcionRehabilitacion | null>(null);

  const [form, setForm] = useState({ beneficiarioId: 0, sede: 1, etapa: 1, tallerMateria: "" });

  const invalidar = () => qc.invalidateQueries({ queryKey: ["rehabilitacion"] });

  const crear = useMutation({
    mutationFn: () => crearInscripcion(form),
    onSuccess: () => {
      invalidar();
      setMostrarForm(false);
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo inscribir al beneficiario."),
  });

  const graduar = useMutation({
    mutationFn: (id: number) => graduarAlumno(id),
    onSuccess: invalidar,
  });

  const editar = useMutation({
    mutationFn: (r: InscripcionRehabilitacion) => editarInscripcion(r.id, r),
    onSuccess: () => {
      invalidar();
      setEditando(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo editar la inscripción."),
  });

  const eliminar = useMutation({ mutationFn: eliminarInscripcion, onSuccess: invalidar });

  const columnas: Columna<InscripcionRehabilitacion>[] = [
    { encabezado: "Beneficiario", render: (r) => (r.beneficiario ? `${r.beneficiario.nombres} ${r.beneficiario.apellidos}` : `#${r.beneficiarioId}`) },
    { encabezado: "Taller / Materia", render: (r) => r.tallerMateria },
    { encabezado: "Etapa", render: (r) => EtapaTexto[r.etapa] },
    { encabezado: "Sede", render: (r) => SedeTexto[r.sede] },
    { encabezado: "Estado", render: (r) => <span className={`tag ${r.completado ? "tag-verde" : "tag-azul"}`}>{r.completado ? "Graduado" : "En curso"}</span> },
    {
      encabezado: "",
      render: (r) => (
        <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
          {!r.completado && (
            <button className="btn btn-secundario" onClick={() => graduar.mutate(r.id)} disabled={graduar.isPending}>
              Marcar graduado
            </button>
          )}
          <button className="btn btn-secundario" onClick={() => setEditando(r)}>Editar</button>
          {sesion?.rol === "Administrador" && (
            <button
              className="btn btn-peligro"
              disabled={eliminar.isPending}
              onClick={() => {
                if (confirm("¿Eliminar esta inscripción?")) eliminar.mutate(r.id);
              }}
            >
              Eliminar
            </button>
          )}
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Rehabilitación"
        subtitulo="Talleres de independencia (Braille, bastón, oficios) y de inserción laboral."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Inscribir beneficiario"}</button>}
      />

      {mostrarForm && (
        <div className="panel">
          <h2>Nueva inscripción</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form onSubmit={(e) => { e.preventDefault(); crear.mutate(); }}>
            <div className="form-grid">
              <div className="campo">
                <label>Beneficiario</label>
                <select required value={form.beneficiarioId} onChange={(e) => setForm({ ...form, beneficiarioId: Number(e.target.value) })}>
                  <option value={0} disabled>Selecciona un beneficiario...</option>
                  {beneficiarios.data?.map((b) => (
                    <option key={b.id} value={b.id}>{b.nombres} {b.apellidos} — {b.cedula}</option>
                  ))}
                </select>
              </div>
              <div className="campo">
                <label>Taller / Materia</label>
                <input required placeholder="Ej: Orientación y Movilidad (Bastón)" value={form.tallerMateria} onChange={(e) => setForm({ ...form, tallerMateria: e.target.value })} />
              </div>
              <div className="campo">
                <label>Etapa</label>
                <select value={form.etapa} onChange={(e) => setForm({ ...form, etapa: Number(e.target.value) })}>
                  {Object.entries(EtapaTexto).map(([v, t]) => <option key={v} value={v}>{t}</option>)}
                </select>
              </div>
              <div className="campo">
                <label>Sede</label>
                <select value={form.sede} onChange={(e) => setForm({ ...form, sede: Number(e.target.value) })}>
                  {Object.entries(SedeTexto).map(([v, t]) => <option key={v} value={v}>{t}</option>)}
                </select>
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending || form.beneficiarioId === 0}>
              {crear.isPending ? "Guardando..." : "Inscribir"}
            </button>
          </form>
        </div>
      )}

      {editando && (
        <div className="panel">
          <h2>Editar inscripción</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              editar.mutate(editando);
            }}
          >
            <div className="form-grid">
              <div className="campo">
                <label>Taller / Materia</label>
                <input required value={editando.tallerMateria} onChange={(e) => setEditando({ ...editando, tallerMateria: e.target.value })} />
              </div>
              <div className="campo">
                <label>Etapa</label>
                <select value={editando.etapa} onChange={(e) => setEditando({ ...editando, etapa: Number(e.target.value) as InscripcionRehabilitacion["etapa"] })}>
                  {Object.entries(EtapaTexto).map(([v, t]) => <option key={v} value={v}>{t}</option>)}
                </select>
              </div>
              <div className="campo">
                <label>Sede</label>
                <select value={editando.sede} onChange={(e) => setEditando({ ...editando, sede: Number(e.target.value) as InscripcionRehabilitacion["sede"] })}>
                  {Object.entries(SedeTexto).map(([v, t]) => <option key={v} value={v}>{t}</option>)}
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

      <DataTable columnas={columnas} filas={inscripciones.data ?? []} claveFila={(r) => r.id} cargando={inscripciones.isLoading} />
    </>
  );
}
