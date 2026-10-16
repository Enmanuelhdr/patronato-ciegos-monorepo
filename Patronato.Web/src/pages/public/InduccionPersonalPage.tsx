import { useState } from "react";
import { Link } from "react-router-dom";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import { FloatingHelpButton } from "../../components/FloatingHelpButton";

interface PreguntaInterna {
  id: string;
  pregunta: string;
  respuesta: string;
}

const FAQS_PERSONAL: PreguntaInterna[] = [
  {
    id: "credenciales",
    pregunta: "¿Cómo obtengo o recupero mis credenciales de acceso al sistema?",
    respuesta:
      "Tu usuario institucional te es entregado por el Departamento de Recursos Humanos y TI el primer día laboral. Al ingresar por primera vez, el sistema te solicitará obligatoriamente cambiar tu clave temporal por una personalizada de mínimo 6 caracteres. Si olvidas tu clave, contacta a soporte TI en la extensión 204.",
  },
  {
    id: "horarios",
    pregunta: "¿Cuáles son los horarios laborales y el código de vestimenta institucional?",
    respuesta:
      "La jornada habitual de atención es de lunes a viernes de 8:00 AM a 4:00 PM. El código de vestimenta es institucional formal / semi-formal o uniforme asignado según el área (batas clínicas para oftalmología, uniforme azul marino para masajes TACTO, identificación visible siempre).",
  },
  {
    id: "asistencia-ciego",
    pregunta: "¿Qué debo hacer si una persona ciega solicita ayuda en los pasillos de la sede?",
    respuesta:
      "Acércate con amabilidad y saluda diciendo: 'Hola, mi nombre es [Tu Nombre], trabajo aquí en [Tu Área], ¿desea que le acompañe o necesita alguna indicación?'. Si acepta, ofrécele tu codo para que se sujete y camina medio paso por delante. Nunca le tomes de la mano ni hales su bastón.",
  },
  {
    id: "permisos",
    pregunta: "¿Cómo se canalizan los permisos médicos, días libres o vacaciones?",
    respuesta:
      "Todas las solicitudes de permisos o licencias deben tramitarse con tu supervisor directo con un mínimo de 48 horas de anticipación (salvo emergencias médicas justificadas con certificado) y remitirse firmadas a la Gerencia de Gestión Humana.",
  },
  {
    id: "caja-sistema",
    pregunta: "¿Por qué el cajero solo opera en la terminal de escritorio y no en la web?",
    respuesta:
      "Por estrictas normas de seguridad bancaria y control de efectivo interno del Patronato. La caja opera exclusivamente en la aplicación de cobro dedicada (POS de escritorio) comunicada por socket criptográfico con el Gateway. La web está reservada a recepción, administración y consulta pública.",
  },
];

export function InduccionPersonalPage() {
  const [puestoSeleccionado, setPuestoSeleccionado] = useState<string>("recepcion");
  const [faqAbierto, setFaqAbierto] = useState<string | null>("credenciales");

  return (
    <div style={{ minHeight: "100vh", display: "flex", flexDirection: "column" }}>
      <PublicNavbar />

      <main style={{ flexGrow: 1, maxWidth: "1200px", margin: "0 auto", padding: "40px 20px 80px", width: "100%" }}>
        {/* Hero */}
        <div className="induccion-hero">
          <span
            style={{
              display: "inline-block",
              background: "rgba(255, 255, 255, 0.2)",
              color: "#ffffff",
              fontSize: "12px",
              fontWeight: 800,
              textTransform: "uppercase",
              letterSpacing: "0.08em",
              padding: "6px 16px",
              borderRadius: "9999px",
              marginBottom: "14px",
            }}
          >
            Portal de Bienvenida e Inducción Institucional
          </span>
          <h1 style={{ fontSize: "36px", fontWeight: 800, margin: "0 0 14px" }}>
            Bienvenido al Equipo del Patronato Nacional de Ciegos
          </h1>
          <p style={{ maxWidth: "760px", margin: "0 auto", fontSize: "16px", color: "#e0f2fe", lineHeight: "1.6" }}>
            Tu labor diaria transforma vidas en la República Dominicana. Esta guía te orientará en nuestros valores,
            el protocolo de trato digno a personas ciegas, y los instructivos operativos de tu puesto.
          </p>
        </div>

        {/* Sección: Protocolo de Trato Digno y Guía Vidente (Crucial) */}
        <section style={{ marginBottom: "50px" }}>
          <div style={{ textAlign: "center", marginBottom: "32px" }}>
            <span style={{ fontSize: "12px", fontWeight: 800, color: "#034ea2", textTransform: "uppercase", letterSpacing: "0.08em" }}>
              Cultura Institucional
            </span>
            <h2 style={{ fontSize: "26px", fontWeight: 800, color: "#0f172a", margin: "6px 0 0" }}>
              Protocolo Esencial de Trato Digno y Guía Vidente
            </h2>
          </div>

          <div className="induccion-grid">
            <div className="protocolo-card">
              <h3>
                <span>👋</span>
                <span>1. Saludo y Presentación</span>
              </h3>
              <ul className="reglas-lista">
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>Preséntate por tu nombre:</strong> Al acercarte a una persona con discapacidad visual, dile siempre quién eres y qué función tienes en la institución.
                  </div>
                </li>
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>Avisa al retirarte:</strong> Si estás conversando y debes alejarte, comunícaselo verbalmente para que no se quede hablando al aire.
                  </div>
                </li>
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>Habla en tono normal:</strong> La ceguera no afecta el sentido auditivo; no es necesario alzar la voz ni gritar.
                  </div>
                </li>
              </ul>
            </div>

            <div className="protocolo-card">
              <h3>
                <span>🤝</span>
                <span>2. Técnica de Guía Vidente</span>
              </h3>
              <ul className="reglas-lista">
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>Ofrece tu codo:</strong> Toca suavemente el dorso de su mano con tu antebrazo o codo en ángulo de 90° para que la persona se sujete cómodamente.
                  </div>
                </li>
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>Nunca hales ni empujes:</strong> Jamás tomes a la persona del brazo ni la empujes desde atrás; camina medio paso adelante respetando su ritmo.
                  </div>
                </li>
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>Anticipa desniveles:</strong> Avisa con una palabra calmada: 'escalón hacia arriba', 'escalón hacia abajo' o 'puerta estrecha'.
                  </div>
                </li>
              </ul>
            </div>

            <div className="protocolo-card">
              <h3>
                <span>🦯</span>
                <span>3. Respeto a Bastones y Lenguaje</span>
              </h3>
              <ul className="reglas-lista">
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>El bastón es intocable:</strong> Es la extensión táctil de la persona; nunca lo muevas de su sitio sin consultar primero con su dueño.
                  </div>
                </li>
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>Usa palabras naturales:</strong> Puedes decir sin temor 'mira', 've', o 'ciego'. No uses eufemismos como 'invidentito' o 'angelito'.
                  </div>
                </li>
                <li className="regla-item">
                  <span className="regla-icono">✓</span>
                  <div>
                    <strong>Direcciones exactas:</strong> Usa referencias de reloj ('a tus dos en punto') o distancias en pasos. Evita decir 'aquí' o 'allí'.
                  </div>
                </li>
              </ul>
            </div>
          </div>
        </section>

        {/* Sección: Manual Operativo por Puesto de Trabajo */}
        <section style={{ marginBottom: "60px", background: "#f8fafc", padding: "36px", borderRadius: "24px", border: "1px solid #e2e8f0" }}>
          <div style={{ textAlign: "center", marginBottom: "28px" }}>
            <span style={{ fontSize: "12px", fontWeight: 800, color: "#047857", textTransform: "uppercase", letterSpacing: "0.08em" }}>
              Procedimientos Operativos
            </span>
            <h2 style={{ fontSize: "24px", fontWeight: 800, color: "#0f172a", margin: "6px 0 0" }}>
              Instructivos Específicos por Área de Desempeño
            </h2>
          </div>

          <div style={{ display: "flex", justifyContent: "center", gap: "10px", flexWrap: "wrap", marginBottom: "28px" }}>
            {[
              { id: "recepcion", label: "📞 Recepción y Citas" },
              { id: "oftalmologia", label: "👁️ Médicos Oftalmólogos" },
              { id: "tacto", label: "💆 Terapeutas TACTO" },
              { id: "instructores", label: "🦯 Instructores de Movilidad" },
              { id: "caja", label: "💵 Finanzas y Caja" },
            ].map((p) => (
              <button
                key={p.id}
                type="button"
                onClick={() => setPuestoSeleccionado(p.id)}
                style={{
                  padding: "10px 18px",
                  borderRadius: "12px",
                  border: puestoSeleccionado === p.id ? "1px solid #034ea2" : "1px solid #cbd5e1",
                  background: puestoSeleccionado === p.id ? "#034ea2" : "#ffffff",
                  color: puestoSeleccionado === p.id ? "#ffffff" : "#334155",
                  fontWeight: 700,
                  fontSize: "13px",
                  cursor: "pointer",
                  transition: "all 0.15s ease",
                }}
              >
                {p.label}
              </button>
            ))}
          </div>

          <div style={{ background: "white", padding: "28px", borderRadius: "16px", border: "1px solid #e2e8f0" }}>
            {puestoSeleccionado === "recepcion" && (
              <div>
                <h3 style={{ color: "#034ea2", marginBottom: "12px" }}>Puesto: Recepción y Atención al Paciente</h3>
                <p style={{ fontSize: "14px", color: "#475569", lineHeight: "1.6" }}>
                  La recepción es la primera cara del Patronato. Debes registrar diligentemente cada paciente en el módulo de Beneficiarios, agendar sus citas respetando el cupo por especialista y canalizar las órdenes de insumos generadas para derivar al paciente a la ventanilla de Caja.
                </p>
                <div style={{ background: "#eff6ff", padding: "14px", borderRadius: "10px", borderLeft: "4px solid #3b82f6", marginTop: "16px", fontSize: "13px" }}>
                  <strong>Consejo clave:</strong> Siempre pregunta el número telefónico actualizado del acompañante y recuerda confirmar la cita 24 horas antes por llamada o WhatsApp institucional.
                </div>
              </div>
            )}

            {puestoSeleccionado === "oftalmologia" && (
              <div>
                <h3 style={{ color: "#034ea2", marginBottom: "12px" }}>Puesto: Médicos Oftalmólogos y Optometristas</h3>
                <p style={{ fontSize: "14px", color: "#475569", lineHeight: "1.6" }}>
                  Responsables de la valoración clínica, diagnóstico oportuno de afecciones oculares (glaucoma, retinopatía diabética, catarata) y derivación temprana a los programas de rehabilitación funcional del Patronato antes de que la pérdida visual sea irreversible.
                </p>
                <div style={{ background: "#ecfdf5", padding: "14px", borderRadius: "10px", borderLeft: "4px solid #10b981", marginTop: "16px", fontSize: "13px" }}>
                  <strong>Consejo clave:</strong> Registra de inmediato la condición visual según la escala de la OMS (Ceguera Total, Baja Visión Moderada o Severa) en la ficha clínica del sistema.
                </div>
              </div>
            )}

            {puestoSeleccionado === "tacto" && (
              <div>
                <h3 style={{ color: "#034ea2", marginBottom: "12px" }}>Puesto: Terapeutas y Masajistas Escuela TACTO</h3>
                <p style={{ fontSize: "14px", color: "#475569", lineHeight: "1.6" }}>
                  Profesionales no videntes certificados encargados de brindar bienestar físico y relajación a clientes particulares y corporativos, demostrando la excelencia técnica y destreza táctil en cada sesión.
                </p>
                <div style={{ background: "#faf5ff", padding: "14px", borderRadius: "10px", borderLeft: "4px solid #a855f7", marginTop: "16px", fontSize: "13px" }}>
                  <strong>Consejo clave:</strong> Mantener siempre la cabina impecable, sábanas desechables renovadas y verificar con el cliente sus antecedentes musculares antes de iniciar la sesión.
                </div>
              </div>
            )}

            {puestoSeleccionado === "instructores" && (
              <div>
                <h3 style={{ color: "#034ea2", marginBottom: "12px" }}>Puesto: Instructores de Movilidad y Tiflotecnología</h3>
                <p style={{ fontSize: "14px", color: "#475569", lineHeight: "1.6" }}>
                  Guías formativos en el uso del bastón blanco, lectoescritura Braille y software lector de pantalla. Fomentan la seguridad psicológica y la independencia de movimiento de cada alumno.
                </p>
                <div style={{ background: "#fefce8", padding: "14px", borderRadius: "10px", borderLeft: "4px solid #eab308", marginTop: "16px", fontSize: "13px" }}>
                  <strong>Consejo clave:</strong> Celebrar cada logro de autonomía y realizar prácticas en rutas reales de Santo Domingo o Santiago.
                </div>
              </div>
            )}

            {puestoSeleccionado === "caja" && (
              <div>
                <h3 style={{ color: "#034ea2", marginBottom: "12px" }}>Puesto: Cajeros y Personal de Cobro</h3>
                <p style={{ fontSize: "14px", color: "#475569", lineHeight: "1.6" }}>
                  Responsables de la apertura de turno, cobro de cuotas de recuperación de consultas, servicios TACTO y venta de suapers e insumos ecológicos a través del sistema POS dedicado.
                </p>
                <div style={{ background: "#fef2f2", padding: "14px", borderRadius: "10px", borderLeft: "4px solid #ef4444", marginTop: "16px", fontSize: "13px" }}>
                  <strong>Consejo clave:</strong> Todo ingreso debe contar con su comprobante oficial emitido y cuadre diario de turno registrado antes de entregar el efectivo a custodia.
                </div>
              </div>
            )}
          </div>
        </section>

        {/* Sección: FAQ Interno del Personal */}
        <section className="faq-section" style={{ marginBottom: "40px" }}>
          <div className="faq-title-wrap">
            <span style={{ fontSize: "12px", fontWeight: 800, color: "#034ea2", textTransform: "uppercase", letterSpacing: "0.08em" }}>
              Soporte a Colaboradores
            </span>
            <h2 style={{ fontSize: "26px", fontWeight: 800, color: "#0f172a", margin: "6px 0 0" }}>
              Preguntas Frecuentes del Personal de Nuevo Ingreso
            </h2>
          </div>

          <div>
            {FAQS_PERSONAL.map((faq) => {
              const abierto = faqAbierto === faq.id;
              return (
                <div key={faq.id} className="faq-item">
                  <button
                    type="button"
                    className="faq-question-btn"
                    onClick={() => setFaqAbierto(abierto ? null : faq.id)}
                    aria-expanded={abierto}
                  >
                    <span>{faq.pregunta}</span>
                    <span style={{ fontSize: "20px", color: "#034ea2", marginLeft: "12px" }}>
                      {abierto ? "−" : "+"}
                    </span>
                  </button>
                  {abierto && (
                    <div className="faq-answer-box">
                      <p style={{ margin: 0 }}>{faq.respuesta}</p>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </section>

        {/* Directorio de Contacto Interno */}
        <div style={{ background: "linear-gradient(135deg, #034ea2 0%, #1e293b 100%)", borderRadius: "20px", padding: "32px", color: "white" }}>
          <h3 style={{ fontSize: "20px", fontWeight: 800, margin: "0 0 16px" }}>
            📞 Directorio Interno y Mesa de Ayuda
          </h3>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))", gap: "20px", fontSize: "13px" }}>
            <div>
              <strong style={{ color: "#38bdf8", display: "block" }}>Soporte Técnico TI & Software:</strong>
              <span>Extensión: 204</span>
              <span style={{ display: "block", color: "#94a3b8" }}>soporte@patronatociegos.org.do</span>
            </div>
            <div>
              <strong style={{ color: "#38bdf8", display: "block" }}>Gestión Humana & RRHH:</strong>
              <span>Extensión: 201</span>
              <span style={{ display: "block", color: "#94a3b8" }}>rrhh@patronatociegos.org.do</span>
            </div>
            <div>
              <strong style={{ color: "#38bdf8", display: "block" }}>Coordinación Médica y Social:</strong>
              <span>Extensión: 208</span>
              <span style={{ display: "block", color: "#94a3b8" }}>medico@patronatociegos.org.do</span>
            </div>
          </div>
          <div style={{ marginTop: "24px", paddingTop: "16px", borderTop: "1px solid rgba(255,255,255,0.15)", display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: "10px" }}>
            <span style={{ fontSize: "12px", color: "#cbd5e1" }}>
              ¿Listo para iniciar tu jornada laboral en el sistema?
            </span>
            <Link to="/login" className="btn btn-pnc-green" style={{ padding: "8px 20px" }}>
              Ingresar al Portal Interno →
            </Link>
          </div>
        </div>
      </main>

      <PublicFooter />
      <FloatingHelpButton />
    </div>
  );
}
