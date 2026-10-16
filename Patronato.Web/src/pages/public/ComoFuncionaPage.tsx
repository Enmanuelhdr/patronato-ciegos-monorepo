import { useState } from "react";
import { Link } from "react-router-dom";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import { FloatingHelpButton } from "../../components/FloatingHelpButton";

interface FaqItem {
  id: string;
  categoria: string;
  pregunta: string;
  respuesta: string;
}

const FAQS_DATOS: FaqItem[] = [
  {
    id: "costos",
    categoria: "Servicios Médicos",
    pregunta: "¿Las consultas oftalmológicas y terapias tienen algún costo?",
    respuesta:
      "El Patronato es una institución sin fines de lucro. Ofrecemos cuotas de recuperación sumamente simbólicas para el público general, y exoneración total del 100% mediante evaluación socioeconómica para personas y familias de escasos recursos. Ningún dominicano deja de recibir atención por motivos económicos.",
  },
  {
    id: "quien-puede-ir",
    categoria: "Atención General",
    pregunta: "¿Quiénes pueden recibir asistencia en el Patronato?",
    respuesta:
      "Cualquier persona con discapacidad visual (ceguera total o baja visión irreversible), así como cualquier ciudadano que desee chequear su salud visual preventiva (tamizaje de cataratas, glaucoma o errores refractivos). También recibimos niños desde recién nacidos en nuestro programa de estimulación temprana.",
  },
  {
    id: "tacto-empresas",
    categoria: "Masajes TACTO",
    pregunta: "¿Cómo puedo contratar masajes TACTO para mi empresa o evento corporativo?",
    respuesta:
      "Nuestros terapeutas no videntes certificados se trasladan a tu empresa con sillas ergonómicas especiales para jornadas de bienestar corporativo ('Día de Spa Laboral' de 15 o 20 min por colaborador), o puedes acudir a nuestras cabinas en Santo Domingo y Santiago. Contáctanos por WhatsApp al (809) 508-7511 para cotizaciones a la medida.",
  },
  {
    id: "comprar-suapers",
    categoria: "Taller Protegido",
    pregunta: "¿Cómo se adquieren los trapeadores y suapers ecológicos?",
    respuesta:
      "Puedes comprarlos al por mayor o detalle directamente en nuestra sede central, sucursales regionales, o solicitarlos a través de nuestra sección de Artesanías y Productos. Suministramos a hospitales, colegios, bancos y empresas de limpieza en toda la República Dominicana.",
  },
  {
    id: "donaciones",
    categoria: "Donaciones y Voluntariado",
    pregunta: "¿Cómo puedo donar o colaborar como voluntario?",
    respuesta:
      "Aceptamos donaciones económicas deducibles de impuestos según la Ley 122-05, donaciones de insumos oftalmológicos, papel Braille, o voluntariado profesional (médicos, docentes, lectores voluntarios). Puedes registrarte en línea o contactarnos a administracion@patronatociegos.org.do.",
  },
  {
    id: "sedes-horarios",
    categoria: "Ubicación",
    pregunta: "¿Cuáles son los horarios y dónde están ubicados?",
    respuesta:
      "Operamos de lunes a viernes de 8:00 AM a 4:00 PM. Nuestra sede principal está en Santo Domingo (Zona Universitaria, calle Huáscar Tejeda #54), y contamos con filiales regionales en Santiago de los Caballeros (Villa Olga) y Barahona (Calle Jaime Mota #12).",
  },
];

export function ComoFuncionaPage() {
  const [faqAbierto, setFaqAbierto] = useState<string | null>("costos");
  const [categoriaFiltro, setCategoriaFiltro] = useState<string>("todas");

  const categorias = ["todas", "Servicios Médicos", "Atención General", "Masajes TACTO", "Taller Protegido", "Donaciones y Voluntariado"];

  const faqsFiltradas = categoriaFiltro === "todas"
    ? FAQS_DATOS
    : FAQS_DATOS.filter((f) => f.categoria === categoriaFiltro);

  return (
    <div style={{ minHeight: "100vh", display: "flex", flexDirection: "column" }}>
      <PublicNavbar />

      <main className="cf-container" style={{ flexGrow: 1 }}>
        {/* Hero Section */}
        <div className="cf-hero">
          <span
            style={{
              display: "inline-block",
              background: "rgba(56, 189, 248, 0.2)",
              border: "1px solid rgba(56, 189, 248, 0.4)",
              color: "#38bdf8",
              fontSize: "12px",
              fontWeight: 800,
              textTransform: "uppercase",
              letterSpacing: "0.08em",
              padding: "6px 16px",
              borderRadius: "9999px",
              marginBottom: "16px",
            }}
          >
            Guía Paso a Paso · Transparencia PNC
          </span>
          <h1 style={{ fontSize: "36px", fontWeight: 800, marginBottom: "16px" }}>
            ¿Cómo Funciona el Patronato Nacional de Ciegos?
          </h1>
          <p style={{ maxWidth: "700px", margin: "0 auto", fontSize: "16px", color: "#cbd5e1", lineHeight: "1.6" }}>
            Acompañamos a las personas con discapacidad visual y sus familias en cada etapa de su vida,
            desde la prevención oftalmológica hasta la plena autonomía e inserción productiva.
          </p>
        </div>

        {/* 4 Pasos del Proceso */}
        <h2 style={{ fontSize: "24px", fontWeight: 800, color: "#0f172a", marginBottom: "24px", textAlign: "center" }}>
          La Ruta de la Autonomía: 4 Pasos Hacia la Inclusión
        </h2>

        <div className="cf-steps-grid">
          {/* Paso 1 */}
          <div className="cf-step-card">
            <div className="cf-step-number">1</div>
            <h3 className="cf-step-title">Evaluación Oftalmológica</h3>
            <p className="cf-step-desc">
              El proceso comienza con un examen visual completo realizado por nuestros oftalmólogos
              para determinar el remanente visual, prescribir ayudas ópticas (filtros, lupas) o canalizar tratamientos quirúrgicos.
            </p>
            <div className="cf-step-highlight">
              📍 Tamizaje y diagnóstico de ceguera evitable.
            </div>
          </div>

          {/* Paso 2 */}
          <div className="cf-step-card">
            <div className="cf-step-number">2</div>
            <h3 className="cf-step-title">Acompañamiento Social</h3>
            <p className="cf-step-desc">
              Nuestro equipo de Trabajo Social y Psicología evalúa el entorno familiar, brinda apoyo emocional para la aceptación del diagnóstico y diseña un plan de vida personalizado.
            </p>
            <div className="cf-step-highlight">
              🤝 Apoyo integral para el paciente y sus familiares.
            </div>
          </div>

          {/* Paso 3 */}
          <div className="cf-step-card">
            <div className="cf-step-number">3</div>
            <h3 className="cf-step-title">Rehabilitación Funcional</h3>
            <p className="cf-step-desc">
              Entrenamiento intensivo en Orientación y Movilidad (uso del bastón blanco), lectoescritura Braille,
              computación adaptada (lectores de pantalla NVDA) y habilidades de vida diaria (cocina, aseo, compras).
            </p>
            <div className="cf-step-highlight">
              🦯 De la dependencia al desplazamiento autónomo.
            </div>
          </div>

          {/* Paso 4 */}
          <div className="cf-step-card">
            <div className="cf-step-number">4</div>
            <h3 className="cf-step-title">Inserción y Emprendimiento</h3>
            <p className="cf-step-desc">
              Graduación e integración socio-laboral: formación en la Escuela de Masajes TACTO, empleo en el taller protegido de suapers ecológicos, o acceso a microcréditos para negocios propios.
            </p>
            <div className="cf-step-highlight">
              💼 Empleo digno y autosuficiencia económica.
            </div>
          </div>
        </div>

        {/* Sección de FAQ Interactivo */}
        <div className="faq-section">
          <div className="faq-title-wrap">
            <span
              style={{
                fontSize: "12px",
                fontWeight: 800,
                color: "#034ea2",
                textTransform: "uppercase",
                letterSpacing: "0.08em",
                display: "block",
                marginBottom: "8px",
              }}
            >
              Resolvemos tus inquietudes
            </span>
            <h2 style={{ fontSize: "28px", fontWeight: 800, color: "#0f172a", margin: 0 }}>
              Preguntas Frecuentes
            </h2>
          </div>

          {/* Filtros de Categorías */}
          <div
            style={{
              display: "flex",
              justifyContent: "center",
              gap: "8px",
              flexWrap: "wrap",
              marginBottom: "32px",
            }}
          >
            {categorias.map((cat) => (
              <button
                key={cat}
                type="button"
                onClick={() => setCategoriaFiltro(cat)}
                style={{
                  padding: "8px 16px",
                  borderRadius: "9999px",
                  border: categoriaFiltro === cat ? "1px solid #034ea2" : "1px solid #e2e8f0",
                  background: categoriaFiltro === cat ? "#034ea2" : "#f8fafc",
                  color: categoriaFiltro === cat ? "#ffffff" : "#475569",
                  fontSize: "13px",
                  fontWeight: 600,
                  cursor: "pointer",
                  transition: "all 0.15s ease",
                }}
              >
                {cat === "todas" ? "Todas las preguntas" : cat}
              </button>
            ))}
          </div>

          {/* Lista de Acordeón */}
          <div>
            {faqsFiltradas.map((faq) => {
              const estaAbierto = faqAbierto === faq.id;
              return (
                <div key={faq.id} className="faq-item">
                  <button
                    type="button"
                    className="faq-question-btn"
                    onClick={() => setFaqAbierto(estaAbierto ? null : faq.id)}
                    aria-expanded={estaAbierto}
                  >
                    <span>{faq.pregunta}</span>
                    <span style={{ fontSize: "20px", color: "#034ea2", marginLeft: "12px" }}>
                      {estaAbierto ? "−" : "+"}
                    </span>
                  </button>
                  {estaAbierto && (
                    <div className="faq-answer-box">
                      <p style={{ margin: 0 }}>{faq.respuesta}</p>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        {/* Llamado a la Acción Inferior */}
        <div
          style={{
            marginTop: "60px",
            background: "linear-gradient(135deg, #059669 0%, #047857 100%)",
            borderRadius: "20px",
            padding: "36px 30px",
            color: "white",
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: "20px",
            boxShadow: "0 15px 30px -10px rgba(5, 150, 105, 0.4)",
          }}
        >
          <div>
            <h3 style={{ fontSize: "22px", fontWeight: 800, margin: "0 0 6px" }}>
              ¿Listo para dar el primer paso o necesitas una cita?
            </h3>
            <p style={{ margin: 0, fontSize: "14px", color: "#d1fae5" }}>
              Agenda tu consulta oftalmológica o comunícate directamente con nuestro equipo de trabajo social.
            </p>
          </div>
          <div style={{ display: "flex", gap: "12px", flexWrap: "wrap" }}>
            <Link
              to="/agendar-cita"
              className="btn"
              style={{ background: "#ffffff", color: "#047857", fontWeight: 800 }}
            >
              📅 Agendar Cita en Línea
            </Link>
            <Link
              to="/verificar"
              className="btn"
              style={{ background: "rgba(255, 255, 255, 0.2)", color: "#ffffff", border: "1px solid white" }}
            >
              🔍 Consultar Mi Cita
            </Link>
          </div>
        </div>
      </main>

      <PublicFooter />
      <FloatingHelpButton />
    </div>
  );
}
