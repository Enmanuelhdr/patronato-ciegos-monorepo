import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { crearSolicitudCita } from "../api/endpoints";
import { ApiError } from "../api/client";
import { EstadoCitaTexto, SedeTexto, TipoServicioCitaTexto, type SolicitudCita, type TipoServicioCita } from "../api/types";

// Página pública: cualquier persona (todavía no registrada en el sistema) puede pedir que
// la contacten para agendar. Recepción confirma o cancela desde /panel/citas.
//
// Solo se ofrecen los servicios que este formulario puede manejar sin más pasos:
// - Masaje TACTO: no requiere ser Beneficiario, confirmar lo deja listo para cobro en Caja.
// - Otro: contacto general, no promete agendar nada automáticamente.
// Consulta Oftalmológica y Rehabilitación exigen ser Beneficiario registrado (cédula +
// condición visual), algo que una persona anónima no puede establecer aquí — por eso
// Recepción las agenda directo desde sus propias pantallas (/panel/consultas,
// /panel/rehabilitacion), nunca a partir de esta solicitud pública.
const SERVICIOS_DISPONIBLES: TipoServicioCita[] = [3, 4];

export function AgendarCitaPage() {
  const [form, setForm] = useState({
    nombreSolicitante: "",
    telefono: "",
    correo: "",
    tipoServicio: 3,
    sedePreferida: 1,
    fechaPreferida: "",
    comentario: "",
  });
  const [solicitudCreada, setSolicitudCreada] = useState<SolicitudCita | null>(null);
  const [error, setError] = useState<string | null>(null);

  const solicitar = useMutation({
    mutationFn: () =>
      crearSolicitudCita({
        ...form,
        fechaPreferida: form.fechaPreferida ? new Date(form.fechaPreferida).toISOString() : new Date().toISOString(),
      }),
    onSuccess: (data) => {
      setSolicitudCreada(data);
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo enviar la solicitud. Intenta de nuevo."),
  });

  return (
    <div className="landing">
      <header className="landing-nav">
        <Link to="/" className="landing-nav-marca">
          <div className="sidebar-escudo">PNC</div>
          <span>Patronato Nacional de Ciegos</span>
        </Link>
        <Link to="/" className="btn btn-secundario">← Volver al sitio</Link>
      </header>

      <section className="landing-seccion" style={{ maxWidth: 640, margin: "0 auto" }}>
        <h2>Agenda tu cita</h2>
        <p className="landing-seccion-sub">
          Déjanos tus datos y el servicio que necesitas. Recepción te contactará para confirmar fecha y hora exactas.
        </p>

        {solicitudCreada ? (
          <div className="panel">
            <div className="alerta alerta-exito">
              ¡Solicitud enviada! Te contactaremos pronto al teléfono {solicitudCreada.telefono} para confirmar tu cita.
            </div>

            <div id="comprobante-cita" className="comprobante-imprimible">
              <h3 style={{ marginTop: 18, marginBottom: 4 }}>Comprobante de solicitud</h3>
              <p style={{ fontSize: 22, fontWeight: 700, color: "#1e3a8a", margin: "4px 0 14px" }}>
                SOL-{String(solicitudCreada.id).padStart(6, "0")}
              </p>
              <div className="form-grid">
                <div className="campo">
                  <label>Solicitante</label>
                  <p style={{ margin: 0 }}>{solicitudCreada.nombreSolicitante}</p>
                </div>
                <div className="campo">
                  <label>Teléfono</label>
                  <p style={{ margin: 0 }}>{solicitudCreada.telefono}</p>
                </div>
                <div className="campo">
                  <label>Servicio solicitado</label>
                  <p style={{ margin: 0 }}>{TipoServicioCitaTexto[solicitudCreada.tipoServicio]}</p>
                </div>
                <div className="campo">
                  <label>Sede de preferencia</label>
                  <p style={{ margin: 0 }}>{SedeTexto[solicitudCreada.sedePreferida]}</p>
                </div>
                <div className="campo">
                  <label>Fecha de preferencia</label>
                  <p style={{ margin: 0 }}>{new Date(solicitudCreada.fechaPreferida).toLocaleDateString()}</p>
                </div>
                <div className="campo">
                  <label>Solicitado el</label>
                  <p style={{ margin: 0 }}>{new Date(solicitudCreada.fechaSolicitud).toLocaleString()}</p>
                </div>
                <div className="campo">
                  <label>Estado</label>
                  <p style={{ margin: 0 }}>{EstadoCitaTexto[solicitudCreada.estado]}</p>
                </div>
              </div>
              <p style={{ marginTop: 14, fontSize: 14, color: "#475569" }}>
                Conserva este código. Recepción te contactará para confirmar fecha y hora exactas.
              </p>
            </div>

            <div style={{ display: "flex", gap: 10, marginTop: 14 }}>
              <button className="btn btn-primario" onClick={() => window.print()}>
                Imprimir comprobante
              </button>
              <button
                className="btn btn-secundario"
                onClick={() => {
                  setSolicitudCreada(null);
                  setForm({ nombreSolicitante: "", telefono: "", correo: "", tipoServicio: 3, sedePreferida: 1, fechaPreferida: "", comentario: "" });
                }}
              >
                Solicitar otra cita
              </button>
            </div>
          </div>
        ) : (
          <div className="panel">
            {error && <div className="alerta alerta-error">{error}</div>}
            <form
              onSubmit={(e) => {
                e.preventDefault();
                solicitar.mutate();
              }}
            >
              <div className="form-grid">
                <div className="campo">
                  <label>Nombre completo</label>
                  <input
                    required
                    value={form.nombreSolicitante}
                    onChange={(e) => setForm({ ...form, nombreSolicitante: e.target.value })}
                  />
                </div>
                <div className="campo">
                  <label>Teléfono</label>
                  <input required value={form.telefono} onChange={(e) => setForm({ ...form, telefono: e.target.value })} />
                </div>
                <div className="campo">
                  <label>Correo (opcional)</label>
                  <input type="email" value={form.correo} onChange={(e) => setForm({ ...form, correo: e.target.value })} />
                </div>
                <div className="campo">
                  <label>Servicio que necesitas</label>
                  <select
                    value={form.tipoServicio}
                    onChange={(e) => setForm({ ...form, tipoServicio: Number(e.target.value) })}
                  >
                    {SERVICIOS_DISPONIBLES.map((v) => (
                      <option key={v} value={v}>{TipoServicioCitaTexto[v]}</option>
                    ))}
                  </select>
                </div>
                <div className="campo">
                  <label>Sede de tu preferencia</label>
                  <select
                    value={form.sedePreferida}
                    onChange={(e) => setForm({ ...form, sedePreferida: Number(e.target.value) })}
                  >
                    {Object.entries(SedeTexto).map(([v, t]) => (
                      <option key={v} value={v}>{t}</option>
                    ))}
                  </select>
                </div>
                <div className="campo">
                  <label>Fecha de tu preferencia</label>
                  <input
                    type="date"
                    required
                    min={new Date().toISOString().slice(0, 10)}
                    value={form.fechaPreferida}
                    onChange={(e) => setForm({ ...form, fechaPreferida: e.target.value })}
                  />
                </div>
                <div className="campo" style={{ gridColumn: "1 / -1" }}>
                  <label>Comentario (opcional)</label>
                  <input
                    value={form.comentario}
                    onChange={(e) => setForm({ ...form, comentario: e.target.value })}
                    placeholder="Cuéntanos brevemente qué necesitas"
                  />
                </div>
              </div>
              <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={solicitar.isPending}>
                {solicitar.isPending ? "Enviando..." : "Solicitar cita"}
              </button>
            </form>
          </div>
        )}
      </section>
    </div>
  );
}
