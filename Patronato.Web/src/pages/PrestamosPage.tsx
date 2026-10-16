import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearPrestamo, getBeneficiarios, getPrestamosActivos } from "../api/endpoints";
import { ApiError } from "../api/client";
import { EstadoPrestamoTexto, RubroTexto, type PrestamoEmprendimiento } from "../api/types";
import { useAuth } from "../context/AuthContext";

export function PrestamosPage() {
  const { sesion } = useAuth();
  const esAdmin = sesion?.rol === "Administrador";
  const qc = useQueryClient();

  const prestamos = useQuery({ queryKey: ["prestamos-activos", ""], queryFn: () => getPrestamosActivos() });
  const beneficiarios = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({
    beneficiarioId: 0,
    montoAprobado: 10000,
    plazoMeses: 12,
    rubro: 1,
    involucraFamilia: true,
  });

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
    {
      encabezado: "Beneficiario",
      render: (p) => (p.beneficiario ? `${p.beneficiario.nombres} ${p.beneficiario.apellidos}` : `#${p.beneficiarioId}`),
      valorTexto: (p) => (p.beneficiario ? `${p.beneficiario.nombres} ${p.beneficiario.apellidos} ${p.beneficiario.cedula}` : `#${p.beneficiarioId}`),
    },
    {
      encabezado: "Rubro",
      render: (p) => RubroTexto[p.rubro] ?? `Rubro ${p.rubro}`,
      valorTexto: (p) => RubroTexto[p.rubro] ?? `Rubro ${p.rubro}`,
    },
    {
      encabezado: "Monto otorgado",
      render: (p) => `RD$ ${p.montoAprobado.toFixed(2)}`,
      valorTexto: (p) => p.montoAprobado.toFixed(2),
    },
    {
      encabezado: "Saldo pendiente",
      render: (p) => `RD$ ${p.saldoPendiente.toFixed(2)}`,
      valorTexto: (p) => p.saldoPendiente.toFixed(2),
    },
    {
      encabezado: "Plazo",
      render: (p) => `${p.plazoMeses} meses`,
      valorTexto: (p) => `${p.plazoMeses} meses`,
    },
    {
      encabezado: "Estado",
      render: (p) => (
        <span
          className={`tag ${
            p.estado === 1 ? "tag-azul" : p.estado === 2 ? "tag-verde" : p.estado === 3 ? "tag-rojo" : "tag-gris"
          }`}
        >
          {EstadoPrestamoTexto[p.estado] ?? "Desconocido"}
        </span>
      ),
      valorTexto: (p) => EstadoPrestamoTexto[p.estado] ?? "",
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Préstamos de Emprendimiento"
        subtitulo="Microcréditos institucionales para beneficiarios. Las cuotas de amortización se cobran exclusivamente en Caja."
        accion={
          esAdmin ? (
            <button
              type="button"
              className="btn btn-primario"
              onClick={() => setMostrarForm((v) => !v)}
              aria-expanded={mostrarForm}
            >
              <span aria-hidden="true">{mostrarForm ? "✖ " : "➕ "}</span>
              <span>{mostrarForm ? "Cancelar" : "Otorgar préstamo (Admin)"}</span>
            </button>
          ) : undefined
        }
      />

      {!esAdmin && (
        <div className="alerta alerta-info" role="status" aria-live="polite">
          <strong aria-hidden="true">ℹ️ Política Institucional: </strong>
          La aprobación y desembolso de microcréditos para emprendimiento está reservada exclusivamente a la
          <strong> Administración</strong>. El personal de Recepción puede consultar la cartera y orientar a los
          beneficiarios. Los pagos de cuotas se registran únicamente en la terminal de <strong>Caja</strong>.
        </div>
      )}

      {esAdmin && mostrarForm && (
        <div className="panel" role="region" aria-label="Formulario para otorgar nuevo préstamo">
          <h2>Aprobar y otorgar nuevo préstamo de emprendimiento</h2>
          {error && (
            <div className="alerta alerta-error" role="alert">
              {error}
            </div>
          )}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              crear.mutate();
            }}
          >
            <div className="form-grid">
              <div className="campo">
                <label htmlFor="select-beneficiario">Beneficiario solicitante</label>
                <select
                  id="select-beneficiario"
                  required
                  value={form.beneficiarioId}
                  onChange={(e) => setForm({ ...form, beneficiarioId: Number(e.target.value) })}
                >
                  <option value={0} disabled>
                    Selecciona un beneficiario...
                  </option>
                  {beneficiarios.data?.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.nombres} {b.apellidos} — {b.cedula}
                    </option>
                  ))}
                </select>
              </div>

              <div className="campo">
                <label htmlFor="select-rubro">Rubro del emprendimiento</label>
                <select
                  id="select-rubro"
                  value={form.rubro}
                  onChange={(e) => setForm({ ...form, rubro: Number(e.target.value) })}
                >
                  {Object.entries(RubroTexto).map(([v, t]) => (
                    <option key={v} value={v}>
                      {t}
                    </option>
                  ))}
                </select>
              </div>

              <div className="campo">
                <label htmlFor="input-monto-aprobado">Monto aprobado (RD$)</label>
                <input
                  id="input-monto-aprobado"
                  required
                  type="number"
                  min={1}
                  step="100"
                  value={form.montoAprobado}
                  onChange={(e) => setForm({ ...form, montoAprobado: Number(e.target.value) })}
                />
              </div>

              <div className="campo">
                <label htmlFor="select-plazo">Plazo de amortización</label>
                <select
                  id="select-plazo"
                  value={form.plazoMeses}
                  onChange={(e) => setForm({ ...form, plazoMeses: Number(e.target.value) })}
                >
                  <option value={6}>6 meses</option>
                  <option value={12}>12 meses</option>
                  <option value={18}>18 meses</option>
                  <option value={24}>24 meses</option>
                </select>
              </div>

              <div className="campo campo-checkbox" style={{ gridColumn: "1 / -1" }}>
                <input
                  type="checkbox"
                  id="familia"
                  checked={form.involucraFamilia}
                  onChange={(e) => setForm({ ...form, involucraFamilia: e.target.checked })}
                />
                <label htmlFor="familia">El proyecto de emprendimiento involucra al núcleo familiar</label>
              </div>
            </div>

            <button
              className="btn btn-exito"
              style={{ marginTop: 14 }}
              type="submit"
              disabled={crear.isPending || form.beneficiarioId === 0}
            >
              {crear.isPending ? "Aprobando préstamo..." : "Aprobar y Registrar Préstamo"}
            </button>
          </form>
        </div>
      )}

      <DataTable
        columnas={columnas}
        filas={prestamos.data ?? []}
        claveFila={(p) => p.id}
        cargando={prestamos.isLoading}
        tituloTabla="Cartera de préstamos de emprendimiento"
        nombreArchivo="prestamos-emprendimiento"
        vacio="No hay préstamos activos registrados en la cartera."
      />
    </>
  );
}
