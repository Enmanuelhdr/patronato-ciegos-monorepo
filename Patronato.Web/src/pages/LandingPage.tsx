import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "../context/AuthContext";
import { getBeneficiarios, getSucursales } from "../api/endpoints";

const PROGRAMAS = [
  {
    icono: "💆",
    titulo: "Masajes TACTO",
    texto: "Terapias de masaje realizadas por terapeutas invidentes capacitados, demostrando su independencia e inserción laboral.",
  },
  {
    icono: "💳",
    titulo: "Préstamos de Emprendimiento",
    texto: "Microcréditos para que beneficiarios invidentes inicien o fortalezcan colmados, paleteras, artesanías y otros negocios propios.",
  },
  {
    icono: "📚",
    titulo: "Rehabilitación Integral",
    texto: "Talleres de Braille, orientación y movilidad con bastón, y oficios técnicos para la independencia personal y la inserción laboral.",
  },
  {
    icono: "👁️",
    titulo: "Prevención de Ceguera",
    texto: "Consultas oftalmológicas con especialistas para detectar y tratar a tiempo condiciones visuales en la comunidad.",
  },
  {
    icono: "🎁",
    titulo: "Donaciones",
    texto: "Aportes de empresas y personas solidarias que sostienen los programas institucionales y los insumos de los beneficiarios.",
  },
  {
    icono: "🤝",
    titulo: "Voluntariado",
    texto: "Personas solidarias que apoyan en lectura y acompañamiento, docencia Braille y logística de eventos institucionales.",
  },
];

export function LandingPage() {
  const { sesion } = useAuth();
  const beneficiarios = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const sucursales = useQuery({ queryKey: ["sucursales"], queryFn: getSucursales });

  return (
    <div className="landing">
      <header className="landing-nav">
        <div className="landing-nav-marca">
          <div className="sidebar-escudo">PNC</div>
          <span>Patronato Nacional de Ciegos</span>
        </div>
        <Link to={sesion ? "/panel" : "/login"} className="btn btn-primario">
          {sesion ? "Ir al Portal" : "Iniciar sesión"}
        </Link>
      </header>

      <section className="landing-hero">
        <div>
          <span className="landing-hero-tag">República Dominicana · desde la Zona Universitaria</span>
          <h1>La ceguera no te impide ver con el corazón</h1>
          <p>
            Trabajamos junto a personas ciegas y de baja visión en todo el país para que vivan con independencia:
            rehabilitación, emprendimiento, salud visual preventiva y acompañamiento comunitario.
          </p>
          <div className="landing-hero-acciones">
            <a href="#programas" className="btn btn-primario">Conoce nuestros programas</a>
            <a href="#contacto" className="btn btn-secundario">Cómo contactarnos</a>
          </div>
        </div>

        <div className="landing-hero-stats">
          <div className="stat-card stat-azul">
            <span className="stat-etiqueta">BENEFICIARIOS ACOMPAÑADOS</span>
            <span className="stat-valor">{beneficiarios.data ? beneficiarios.data.length : "—"}</span>
          </div>
          <div className="stat-card stat-verde">
            <span className="stat-etiqueta">SEDES A NIVEL NACIONAL</span>
            <span className="stat-valor">{sucursales.data ? sucursales.data.length : "—"}</span>
          </div>
          <div className="stat-card stat-morado">
            <span className="stat-etiqueta">PROGRAMAS ACTIVOS</span>
            <span className="stat-valor">6</span>
          </div>
        </div>
      </section>

      <section id="programas" className="landing-seccion">
        <h2>Nuestros programas</h2>
        <p className="landing-seccion-sub">Así es como acompañamos a cada beneficiario, de principio a fin.</p>
        <div className="landing-grid">
          {PROGRAMAS.map((p) => (
            <div key={p.titulo} className="landing-card">
              <div className="landing-card-icono">{p.icono}</div>
              <h3>{p.titulo}</h3>
              <p>{p.texto}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="landing-seccion landing-seccion-alt">
        <h2>Nuestras sedes</h2>
        <p className="landing-seccion-sub">Presencia en todo el territorio nacional.</p>
        <div className="landing-grid">
          {(sucursales.data ?? []).map((s) => (
            <div key={s.id} className="landing-card">
              <h3>🏢 {s.nombre}</h3>
              <p>{s.direccion}</p>
              <p>📞 {s.telefono}</p>
              <p>Encargado: {s.encargado}</p>
            </div>
          ))}
          {sucursales.isLoading && <p>Cargando sedes...</p>}
        </div>
      </section>

      <section id="contacto" className="landing-cta">
        <h2>¿Quieres apoyar o necesitas nuestros servicios?</h2>
        <p>Visítanos en cualquiera de nuestras sedes o comunícate con nosotros. Con gusto te orientamos.</p>
        <div className="landing-contacto-datos">
          <span>📍 Calle Huáscar Tejeda #54, Zona Universitaria, Santo Domingo, D.N.</span>
          <span>📞 (809) 533-2833</span>
        </div>
      </section>

      <footer className="landing-footer">
        <div>
          <strong>Patronato Nacional de Ciegos, Inc.</strong>
          <span>RNC: 4-01-00623-1</span>
        </div>
        <Link to="/login" className="landing-footer-link">Portal del personal →</Link>
      </footer>
    </div>
  );
}
