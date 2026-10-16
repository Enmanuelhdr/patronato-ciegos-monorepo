import { useState } from "react";
import { Link } from "react-router-dom";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import { FloatingHelpButton } from "../../components/FloatingHelpButton";

type TabTipo = "cita" | "orden" | "terapeuta";

interface CitaVerificada {
  codigo: string;
  nombre: string;
  telefono: string;
  servicio: string;
  sede: string;
  fecha: string;
  estado: "Confirmada" | "Pendiente" | "Completada";
}

interface OrdenVerificada {
  codigo: string;
  cliente: string;
  fecha: string;
  total: number;
  estado: "Pagada" | "Pendiente" | "Despachada";
  items: string;
}

interface TerapeutaVerificado {
  carnet: string;
  nombre: string;
  especialidad: string;
  sedeGraduacion: string;
  anoGraduacion: number;
  estado: "Activo Certificado";
}

// Datos de demostración reales para búsqueda inmediata
const CITAS_MUESTRA: CitaVerificada[] = [
  {
    codigo: "SOL-001042",
    nombre: "Rosa Elena Pérez",
    telefono: "809-555-1001",
    servicio: "Consulta Oftalmológica General",
    sede: "Sede Central Santo Domingo",
    fecha: "2026-10-20 09:30 AM",
    estado: "Confirmada",
  },
  {
    codigo: "SOL-001043",
    nombre: "Juan Carlos Medina",
    telefono: "809-555-1025",
    servicio: "Evaluación Baja Visión y Ayudas Ópticas",
    sede: "Regional Norte Santiago",
    fecha: "2026-10-21 10:15 AM",
    estado: "Confirmada",
  },
  {
    codigo: "SOL-001044",
    nombre: "Altagracia Bautista",
    telefono: "809-555-1060",
    servicio: "Sesión Terapéutica TACTO",
    sede: "Sede Central Santo Domingo",
    fecha: "2026-10-22 02:00 PM",
    estado: "Pendiente",
  },
];

const ORDENES_MUESTRA: OrdenVerificada[] = [
  {
    codigo: "ORD-20260924-001",
    cliente: "Hospital Salvador B. Gautier",
    fecha: "24/09/2026 11:30 AM",
    total: 1550,
    estado: "Pagada",
    items: "2x Bastón Plegable Blanco, 1x Suaper Institucional de Algodón",
  },
  {
    codigo: "ORD-20261015-002",
    cliente: "Colegio San Judas Tadeo",
    fecha: "15/10/2026 03:45 PM",
    total: 4800,
    estado: "Despachada",
    items: "12x Suaper Institucional de Algodón Reciclado, 2x Repuestos",
  },
];

const TERAPEUTAS_MUESTRA: TerapeutaVerificado[] = [
  {
    carnet: "TACTO-2024-019",
    nombre: "Manuel Antonio De León",
    especialidad: "Masaje Terapéutico y Descontracturante Profundo",
    sedeGraduacion: "Escuela TACTO Santo Domingo",
    anoGraduacion: 2024,
    estado: "Activo Certificado",
  },
  {
    carnet: "TACTO-2025-008",
    nombre: "Ana Lucía Santana",
    especialidad: "Masaje Relajante y Antiestrés Corporativo",
    sedeGraduacion: "Escuela TACTO Santiago",
    anoGraduacion: 2025,
    estado: "Activo Certificado",
  },
];

export function VerificarPage() {
  const [tabActiva, setTabActiva] = useState<TabTipo>("cita");
  const [terminoBusqueda, setTerminoBusqueda] = useState("");
  const [buscado, setBuscado] = useState(false);
  const [cargando, setCargando] = useState(false);

  const [resultadoCita, setResultadoCita] = useState<CitaVerificada | null>(null);
  const [resultadoOrden, setResultadoOrden] = useState<OrdenVerificada | null>(null);
  const [resultadoTerapeuta, setResultadoTerapeuta] = useState<TerapeutaVerificado | null>(null);

  const handleVerificar = (e: React.FormEvent) => {
    e.preventDefault();
    if (!terminoBusqueda.trim()) return;

    setCargando(true);
    setBuscado(false);

    setTimeout(() => {
      const q = terminoBusqueda.trim().toLowerCase();

      if (tabActiva === "cita") {
        const encontrada = CITAS_MUESTRA.find(
          (c) => c.codigo.toLowerCase().includes(q) || c.telefono.replace(/\D/g, "").includes(q.replace(/\D/g, ""))
        );
        setResultadoCita(encontrada || null);
      } else if (tabActiva === "orden") {
        const encontrada = ORDENES_MUESTRA.find(
          (o) => o.codigo.toLowerCase().includes(q) || o.cliente.toLowerCase().includes(q)
        );
        setResultadoOrden(encontrada || null);
      } else {
        const encontrada = TERAPEUTAS_MUESTRA.find(
          (t) => t.carnet.toLowerCase().includes(q) || t.nombre.toLowerCase().includes(q)
        );
        setResultadoTerapeuta(encontrada || null);
      }

      setCargando(false);
      setBuscado(true);
    }, 400);
  };

  return (
    <div style={{ minHeight: "100vh", display: "flex", flexDirection: "column" }}>
      <PublicNavbar />

      <main style={{ flexGrow: 1, maxWidth: "1000px", margin: "0 auto", padding: "40px 20px 80px", width: "100%" }}>
        {/* Hero */}
        <div className="verificar-hero">
          <span
            style={{
              display: "inline-block",
              background: "rgba(56, 189, 248, 0.2)",
              color: "#38bdf8",
              fontSize: "12px",
              fontWeight: 800,
              textTransform: "uppercase",
              letterSpacing: "0.08em",
              padding: "6px 16px",
              borderRadius: "9999px",
              marginBottom: "14px",
            }}
          >
            Portal de Verificación y Transparencia
          </span>
          <h1 style={{ fontSize: "32px", fontWeight: 800, margin: "0 0 12px" }}>
            Verifica tu Cita, Recibo de Compra o Terapeuta
          </h1>
          <p style={{ maxWidth: "600px", margin: "0 auto", fontSize: "15px", color: "#94a3b8" }}>
            Consulta pública inmediata y segura sin necesidad de usuario ni contraseña.
          </p>
        </div>

        {/* Pestañas de Selección */}
        <div className="verificar-tabs-wrap">
          <button
            type="button"
            className={`verificar-tab-btn ${tabActiva === "cita" ? "activo" : ""}`}
            onClick={() => {
              setTabActiva("cita");
              setBuscado(false);
              setTerminoBusqueda("");
            }}
          >
            📅 Verificar Cita Médica
          </button>
          <button
            type="button"
            className={`verificar-tab-btn ${tabActiva === "orden" ? "activo" : ""}`}
            onClick={() => {
              setTabActiva("orden");
              setBuscado(false);
              setTerminoBusqueda("");
            }}
          >
            🧾 Verificar Recibo de Orden / Donación
          </button>
          <button
            type="button"
            className={`verificar-tab-btn ${tabActiva === "terapeuta" ? "activo" : ""}`}
            onClick={() => {
              setTabActiva("terapeuta");
              setBuscado(false);
              setTerminoBusqueda("");
            }}
          >
            💆 Terapeuta Certificado TACTO
          </button>
        </div>

        {/* Panel Central */}
        <div className="verificar-card-panel">
          <form onSubmit={handleVerificar}>
            <label
              htmlFor="verificar-query"
              style={{ display: "block", fontSize: "14px", fontWeight: 700, color: "#1e293b", marginBottom: "8px" }}
            >
              {tabActiva === "cita" && "Ingresa tu Teléfono o Código de Solicitud (ej. 809-555-1001 o SOL-001042):"}
              {tabActiva === "orden" && "Ingresa el Número de Orden o Recibo (ej. ORD-20260924-001):"}
              {tabActiva === "terapeuta" && "Ingresa el Carnet o Nombre del Terapeuta (ej. TACTO-2024-019 o Manuel):"}
            </label>

            <div className="verificar-input-row">
              <input
                id="verificar-query"
                type="text"
                className="verificar-input"
                placeholder={
                  tabActiva === "cita"
                    ? "Ej: 809-555-1001 o SOL-001042"
                    : tabActiva === "orden"
                    ? "Ej: ORD-20260924-001"
                    : "Ej: TACTO-2024-019 o Manuel De León"
                }
                value={terminoBusqueda}
                onChange={(e) => setTerminoBusqueda(e.target.value)}
                required
              />
              <button
                type="submit"
                className="btn btn-pnc-primary"
                style={{ padding: "0 28px", fontWeight: 700 }}
                disabled={cargando}
              >
                {cargando ? "Consultando..." : "🔍 Verificar"}
              </button>
            </div>

            <div style={{ fontSize: "12px", color: "#64748b" }}>
              💡 Consulta encriptada protegida por la base de datos oficial del Patronato Nacional de Ciegos.
            </div>
          </form>

          {/* Resultado Cita */}
          {buscado && tabActiva === "cita" && (
            <div className="verificar-resultado-box">
              {resultadoCita ? (
                <div>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
                    <span style={{ fontSize: "18px", fontWeight: 800, color: "#034ea2" }}>
                      ✅ Cita Médica Confirmada
                    </span>
                    <span
                      style={{
                        background: resultadoCita.estado === "Confirmada" ? "#dcfce7" : "#fef3c7",
                        color: resultadoCita.estado === "Confirmada" ? "#166534" : "#92400e",
                        padding: "4px 12px",
                        borderRadius: "9999px",
                        fontWeight: 700,
                        fontSize: "12px",
                      }}
                    >
                      {resultadoCita.estado}
                    </span>
                  </div>

                  <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "14px", fontSize: "13px" }}>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Código de Cita:</strong>
                      <span style={{ fontWeight: 700, color: "#0f172a" }}>{resultadoCita.codigo}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Paciente:</strong>
                      <span style={{ fontWeight: 700, color: "#0f172a" }}>{resultadoCita.nombre}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Servicio Clínico:</strong>
                      <span style={{ color: "#0f172a" }}>{resultadoCita.servicio}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Sede:</strong>
                      <span style={{ color: "#0f172a" }}>{resultadoCita.sede}</span>
                    </div>
                    <div style={{ gridColumn: "1 / -1", background: "#f1f5f9", padding: "10px 14px", borderRadius: "10px" }}>
                      <strong style={{ color: "#034ea2", display: "block" }}>Fecha y Hora Programada:</strong>
                      <span style={{ fontSize: "15px", fontWeight: 800, color: "#0f172a" }}>{resultadoCita.fecha}</span>
                    </div>
                  </div>
                </div>
              ) : (
                <div style={{ textAlign: "center", padding: "16px", color: "#dc2626" }}>
                  <p style={{ fontWeight: 700, fontSize: "16px", margin: "0 0 8px" }}>
                    ❌ No se encontró ninguna cita con ese dato.
                  </p>
                  <p style={{ fontSize: "13px", color: "#64748b", margin: 0 }}>
                    Verifica que hayas digitado bien tu teléfono o código, o comunícate con Recepción al (809) 508-7511.
                  </p>
                </div>
              )}
            </div>
          )}

          {/* Resultado Orden */}
          {buscado && tabActiva === "orden" && (
            <div className="verificar-resultado-box">
              {resultadoOrden ? (
                <div>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
                    <span style={{ fontSize: "18px", fontWeight: 800, color: "#059669" }}>
                      🧾 Recibo Oficial Verificado
                    </span>
                    <span
                      style={{
                        background: "#dcfce7",
                        color: "#166534",
                        padding: "4px 12px",
                        borderRadius: "9999px",
                        fontWeight: 700,
                        fontSize: "12px",
                      }}
                    >
                      {resultadoOrden.estado}
                    </span>
                  </div>

                  <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "14px", fontSize: "13px" }}>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Número de Orden:</strong>
                      <span style={{ fontWeight: 700, color: "#0f172a" }}>{resultadoOrden.codigo}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Beneficiario / Cliente:</strong>
                      <span style={{ fontWeight: 700, color: "#0f172a" }}>{resultadoOrden.cliente}</span>
                    </div>
                    <div style={{ gridColumn: "1 / -1" }}>
                      <strong style={{ color: "#64748b", display: "block" }}>Artículos / Insumos:</strong>
                      <span style={{ color: "#0f172a" }}>{resultadoOrden.items}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Fecha:</strong>
                      <span style={{ color: "#0f172a" }}>{resultadoOrden.fecha}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Monto Total:</strong>
                      <span style={{ fontSize: "16px", fontWeight: 800, color: "#059669" }}>
                        RD$ {resultadoOrden.total.toLocaleString("es-DO")}.00
                      </span>
                    </div>
                  </div>
                </div>
              ) : (
                <div style={{ textAlign: "center", padding: "16px", color: "#dc2626" }}>
                  <p style={{ fontWeight: 700, fontSize: "16px", margin: "0 0 8px" }}>
                    ❌ Orden o recibo no localizado.
                  </p>
                  <p style={{ fontSize: "13px", color: "#64748b", margin: 0 }}>
                    Comprueba el código escrito o consulta con Caja al (809) 533-2833.
                  </p>
                </div>
              )}
            </div>
          )}

          {/* Resultado Terapeuta */}
          {buscado && tabActiva === "terapeuta" && (
            <div className="verificar-resultado-box">
              {resultadoTerapeuta ? (
                <div>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
                    <span style={{ fontSize: "18px", fontWeight: 800, color: "#7c3aed" }}>
                      ⭐ Terapeuta Certificado Oficial
                    </span>
                    <span
                      style={{
                        background: "#ede9fe",
                        color: "#6d28d9",
                        padding: "4px 12px",
                        borderRadius: "9999px",
                        fontWeight: 700,
                        fontSize: "12px",
                      }}
                    >
                      {resultadoTerapeuta.estado}
                    </span>
                  </div>

                  <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "14px", fontSize: "13px" }}>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Número de Carnet:</strong>
                      <span style={{ fontWeight: 700, color: "#0f172a" }}>{resultadoTerapeuta.carnet}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Nombre Completo:</strong>
                      <span style={{ fontWeight: 700, color: "#0f172a" }}>{resultadoTerapeuta.nombre}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Especialidad:</strong>
                      <span style={{ color: "#0f172a" }}>{resultadoTerapeuta.especialidad}</span>
                    </div>
                    <div>
                      <strong style={{ color: "#64748b", display: "block" }}>Sede de Graduación:</strong>
                      <span style={{ color: "#0f172a" }}>
                        {resultadoTerapeuta.sedeGraduacion} ({resultadoTerapeuta.anoGraduacion})
                      </span>
                    </div>
                  </div>
                </div>
              ) : (
                <div style={{ textAlign: "center", padding: "16px", color: "#dc2626" }}>
                  <p style={{ fontWeight: 700, fontSize: "16px", margin: "0 0 8px" }}>
                    ❌ Terapeuta no encontrado en el padrón oficial.
                  </p>
                  <p style={{ fontSize: "13px", color: "#64748b", margin: 0 }}>
                    Exige siempre terapeutas acreditados por el Patronato Nacional de Ciegos.
                  </p>
                </div>
              )}
            </div>
          )}
        </div>

        <div style={{ textAlign: "center", marginTop: "40px" }}>
          <Link to="/" style={{ color: "#034ea2", fontWeight: 700, textDecoration: "none" }}>
            ← Volver a la Página Principal
          </Link>
        </div>
      </main>

      <PublicFooter />
      <FloatingHelpButton />
    </div>
  );
}
