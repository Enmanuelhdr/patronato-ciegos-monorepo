import { useState } from "react";
import { Link, NavLink } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

interface PublicNavbarProps {
  paginaActiva?: "inicio" | "servicios" | "simulador" | "impacto" | "productos" | "sedes" | "citas" | "como-funciona" | "verificar" | "induccion";
}

export function PublicNavbar({ paginaActiva }: PublicNavbarProps) {
  const { sesion } = useAuth();
  const [menuAbierto, setMenuAbierto] = useState(false);

  return (
    <>
      {/* 1. Barra Superior Institucional */}
      <div className="landing-topbar-institucional">
        <div className="landing-topbar-inner">
          <div className="topbar-contacto-info">
            <span>📍 Av. Dr. Bernardo Correa y Cidrón Esq. Huáscar Tejeda, Zona Universitaria</span>
            <span className="separador-punto">•</span>
            <span>📞 Santo Domingo: (809) 508-7511 / (809) 533-2833</span>
            <span className="separador-punto">•</span>
            <span>📞 Santiago: (809) 575-9735</span>
          </div>
          <div className="topbar-redes">
            <span className="topbar-rnc">RNC: 4-01-00623-1</span>
            <span className="topbar-tag-eco">🌱 Gestión Sostenible & Eco-Friendly</span>
          </div>
        </div>
      </div>

      {/* 2. Barra de Navegación Principal con Logo Oficial */}
      <header className="landing-header">
        <div className="landing-header-inner">
          <Link to="/" className="landing-marca-link" title="Inicio - Patronato Nacional de Ciegos">
            <div className="landing-logo-container">
              <img
                src="/logo-pnc.png"
                alt="Logo Oficial Patronato Nacional de Ciegos"
                className="logo-oficial-img"
              />
            </div>
            <div className="landing-marca-textos">
              <span className="marca-titulo">Patronato Nacional de Ciegos</span>
              <span className="marca-slogan">República Dominicana · Fundado en 1964</span>
            </div>
          </Link>

          <button
            type="button"
            className="btn-hamburguesa-movil"
            onClick={() => setMenuAbierto(!menuAbierto)}
            aria-label={menuAbierto ? "Cerrar menú" : "Abrir menú de navegación"}
          >
            {menuAbierto ? "✕" : "☰"}
          </button>

          <nav className={`landing-nav-links ${menuAbierto ? "abierto" : ""}`}>
            <NavLink
              to="/"
              className={({ isActive }) =>
                `nav-enlace ${isActive && paginaActiva === "inicio" ? "activo" : ""}`
              }
            >
              Inicio
            </NavLink>
            <NavLink
              to="/como-funciona"
              className={({ isActive }) =>
                `nav-enlace ${isActive || paginaActiva === "como-funciona" ? "activo" : ""}`
              }
            >
              ✨ ¿Cómo Funciona?
            </NavLink>
            <NavLink
              to="/verificar"
              className={({ isActive }) =>
                `nav-enlace ${isActive || paginaActiva === "verificar" ? "activo" : ""}`
              }
            >
              🔍 Verificar
            </NavLink>
            <NavLink
              to="/induccion"
              className={({ isActive }) =>
                `nav-enlace ${isActive || paginaActiva === "induccion" ? "activo" : ""}`
              }
            >
              🤝 Inducción
            </NavLink>
            <NavLink
              to="/servicios"
              className={({ isActive }) =>
                `nav-enlace ${isActive || paginaActiva === "servicios" ? "activo" : ""}`
              }
            >
              Servicios
            </NavLink>
            <NavLink
              to="/simulador"
              className={({ isActive }) =>
                `nav-enlace nav-enlace-destacado ${isActive || paginaActiva === "simulador" ? "activo" : ""}`
              }
            >
              👁️ Simulador Visual
            </NavLink>
            <NavLink
              to="/impacto"
              className={({ isActive }) =>
                `nav-enlace nav-enlace-eco ${isActive || paginaActiva === "impacto" ? "activo" : ""}`
              }
            >
              🌱 Impacto Eco
            </NavLink>
            <NavLink
              to="/productos"
              className={({ isActive }) =>
                `nav-enlace ${isActive || paginaActiva === "productos" ? "activo" : ""}`
              }
            >
              Productos
            </NavLink>
            <NavLink
              to="/sedes"
              className={({ isActive }) =>
                `nav-enlace ${isActive || paginaActiva === "sedes" ? "activo" : ""}`
              }
            >
              Sedes
            </NavLink>
          </nav>

          <div className="landing-nav-botones">
            <Link
              to="/portal-paciente"
              className="btn btn-secundario"
              style={{ border: "1.5px solid #2563eb", color: "#2563eb", background: "#ffffff", fontWeight: 700 }}
              title="Portal de Pacientes: consulta tus citas sin códigos"
            >
              <span>👤 Portal Paciente</span>
            </Link>
            <Link to="/agendar-cita" className="btn btn-pnc-green">
              <span>📅 Agendar Cita</span>
            </Link>
            <Link to={sesion ? "/panel" : "/login"} className="btn btn-pnc-primary">
              <span>{sesion ? "Portal Interno" : "Acceso Personal"}</span>
            </Link>
          </div>
        </div>
      </header>
    </>
  );
}
