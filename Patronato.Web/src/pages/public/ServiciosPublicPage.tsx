import { Link } from "react-router-dom";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import { FloatingHelpButton } from "../../components/FloatingHelpButton";

const SERVICIOS_DETALLE = [
  {
    id: "oftalmologia",
    icono: "👁️",
    categoria: "Salud y Diagnóstico Ocular",
    titulo: "Prevención de Ceguera y Consultas Oftalmológicas",
    descripcion:
      "Servicios médicos especializados orientados a la detección temprana, diagnóstico oportuno y tratamiento preventivo de patologías visuales.",
    beneficios: [
      "Evaluación completa de agudeza visual y refracción.",
      "Tonometría y fondo de ojo para detección temprana de glaucoma.",
      "Tamizaje de catarata senil y congénita.",
      "Evaluación de retinopatía diabética e hipertensiva.",
      "Consultas a tarifas altamente subsidiadas para familias de escasos recursos.",
    ],
    horario: "Lunes a Viernes: 8:00 AM - 3:00 PM",
    requisitos: "Cédula o acta de nacimiento, registro previo en recepción o cita en línea.",
    accionTexto: "Solicitar Consulta Oftalmológica",
    accionUrl: "/agendar-cita?servicio=3",
    color: "#0284c7",
    colorBg: "#f0f9ff",
  },
  {
    id: "rehabilitacion",
    icono: "🦯",
    categoria: "Autonomía e Integración",
    titulo: "Rehabilitación Funcional Integral",
    descripcion:
      "Programa integral diseñado para que la persona con ceguera o baja visión adquiera las competencias prácticas, emocionales y de movilidad para desenvolverse con absoluta independencia.",
    beneficios: [
      "Sistema Braille: lectura y escritura inicial y avanzada con regleta y punzón.",
      "Orientación y Movilidad: técnicas de bastón blanco en entornos urbanos y transporte.",
      "Habilidades de Vida Diaria (AVD): autonomía en el hogar, cocina segura y autocuidado.",
      "Informática Adaptada: manejo de computadoras y teléfonos inteligentes con lectores de pantalla (NVDA, TalkBack, VoiceOver).",
      "Apoyo psicológico individualizado y grupal.",
    ],
    horario: "Lunes a Jueves: 8:30 AM - 2:30 PM",
    requisitos: "Evaluación inicial por el departamento de trabajo social del Patronato.",
    accionTexto: "Conocer Programa de Rehabilitación",
    accionUrl: "/agendar-cita?servicio=4",
    color: "#059669",
    colorBg: "#ecfdf5",
  },
  {
    id: "masajes-tacto",
    icono: "💆",
    categoria: "Empleo Inclusivo y Salud",
    titulo: "Centro de Masajes TACTO",
    descripcion:
      "Iniciativa emblemática de inserción laboral digna donde terapeutas invidentes capacitados internacionalmente ofrecen terapias corporales de relajación y bienestar.",
    beneficios: [
      "Masaje relajante antiestrés de cuerpo completo.",
      "Terapia descontracturante y alivio de tensión muscular de espalda y cuello.",
      "Reflexología podal y drenaje linfático asistido.",
      "Cabinas privadas climatizadas con aromaterapia y música relajante.",
      "Cada sesión contratada apoya directamente el sustento económico del terapeuta.",
    ],
    horario: "Lunes a Viernes: 9:00 AM - 5:00 PM (Previa Cita)",
    requisitos: "Disponible para todo público. Reserva telefónica o en línea.",
    accionTexto: "Reservar Sesión TACTO",
    accionUrl: "/agendar-cita?servicio=3",
    color: "#d97706",
    colorBg: "#fffbeb",
  },
  {
    id: "talleres-eco",
    icono: "🌱",
    categoria: "Economía Circular y Sostenibilidad",
    titulo: "Talleres Ecológicos y Producción de Trapeadores",
    descripcion:
      "Modelo pionero en República Dominicana de economía circular inclusiva. Transformamos desperdicios textiles en productos de aseo de alta calidad fabricados por personas ciegas.",
    beneficios: [
      "Elaboración de trapeadores ecológicos con hilazas 100% de algodón reciclado.",
      "Línea de desinfectantes biodegradables ecológicos sin químicos corrosivos.",
      "Talleres de tejido en macramé y fibras naturales para decoración.",
      "Certificación técnica ocupacional para los participantes.",
      "Alianzas con empresas para compras corporativas con impacto social medible.",
    ],
    horario: "Lunes a Viernes: 8:00 AM - 4:00 PM",
    requisitos: "Inscripción en programas de capacitación ocupacional del Patronato.",
    accionTexto: "Ver Catálogo de Productos Ecológicos",
    accionUrl: "/productos",
    color: "#10b981",
    colorBg: "#f0fdf4",
  },
  {
    id: "microcreditos",
    icono: "💳",
    categoria: "Autosuficiencia Financiera",
    titulo: "Microcréditos y Fondo Rotatorio de Emprendimiento",
    descripcion:
      "Apoyo financiero solidario sin intermediación usurera para que personas con discapacidad visual graduadas de nuestros programas inicien o fortalezcan sus microempresas.",
    beneficios: [
      "Financiamiento a tasas solidarias para pequeños negocios familiares.",
      "Asesoría técnica en comercialización, administración y manejo de inventario.",
      "Casos de éxito en colmados, cafeterías, fabricación de productos y artesanías.",
      "Esquema de cuotas flexibles adaptadas al flujo de ingresos del emprendedor.",
    ],
    horario: "Atención por cita con el Comité de Crédito",
    requisitos: "Haber cursado programas de rehabilitación o capacitación en el Patronato.",
    accionTexto: "Consultar Requisitos de Préstamo",
    accionUrl: "/agendar-cita",
    color: "#7c3aed",
    colorBg: "#faf5ff",
  },
  {
    id: "familia",
    icono: "🤝",
    categoria: "Comunidad y Trabajo Social",
    titulo: "Acompañamiento a la Familia y Trabajo Social",
    descripcion:
      "Orientación integral a los núcleos familiares de personas que pierden la visión repentinamente o nacen con discapacidad visual, promoviendo la inclusión sin sobreprotección.",
    beneficios: [
      "Escuela para familias y talleres de sensibilización sobre autonomía.",
      "Visitas domiciliarias de trabajadoras sociales a nivel nacional.",
      "Asistencia en gestión de ayudas técnicas (bastones, regletas, lupas).",
      "Red de apoyo emocional entre padres, tutores y parejas de beneficiarios.",
    ],
    horario: "Lunes a Viernes: 8:00 AM - 3:00 PM",
    requisitos: "Abierto a familias de beneficiarios activos y de nuevo ingreso.",
    accionTexto: "Contactar a Trabajo Social",
    accionUrl: "/sedes",
    color: "#e11d48",
    colorBg: "#fff1f2",
  },
];

export function ServiciosPublicPage() {
  return (
    <div className="landing-moderna">
      <PublicNavbar paginaActiva="servicios" />

      {/* Hero de Servicios */}
      <section className="pagina-publica-hero">
        <div className="pagina-publica-hero-inner">
          <div className="breadcrumb-nav">
            <Link to="/">Inicio</Link>
            <span>/</span>
            <span className="breadcrumb-actual">Servicios Institucionales</span>
          </div>
          <span className="hero-pill-badge">🏛️ Más de 60 Años al Servicio del País</span>
          <h1 className="pagina-hero-titulo">
            Servicios Integrales para la <span className="texto-destacado-verde">Autonomía y Salud Visual</span>
          </h1>
          <p className="pagina-hero-subtitulo">
            Brindamos atención médica oftalmológica especializada, programas de rehabilitación funcional con bastón y Braille,
            empleo digno en el Centro de Masajes TACTO y producción sostenible con impacto social en toda la República Dominicana.
          </p>

          <div className="hero-quick-actions">
            <Link to="/agendar-cita" className="btn btn-pnc-green">
              <span>📅 Agendar Cita en Línea</span>
            </Link>
            <Link to="/simulador" className="btn btn-pnc-outline-white">
              <span>👁️ Probar Simulador de Empatía</span>
            </Link>
          </div>
        </div>
      </section>

      {/* Listado de Servicios con Efectos de Luz y Tarjetas Interactivas */}
      <section className="seccion-servicios-completa">
        <div className="servicios-container">
          <div className="servicios-grid-destacado">
            {SERVICIOS_DETALLE.map((s) => (
              <article key={s.id} className="tarjeta-servicio-card">
                <div className="servicio-card-header">
                  <div
                    className="servicio-icono-circulo"
                    style={{ backgroundColor: s.colorBg, color: s.color, borderColor: s.color }}
                  >
                    <span aria-hidden="true">{s.icono}</span>
                  </div>
                  <div>
                    <span className="servicio-categoria-tag" style={{ color: s.color }}>
                      {s.categoria}
                    </span>
                    <h2 className="servicio-titulo">{s.titulo}</h2>
                  </div>
                </div>

                <p className="servicio-descripcion">{s.descripcion}</p>

                <div className="servicio-beneficios-box">
                  <strong className="beneficios-titulo">¿Qué incluye este servicio?</strong>
                  <ul className="beneficios-lista">
                    {s.beneficios.map((b, idx) => (
                      <li key={idx}>
                        <span className="check-bullet" style={{ color: s.color }}>✓</span>
                        <span>{b}</span>
                      </li>
                    ))}
                  </ul>
                </div>

                <div className="servicio-meta-info">
                  <div className="meta-item">
                    <span className="meta-etiqueta">🕒 Horario:</span>
                    <span className="meta-valor">{s.horario}</span>
                  </div>
                  <div className="meta-item">
                    <span className="meta-etiqueta">📋 Acceso:</span>
                    <span className="meta-valor">{s.requisitos}</span>
                  </div>
                </div>

                <div className="servicio-card-footer">
                  <Link
                    to={s.accionUrl}
                    className="btn btn-block btn-servicio-accion"
                    style={{ backgroundColor: s.color }}
                  >
                    <span>{s.accionTexto}</span>
                    <span aria-hidden="true"> →</span>
                  </Link>
                </div>
              </article>
            ))}
          </div>

          {/* Banner de Consulta de Citas */}
          <div className="banner-cta-institucional">
            <div className="banner-cta-textos">
              <h3>¿Ya tienes una cita agendada o solicitud previa?</h3>
              <p>Puedes consultar el estado de tu turno en tiempo real ingresando tu número telefónico o código de solicitud.</p>
            </div>
            <Link to="/agendar-cita?tab=consultar" className="btn btn-pnc-primary">
              <span>🔍 Consultar Estado de Solicitud</span>
            </Link>
          </div>
        </div>
      </section>

      <PublicFooter />
      <FloatingHelpButton />
    </div>
  );
}
