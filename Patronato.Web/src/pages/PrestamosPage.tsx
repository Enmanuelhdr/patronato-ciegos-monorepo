import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearPrestamo, getBeneficiarios, getPrestamosActivos } from "../api/endpoints";
import { ApiError } from "../api/client";
import { EstadoPrestamoTexto, RubroTexto, type PrestamoEmprendimiento } from "../api/types";

export function PrestamosPage() {
  const qc = useQueryClient();
  const prestamos = useQuery({ queryKey: ["prestamos-activos", ""], queryFn: () => getPrestamosActivos() });
  const beneficiarios = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({ beneficiarioId: 0, montoAprobado: 10000, plazoMeses: 12, rubro: 1, involucraFamilia: true });

  const crear = useMutation({
    mutationFn: () => crearPrestamo(form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["prestamos-activos"] });
      setMostrarForm(false);
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo otorgar el préstamo."),
  });

  const columnas: Columna<PrestamoEmprendimiento>[] = [
    { encabezado: "Beneficiario", render: (p) => (p.beneficiario ? `${p.beneficiario.nombres} ${p.beneficiario.apellidos}` : `#${p.beneficiarioId}`) },
    { encabezado: "Rubro", render: (p) => RubroTexto[p.rubro] },
    { encabezado: "Monto otorgado", render: (p) => `RD$ ${p.montoAprobado.toFixed(2)}` },
    { encabezado: "Saldo pendiente", render: (p) => `RD$ ${p.saldoPendiente.toFixed(2)}` },
    { encabezado: "Plazo", render: (p) => `${p.plazoMeses} meses` },
    {
      encabezado: "Estado",
      render: (p) => (
        <span className={`tag ${p.estado === 1 ? "tag-azul" : p.estado === 2 ? "tag-verde" : p.estado === 3 ? "tag-rojo" : "tag-gris"}`}>
          {EstadoPrestamoTexto[p.estado]}
        </span>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Préstamos de Emprendimiento"
        subtitulo="Otorga microcréditos a beneficiarios. Los abonos a cuotas se cobran exclusivamente en Caja."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Otorgar préstamo"}</button>}
      />

      {mostrarForm && (
        <div className="panel">
          <h2>Nuevo préstamo</h2>
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
                <label>Rubro del negocio</label>
                <select value={form.rubro} onChange={(e) => setForm({ ...form, rubro: Number(e.target.value) })}>
                  {Object.entries(RubroTexto).map(([v, t]) => <option key={v} value={v}>{t}</option>)}
                </select>
              </div>
              <div className="campo">
                <label>Monto aprobado (RD$)</label>
                <input required type="number" min={1} value={form.montoAprobado} onChange={(e) => setForm({ ...form, montoAprobado: Number(e.target.value) })} />
              </div>
              <div className="campo">
                <label>Plazo (meses)</label>
                <select value={form.plazoMeses} onChange={(e) => setForm({ ...form, plazoMeses: Number(e.target.value) })}>
                  <option value={6}>6 meses</option>
                  <option value={12}>12 meses</option>
                  <option value={18}>18 meses</option>
                  <option value={24}>24 meses</option>
                </select>
              </div>
              <div className="campo campo-checkbox">
                <input type="checkbox" id="familia" checked={form.involucraFamilia} onChange={(e) => setForm({ ...form, involucraFamilia: e.target.checked })} />
                <label htmlFor="familia">Involucra al núcleo familiar</label>
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending || form.beneficiarioId === 0}>
              {crear.isPending ? "Guardando..." : "Otorgar préstamo"}
            </button>
          </form>
        </div>
      )}

      <DataTable columnas={columnas} filas={prestamos.data ?? []} claveFila={(p) => p.id} cargando={prestamos.isLoading} />
    </>
  );
}
