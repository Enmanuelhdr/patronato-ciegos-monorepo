import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { crearSolicitudCita } from "../api/endpoints";
import { ApiError } from "../api/client";
import { SedeTexto, TipoServicioCitaTexto } from "../api/types";

// Página pública: cualquier persona (todavía no registrada en el sistema) puede pedir que
// la contacten para agendar. Recepción confirma o cancela desde /panel/citas.
export function AgendarCitaPage() {
  const [form, setForm] = useState({
    nombreSolicitante: "",
    telefono: "",
    correo: "",
    tipoServicio: 1,
    sedePreferida: 1,
    fechaPreferida: "",
    comentario: "",
  });
  const [enviado, setEnviado] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const solicitar = useMutation({
    mutationFn: () =>
      crearSolicitudCita({
        ...form,
        fechaPreferida: form.fechaPreferida ? new Date(form.fechaPreferida).toISOString() : new Date().toISOString(),
      }),
    onSuccess: () => {
      setEnviado(true);
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

        {enviado ? (
          <div className="panel">
            <div className="alerta alerta-exito">
              ¡Solicitud enviada! Te contactaremos pronto al teléfono {form.telefono} para confirmar tu cita.
            </div>
            <button
              className="btn btn-primario"
              style={{ marginTop: 14 }}
              onClick={() => {
                setEnviado(false);
                setForm({ nombreSolicitante: "", telefono: "", correo: "", tipoServicio: 1, sedePreferida: 1, fechaPreferida: "", comentario: "" });
              }}
            >
              Solicitar otra cita
            </button>
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
                    {Object.entries(TipoServicioCitaTexto).map(([v, t]) => (
                      <option key={v} value={v}>{t}</option>
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
