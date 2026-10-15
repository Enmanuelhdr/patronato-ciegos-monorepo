import { useState, useEffect } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { crearSolicitudCita, consultarSolicitudes } from "../api/endpoints";
import { ApiError } from "../api/client";
import { EstadoCitaTexto, SedeTexto, TipoServicioCitaTexto, type SolicitudCita, type TipoServicioCita } from "../api/types";

// Página pública: cualquier persona puede solicitar una cita y además consultar el
// estado de sus solicitudes anteriores ingresando su teléfono o código de solicitud.
const SERVICIOS_DISPONIBLES: TipoServicioCita[] = [3, 4];

export function AgendarCitaPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [tab, setTab] = useState<"agendar" | "consultar">(
    searchParams.get("tab") === "consultar" ? "consultar" : "agendar"
  );

  // Formulario de nueva solicitud
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

  // Búsqueda de solicitudes existentes
  const [terminoBusqueda, setTerminoBusqueda] = useState(searchParams.get("telefono") ?? "");
  const [resultados, setResultados] = useState<SolicitudCita[] | null>(null);
  const [buscando, setBuscando] = useState(false);
  const [errorBusqueda, setErrorBusqueda] = useState<string | null>(null);

  useEffect(() => {
    const pTab = searchParams.get("tab");
    if (pTab === "consultar") {
      setTab("consultar");
      const tel = searchParams.get("telefono");
      if (tel) {
        setTerminoBusqueda(tel);
        ejecutarBusqueda(tel);
      }
    }
  }, [searchParams]);

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

  const ejecutarBusqueda = async (termino: string) => {
    if (!termino.trim()) return;
    setBuscando(true);
    setErrorBusqueda(null);
    try {
      const data = await consultarSolicitudes(termino);
      setResultados(data);
    } catch (e) {
      setErrorBusqueda(e instanceof ApiError ? e.message : "Error al consultar las solicitudes. Verifica tu conexión.");
    } finally {
      setBuscando(false);
    }
  };

  const handleBuscar = (e: React.FormEvent) => {
    e.preventDefault();
    ejecutarBusqueda(terminoBusqueda);
  };

  const tagEstado = (estado: number) => {
    switch (estado) {
      case 1:
        return <span className="tag tag-ambar">🟡 Pendiente</span>;
      case 2:
        return <span className="tag tag-verde">🟢 Confirmada</span>;
      case 3:
        return <span className="tag tag-gris">🔴 Cancelada</span>;
      default:
        return <span className="tag">{EstadoCitaTexto[estado as 1 | 2 | 3] ?? "Desconocido"}</span>;
    }
  };

  return (
    <div className="landing">
      <header className="landing-nav">
        <Link to="/" className="landing-nav-marca">
          <div className="sidebar-escudo">PNC</div>
          <span>Patronato Nacional de Ciegos</span>
        </Link>
        <Link to="/" className="btn btn-secundario">← Volver al sitio</Link>
      </header>

      <section className="landing-seccion" style={{ maxWidth: 680, margin: "0 auto" }}>
        <h2>Portal de Citas</h2>
        <p className="landing-seccion-sub">
          Solicita una nueva cita o consulta en tiempo real el estado de tus solicitudes enviadas.
        </p>

        {/* Pestañas de Navegación */}
        <div style={{ display: "flex", gap: 8, marginBottom: 20 }}>
          <button
            type="button"
            className={`btn ${tab === "agendar" ? "btn-primario" : "btn-secundario"}`}
            onClick={() => {
              setTab("agendar");
              setSearchParams({});
            }}
          >
            📝 Solicitar nueva cita
          </button>
          <button
            type="button"
            className={`btn ${tab === "consultar" ? "btn-primario" : "btn-secundario"}`}
            onClick={() => {
              setTab("consultar");
              setSearchParams({ tab: "consultar" });
            }}
          >
            🔍 Consultar mis solicitudes
          </button>
        </div>

        {tab === "agendar" ? (
          solicitudCreada ? (
            <div className="panel">
              <div className="alerta alerta-exito">
                ¡Solicitud enviada con éxito! Te contactaremos al teléfono {solicitudCreada.telefono} para confirmar.
              </div>

              <div id="comprobante-cita" className="comprobante-imprimible">
                <h3 style={{ marginTop: 18, marginBottom: 4 }}>Comprobante de solicitud</h3>
                <p style={{ fontSize: 22, fontWeight: 700, color: "#2563eb", margin: "4px 0 14px" }}>
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
                    <label>Estado actual</label>
                    <p style={{ margin: 0 }}>{tagEstado(solicitudCreada.estado)}</p>
                  </div>
                </div>
                <p style={{ marginTop: 14, fontSize: 13, color: "#94a3b8" }}>
                  Conserva este comprobante o tu número de teléfono. Puedes verificar el estado de tu cita en cualquier momento en la pestaña "Consultar mis solicitudes".
                </p>
              </div>

              <div style={{ display: "flex", gap: 10, marginTop: 16, flexWrap: "wrap" }}>
                <button className="btn btn-primario" onClick={() => window.print()}>
                  🖨️ Imprimir comprobante
                </button>
                <button
                  className="btn btn-exito"
                  onClick={() => {
                    setTerminoBusqueda(solicitudCreada.telefono);
                    setTab("consultar");
                    ejecutarBusqueda(solicitudCreada.telefono);
                  }}
                >
                  🔍 Ver estado de mi cita
                </button>
                <button
                  className="btn btn-secundario"
                  onClick={() => {
                    setSolicitudCreada(null);
                    setForm({ nombreSolicitante: "", telefono: "", correo: "", tipoServicio: 3, sedePreferida: 1, fechaPreferida: "", comentario: "" });
                  }}
                >
                  ➕ Solicitar otra cita
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
                      placeholder="Ej. Juan Pérez"
                      value={form.nombreSolicitante}
                      onChange={(e) => setForm({ ...form, nombreSolicitante: e.target.value })}
                    />
                  </div>
                  <div className="campo">
                    <label>Teléfono</label>
                    <input
                      required
                      placeholder="Ej. 809-555-0101"
                      value={form.telefono}
                      onChange={(e) => setForm({ ...form, telefono: e.target.value })}
                    />
                  </div>
                  <div className="campo">
                    <label>Correo (opcional)</label>
                    <input
                      type="email"
                      placeholder="correo@ejemplo.com"
                      value={form.correo}
                      onChange={(e) => setForm({ ...form, correo: e.target.value })}
                    />
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
          )
        ) : (
          /* Pestaña: Consultar Solicitudes */
          <div className="panel">
            <h3 style={{ marginTop: 0, marginBottom: 8 }}>Buscar solicitudes de cita</h3>
            <p style={{ color: "#94a3b8", fontSize: 13, marginBottom: 14 }}>
              Ingresa tu número de teléfono (ej. 8098156623) o tu código de solicitud (ej. SOL-000001) para ver el estado de tu cita.
            </p>

            <form onSubmit={handleBuscar} style={{ display: "flex", gap: 8, marginBottom: 20 }}>
              <input
                required
                style={{ flex: 1, padding: "8px 12px" }}
                placeholder="Teléfono o código (ej: 809-555-0101 o SOL-000001)"
                value={terminoBusqueda}
                onChange={(e) => setTerminoBusqueda(e.target.value)}
              />
              <button className="btn btn-primario" type="submit" disabled={buscando}>
                {buscando ? "Buscando..." : "Buscar"}
              </button>
            </form>

            {errorBusqueda && <div className="alerta alerta-error">{errorBusqueda}</div>}

            {resultados !== null && (
              resultados.length === 0 ? (
                <div className="alerta alerta-ambar">
                  No se encontraron solicitudes asociadas a <strong>{terminoBusqueda}</strong>. Verifica el número e intenta nuevamente.
                </div>
              ) : (
                <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
                  {resultados.map((c) => (
                    <div
                      key={c.id}
                      style={{
                        background: "#0f172a",
                        border: "1px solid #1e293b",
                        borderRadius: 8,
                        padding: 16,
                      }}
                    >
                      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 10 }}>
                        <span style={{ fontSize: 16, fontWeight: 700, color: "#38bdf8" }}>
                          SOL-{String(c.id).padStart(6, "0")}
                        </span>
                        {tagEstado(c.estado)}
                      </div>

                      <div className="form-grid" style={{ fontSize: 13 }}>
                        <div>
                          <strong style={{ color: "#94a3b8" }}>Solicitante:</strong>
                          <div>{c.nombreSolicitante}</div>
                        </div>
                        <div>
                          <strong style={{ color: "#94a3b8" }}>Teléfono:</strong>
                          <div>{c.telefono}</div>
                        </div>
                        <div>
                          <strong style={{ color: "#94a3b8" }}>Servicio:</strong>
                          <div>{TipoServicioCitaTexto[c.tipoServicio]}</div>
                        </div>
                        <div>
                          <strong style={{ color: "#94a3b8" }}>Sede:</strong>
                          <div>{SedeTexto[c.sedePreferida]}</div>
                        </div>
                        <div>
                          <strong style={{ color: "#94a3b8" }}>Fecha preferida:</strong>
                          <div>{new Date(c.fechaPreferida).toLocaleDateString()}</div>
                        </div>
                        <div>
                          <strong style={{ color: "#94a3b8" }}>Fecha de solicitud:</strong>
                          <div>{new Date(c.fechaSolicitud).toLocaleString()}</div>
                        </div>
                      </div>

                      <div style={{ marginTop: 12, padding: "8px 12px", background: "#1e293b", borderRadius: 6, fontSize: 12 }}>
                        {c.estado === 1 && (
                          <span style={{ color: "#fbbf24" }}>
                            ℹ️ Tu solicitud está en cola de revisión por el personal de Recepción. Te contactarán por teléfono para confirmar los detalles.
                          </span>
                        )}
                        {c.estado === 2 && (
                          <span style={{ color: "#4ade80" }}>
                            ✅ ¡Tu cita ha sido confirmada por Recepción! {c.tipoServicio === 3 ? "Puedes acudir a tu sesión de Masaje TACTO y realizar el cobro directamente en Caja." : "Te esperamos en la sede en la fecha asignada."}
                          </span>
                        )}
                        {c.estado === 3 && (
                          <span style={{ color: "#f87171" }}>
                            ❌ Esta solicitud fue cancelada por Recepción o por el solicitante.
                          </span>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )
            )}
          </div>
        )}
      </section>
    </div>
  );
}
