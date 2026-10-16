import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { crearSolicitudCita, consultarSolicitudes } from "../api/endpoints";
import { ApiError } from "../api/client";
import { EstadoCitaTexto, SedeTexto, TipoServicioCitaTexto, type SolicitudCita, type TipoServicioCita } from "../api/types";
import { PublicNavbar } from "../components/PublicNavbar";
import { PublicFooter } from "../components/PublicFooter";

// Página pública: cualquier persona puede solicitar una cita y además consultar el
// estado de sus solicitudes anteriores ingresando su teléfono o código de solicitud.
const SERVICIOS_DISPONIBLES: TipoServicioCita[] = [3, 4];
const FECHA_MINIMA_HOY = new Date().toISOString().slice(0, 10);

export function AgendarCitaPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const tab: "agendar" | "consultar" = searchParams.get("tab") === "consultar" ? "consultar" : "agendar";
  const setTab = (nuevoTab: "agendar" | "consultar") => {
    if (nuevoTab === "consultar") {
      setSearchParams({ tab: "consultar" });
    } else {
      setSearchParams({});
    }
  };

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
  const [filtroActivo, setFiltroActivo] = useState(searchParams.get("telefono") ?? "");

  const consultaQuery = useQuery({
    queryKey: ["solicitudes-publicas", filtroActivo],
    queryFn: () => consultarSolicitudes(filtroActivo),
    enabled: tab === "consultar" && filtroActivo.trim().length > 0,
  });

  const resultados = filtroActivo.trim().length > 0 ? (consultaQuery.data ?? null) : null;
  const buscando = consultaQuery.isFetching;
  const errorBusqueda = consultaQuery.error
    ? (consultaQuery.error instanceof ApiError ? consultaQuery.error.message : "Error al consultar las solicitudes. Verifica tu conexión.")
    : null;

  const ejecutarBusqueda = (termino: string) => {
    if (!termino.trim()) return;
    setFiltroActivo(termino.trim());
  };

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
    <div className="landing-moderna">
      <PublicNavbar paginaActiva="citas" />

      {/* Hero Dedicado de Citas */}
      <section className="pagina-publica-hero">
        <div className="pagina-publica-hero-inner">
          <div className="breadcrumb-nav">
            <Link to="/">Inicio</Link>
            <span>/</span>
            <span className="breadcrumb-actual">Portal de Citas y Turnos</span>
          </div>
          <span className="hero-pill-badge">📅 Atención Médica y Terapias Especializadas</span>
          <h1 className="pagina-hero-titulo">
            Portal de Citas y <span className="texto-destacado-verde">Consultas en Línea</span>
          </h1>
          <p className="pagina-hero-subtitulo">
            Agenda tu cita para consultas oftalmológicas, terapias en el Centro de Masajes TACTO o capacitación en rehabilitación.
            También puedes consultar el estado de tus solicitudes con tu número telefónico.
          </p>
        </div>
      </section>

      <section className="seccion-citas-formulario">
        <div className="citas-form-container">
          {/* Pestañas de Navegación con Efectos de Luz */}
          <div className="citas-tabs-bar">
            <button
              type="button"
              className={`btn-cita-tab ${tab === "agendar" ? "activo" : ""}`}
              onClick={() => {
                setTab("agendar");
                setSearchParams({});
              }}
            >
              <span aria-hidden="true">📝 </span>
              <span>Solicitar nueva cita</span>
            </button>
            <button
              type="button"
              className={`btn-cita-tab ${tab === "consultar" ? "activo" : ""}`}
              onClick={() => {
                setTab("consultar");
                setSearchParams({ tab: "consultar" });
              }}
            >
              <span aria-hidden="true">🔍 </span>
              <span>Consultar mis solicitudes</span>
            </button>
          </div>

        {tab === "agendar" ? (
          solicitudCreada ? (
            <div className="panel">
              <div className="alerta alerta-exito" role="status" aria-live="polite">
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
                <button type="button" className="btn btn-primario" onClick={() => window.print()}>
                  <span aria-hidden="true">🖨️ </span>
                  <span>Imprimir comprobante</span>
                </button>
                <button
                  type="button"
                  className="btn btn-exito"
                  onClick={() => {
                    setTerminoBusqueda(solicitudCreada.telefono);
                    setTab("consultar");
                    ejecutarBusqueda(solicitudCreada.telefono);
                  }}
                >
                  <span aria-hidden="true">🔍 </span>
                  <span>Ver estado de mi cita</span>
                </button>
                <button
                  type="button"
                  className="btn btn-secundario"
                  onClick={() => {
                    setSolicitudCreada(null);
                    setForm({ nombreSolicitante: "", telefono: "", correo: "", tipoServicio: 3, sedePreferida: 1, fechaPreferida: "", comentario: "" });
                  }}
                >
                  <span aria-hidden="true">➕ </span>
                  <span>Solicitar otra cita</span>
                </button>
              </div>
            </div>
          ) : (
            <div className="panel">
              <div
                style={{
                  background: "linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%)",
                  border: "1px solid #bfdbfe",
                  borderRadius: 12,
                  padding: "14px 18px",
                  marginBottom: 20,
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  flexWrap: "wrap",
                  gap: 12,
                }}
              >
                <div>
                  <strong style={{ color: "#1e3a8a", display: "block", fontSize: 14 }}>
                    👤 ¿Ya eres paciente o prefieres no recordar códigos?
                  </strong>
                  <span style={{ color: "#1e40af", fontSize: 13 }}>
                    Crea tu cuenta verificada en el Portal del Paciente para ver todas tus citas automáticamente.
                  </span>
                </div>
                <Link
                  to="/portal-paciente"
                  className="btn btn-primario"
                  style={{ fontSize: 13, padding: "8px 16px", textDecoration: "none" }}
                >
                  Ir al Portal del Paciente →
                </Link>
              </div>

              {error && <div className="alerta alerta-error">{error}</div>}
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  if (!form.correo.trim() || !form.correo.includes("@") || !form.correo.includes(".")) {
                    setError("Por favor ingresa un correo electrónico válido para recibir tu confirmación.");
                    return;
                  }
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
                    <label>
                      Correo Electrónico <span style={{ color: "#dc2626", fontWeight: 700 }}>* (Obligatorio)</span>
                    </label>
                    <input
                      type="email"
                      required
                      placeholder="ejemplo@correo.com"
                      value={form.correo}
                      onChange={(e) => setForm({ ...form, correo: e.target.value })}
                    />
                    <small style={{ color: "#64748b", fontSize: 11, marginTop: 4, display: "block" }}>
                      Te enviaremos la confirmación oficial y podrás acceder a tus citas desde el Portal del Paciente.
                    </small>
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
                      min={FECHA_MINIMA_HOY}
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
                        background: "#ffffff",
                        border: "1px solid var(--slate-300)",
                        borderRadius: 8,
                        padding: 16,
                        boxShadow: "0 1px 3px rgba(0,0,0,0.05)",
                      }}
                    >
                      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 10 }}>
                        <span style={{ fontSize: 16, fontWeight: 700, color: "var(--azul-oscuro)" }}>
                          SOL-{String(c.id).padStart(6, "0")}
                        </span>
                        {tagEstado(c.estado)}
                      </div>

                      <div className="form-grid" style={{ fontSize: 13 }}>
                        <div>
                          <strong style={{ color: "var(--slate-500)" }}>Solicitante:</strong>
                          <div style={{ color: "var(--slate-900)", fontWeight: 600 }}>{c.nombreSolicitante}</div>
                        </div>
                        <div>
                          <strong style={{ color: "var(--slate-500)" }}>Teléfono:</strong>
                          <div style={{ color: "var(--slate-900)" }}>{c.telefono}</div>
                        </div>
                        <div>
                          <strong style={{ color: "var(--slate-500)" }}>Servicio:</strong>
                          <div style={{ color: "var(--slate-900)" }}>{TipoServicioCitaTexto[c.tipoServicio]}</div>
                        </div>
                        <div>
                          <strong style={{ color: "var(--slate-500)" }}>Sede:</strong>
                          <div style={{ color: "var(--slate-900)" }}>{SedeTexto[c.sedePreferida]}</div>
                        </div>
                        <div>
                          <strong style={{ color: "var(--slate-500)" }}>Fecha preferida:</strong>
                          <div style={{ color: "var(--slate-900)" }}>{new Date(c.fechaPreferida).toLocaleDateString()}</div>
                        </div>
                        <div>
                          <strong style={{ color: "var(--slate-500)" }}>Fecha de solicitud:</strong>
                          <div style={{ color: "var(--slate-900)" }}>{new Date(c.fechaSolicitud).toLocaleString()}</div>
                        </div>
                      </div>

                      <div
                        style={{
                          marginTop: 12,
                          padding: "10px 14px",
                          borderRadius: 6,
                          fontSize: 12,
                          background: c.estado === 1 ? "#fffbeb" : c.estado === 2 ? "#ecfdf5" : "#fef2f2",
                          border: `1px solid ${c.estado === 1 ? "#fde68a" : c.estado === 2 ? "#a7f3d0" : "#fecaca"}`,
                          color: c.estado === 1 ? "#92400e" : c.estado === 2 ? "#065f46" : "#991b1b",
                        }}
                      >
                        {c.estado === 1 && (
                          <span>
                            <strong aria-hidden="true">ℹ️ </strong>
                            Tu solicitud está en cola de revisión por el personal de Recepción. Te contactarán por teléfono para confirmar los detalles.
                          </span>
                        )}
                        {c.estado === 2 && (
                          <span>
                            <strong aria-hidden="true">✅ </strong>
                            ¡Tu cita ha sido confirmada por Recepción! {c.tipoServicio === 3 ? "Puedes acudir a tu sesión de Masaje TACTO y realizar el cobro directamente en Caja." : "Te esperamos en la sede en la fecha asignada."}
                          </span>
                        )}
                        {c.estado === 3 && (
                          <span>
                            <strong aria-hidden="true">❌ </strong>
                            Esta solicitud fue cancelada por Recepción o por el solicitante.
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
        </div>
      </section>

      <PublicFooter />
    </div>
  );
}
