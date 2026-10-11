import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearConsulta, getBeneficiarios, getConsultas, registrarDiagnostico } from "../api/endpoints";
import { ApiError } from "../api/client";
import { SedeTexto, type ConsultaOftalmologica } from "../api/types";

export function ConsultasPage() {
  const qc = useQueryClient();
  const consultas = useQuery({ queryKey: ["consultas"], queryFn: getConsultas });
  const beneficiarios = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [diagnosticoEdit, setDiagnosticoEdit] = useState<{ id: number; diagnostico: string; tratamiento: string } | null>(null);

  const [form, setForm] = useState({ beneficiarioId: 0, sede: 1, oftalmologo: "", tarifaConsulta: 300, cubiertoPorARS: false });

  const crear = useMutation({
    mutationFn: () =>
      crearConsulta({ ...form, diagnosticoOcular: "Pendiente de evaluación", tratamientoPrescrito: "" }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["consultas"] });
      setMostrarForm(false);
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo agendar la consulta."),
  });

  const diagnosticar = useMutation({
    mutationFn: (p: { id: number; diagnostico: string; tratamiento: string }) =>
      registrarDiagnostico(p.id, p.diagnostico, p.tratamiento),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["consultas"] });
      setDiagnosticoEdit(null);
    },
  });

  const columnas: Columna<ConsultaOftalmologica>[] = [
    { encabezado: "Paciente", render: (c) => (c.beneficiario ? `${c.beneficiario.nombres} ${c.beneficiario.apellidos}` : `#${c.beneficiarioId}`) },
    { encabezado: "Oftalmólogo", render: (c) => c.oftalmologo },
    { encabezado: "Sede", render: (c) => SedeTexto[c.sede] },
    { encabezado: "Diagnóstico", render: (c) => c.diagnosticoOcular },
    { encabezado: "Cubierto ARS", render: (c) => <span className={`tag ${c.cubiertoPorARS ? "tag-verde" : "tag-gris"}`}>{c.cubiertoPorARS ? "Sí" : "No"}</span> },
    { encabezado: "Facturada en Caja", render: (c) => <span className={`tag ${c.estaFacturadaEnCaja ? "tag-verde" : "tag-ambar"}`}>{c.estaFacturadaEnCaja ? "Sí" : "Pendiente"}</span> },
    {
      encabezado: "",
      render: (c) => (
        <button
          className="btn btn-secundario"
          onClick={() => setDiagnosticoEdit({ id: c.id, diagnostico: c.diagnosticoOcular, tratamiento: c.tratamientoPrescrito })}
        >
          Asentar diagnóstico
        </button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Consultas Oftalmológicas"
        subtitulo="Agenda consultas de prevención y registra el diagnóstico del especialista."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Agendar consulta"}</button>}
      />

      {mostrarForm && (
        <div className="panel">
          <h2>Agendar consulta</h2>
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
                <label>Sede</label>
                <select value={form.sede} onChange={(e) => setForm({ ...form, sede: Number(e.target.value) })}>
                  {Object.entries(SedeTexto).map(([v, t]) => <option key={v} value={v}>{t}</option>)}
                </select>
              </div>
              <div className="campo">
                <label>Oftalmólogo</label>
                <input required value={form.oftalmologo} onChange={(e) => setForm({ ...form, oftalmologo: e.target.value })} />
              </div>
              <div className="campo">
                <label>Tarifa (RD$)</label>
                <input required type="number" min={0} value={form.tarifaConsulta} onChange={(e) => setForm({ ...form, tarifaConsulta: Number(e.target.value) })} />
              </div>
              <div className="campo campo-checkbox">
                <input type="checkbox" id="ars" checked={form.cubiertoPorARS} onChange={(e) => setForm({ ...form, cubiertoPorARS: e.target.checked })} />
                <label htmlFor="ars">Cubierto por ARS/SENASA</label>
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending || form.beneficiarioId === 0}>
              {crear.isPending ? "Guardando..." : "Agendar consulta"}
            </button>
          </form>
        </div>
      )}

      {diagnosticoEdit && (
        <div className="panel">
          <h2>Asentar diagnóstico — consulta #{diagnosticoEdit.id}</h2>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              diagnosticar.mutate(diagnosticoEdit);
            }}
          >
            <div className="form-grid">
              <div className="campo">
                <label>Diagnóstico ocular</label>
                <textarea
                  required
                  value={diagnosticoEdit.diagnostico}
                  onChange={(e) => setDiagnosticoEdit({ ...diagnosticoEdit, diagnostico: e.target.value })}
                />
              </div>
              <div className="campo">
                <label>Tratamiento prescrito</label>
                <textarea
                  value={diagnosticoEdit.tratamiento}
                  onChange={(e) => setDiagnosticoEdit({ ...diagnosticoEdit, tratamiento: e.target.value })}
                />
              </div>
            </div>
            <div style={{ display: "flex", gap: 10, marginTop: 14 }}>
              <button className="btn btn-exito" type="submit" disabled={diagnosticar.isPending}>Guardar diagnóstico</button>
              <button className="btn btn-secundario" type="button" onClick={() => setDiagnosticoEdit(null)}>Cancelar</button>
            </div>
          </form>
        </div>
      )}

      <DataTable columnas={columnas} filas={consultas.data ?? []} claveFila={(c) => c.id} cargando={consultas.isLoading} />
    </>
  );
}
