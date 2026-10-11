import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearMasaje, getMasajesPendientes } from "../api/endpoints";
import { ApiError } from "../api/client";
import { SedeTexto, type SesionMasajeTacto } from "../api/types";

export function MasajesPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["masajes-pendientes"], queryFn: getMasajesPendientes });
  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({
    nombreCliente: "",
    telefonoCliente: "",
    nombreMasajista: "David Silverio (Terapeuta Invidente)",
    sede: 1,
    duracionMinutos: 45,
    precioTarifa: 500,
  });

  const crear = useMutation({
    mutationFn: () => crearMasaje(form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["masajes-pendientes"] });
      setMostrarForm(false);
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo agendar la cita."),
  });

  const columnas: Columna<SesionMasajeTacto>[] = [
    { encabezado: "Cliente", render: (s) => s.nombreCliente },
    { encabezado: "Teléfono", render: (s) => s.telefonoCliente },
    { encabezado: "Masajista", render: (s) => s.nombreMasajista },
    { encabezado: "Sede", render: (s) => SedeTexto[s.sede] },
    { encabezado: "Duración", render: (s) => `${s.duracionMinutos} min` },
    { encabezado: "Tarifa", render: (s) => `RD$ ${s.precioTarifa.toFixed(2)}` },
    { encabezado: "Estado", render: (s) => <span className={`tag ${s.estaFacturadoEnCaja ? "tag-verde" : "tag-ambar"}`}>{s.estaFacturadoEnCaja ? "Cobrado" : "Pendiente de cobro en Caja"}</span> },
  ];

  return (
    <>
      <PageHeader
        titulo="Masajes TACTO"
        subtitulo="Agenda citas con los terapeutas invidentes. El cobro se realiza en la Caja."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Agendar cita"}</button>}
      />

      {mostrarForm && (
        <div className="panel">
          <h2>Nueva cita</h2>
          {error && <div className="alerta alerta-error">{error}</div>}
          <form onSubmit={(e) => { e.preventDefault(); crear.mutate(); }}>
            <div className="form-grid">
              <div className="campo">
                <label>Nombre del cliente</label>
                <input required value={form.nombreCliente} onChange={(e) => setForm({ ...form, nombreCliente: e.target.value })} />
              </div>
              <div className="campo">
                <label>Teléfono</label>
                <input value={form.telefonoCliente} onChange={(e) => setForm({ ...form, telefonoCliente: e.target.value })} />
              </div>
              <div className="campo">
                <label>Masajista</label>
                <input required value={form.nombreMasajista} onChange={(e) => setForm({ ...form, nombreMasajista: e.target.value })} />
              </div>
              <div className="campo">
                <label>Sede</label>
                <select value={form.sede} onChange={(e) => setForm({ ...form, sede: Number(e.target.value) })}>
                  {Object.entries(SedeTexto).map(([v, t]) => <option key={v} value={v}>{t}</option>)}
                </select>
              </div>
              <div className="campo">
                <label>Duración (minutos)</label>
                <select value={form.duracionMinutos} onChange={(e) => setForm({ ...form, duracionMinutos: Number(e.target.value) })}>
                  <option value={30}>30 mins</option>
                  <option value={45}>45 mins</option>
                  <option value={60}>60 mins</option>
                </select>
              </div>
              <div className="campo">
                <label>Tarifa (RD$)</label>
                <input required type="number" min={0} value={form.precioTarifa} onChange={(e) => setForm({ ...form, precioTarifa: Number(e.target.value) })} />
              </div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending}>
              {crear.isPending ? "Guardando..." : "Agendar cita"}
            </button>
          </form>
        </div>
      )}

      <DataTable columnas={columnas} filas={data ?? []} claveFila={(s) => s.id} cargando={isLoading} />
    </>
  );
}
