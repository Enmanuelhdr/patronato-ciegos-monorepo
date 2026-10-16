import { useState } from "react";
import { Link } from "react-router-dom";

export function FloatingHelpButton() {
  const [abierto, setAbierto] = useState(false);

  return (
    <div className="floating-help-wrapper">
      {abierto && (
        <div className="floating-help-modal" role="dialog" aria-label="Centro de Ayuda y Dudas">
          <div className="floating-help-header">
            <button
              type="button"
              className="floating-help-close-btn"
              onClick={() => setAbierto(false)}
              aria-label="Cerrar ventana de ayuda"
            >
              ✕
            </button>
            <div className="floating-help-header-title">
              Patronato Nacional de Ciegos
            </div>
            <div className="floating-help-header-status">
              <span className="floating-help-status-dot" />
              <span>Atención al Ciudadano · En Línea</span>
            </div>
          </div>

          <div className="floating-help-body">
            {/* WhatsApp Directo */}
            <a
              href="https://wa.me/18095087511?text=Hola,%20deseo%20informaci%C3%B3n%20sobre%20los%20servicios%20del%20Patronato%20Nacional%20de%20Ciegos."
              target="_blank"
              rel="noopener noreferrer"
              className="floating-help-action-card"
            >
              <div className="floating-help-icon-wrap floating-help-icon-whatsapp">
                💬
              </div>
              <div className="floating-help-card-text">
                <strong>WhatsApp Oficial</strong>
                <span>(809) 508-7511 · Chat directo</span>
              </div>
            </a>

            {/* Llamada Central */}
            <a href="tel:8095332833" className="floating-help-action-card">
              <div className="floating-help-icon-wrap floating-help-icon-phone">
                📞
              </div>
              <div className="floating-help-card-text">
                <strong>Central Telefónica</strong>
                <span>(809) 533-2833 · Santo Domingo</span>
              </div>
            </a>

            {/* Verificar Cita o Donación */}
            <Link
              to="/verificar"
              className="floating-help-action-card"
              onClick={() => setAbierto(false)}
            >
              <div className="floating-help-icon-wrap floating-help-icon-check">
                🔍
              </div>
              <div className="floating-help-card-text">
                <strong>Verificar Cita o Donación</strong>
                <span>Consulta transparente de turnos y recibos</span>
              </div>
            </Link>

            {/* Cómo Funciona */}
            <Link
              to="/como-funciona"
              className="floating-help-action-card"
              onClick={() => setAbierto(false)}
            >
              <div className="floating-help-icon-wrap floating-help-icon-faq">
                ❓
              </div>
              <div className="floating-help-card-text">
                <strong>Cómo Funciona el Patronato</strong>
                <span>Preguntas frecuentes y proceso paso a paso</span>
              </div>
            </Link>
          </div>

          <div className="floating-help-socials">
            <span style={{ fontSize: "11px", color: "#64748b", fontWeight: 600 }}>
              Síguenos en redes:
            </span>
            <div className="floating-help-social-links">
              <a
                href="https://www.instagram.com/patronatociegosrd"
                target="_blank"
                rel="noopener noreferrer"
                className="social-circle-link"
                title="Instagram @patronatociegosrd"
              >
                📸
              </a>
              <a
                href="https://wa.me/18095087511"
                target="_blank"
                rel="noopener noreferrer"
                className="social-circle-link"
                title="WhatsApp Institucional"
              >
                💬
              </a>
              <a
                href="https://www.facebook.com/patronatociegosrd"
                target="_blank"
                rel="noopener noreferrer"
                className="social-circle-link"
                title="Facebook Patronato Nacional de Ciegos"
              >
                📘
              </a>
            </div>
          </div>
        </div>
      )}

      {/* Botón flotante siempre visible */}
      <button
        type="button"
        className="floating-help-pulse-btn"
        onClick={() => setAbierto(!abierto)}
        aria-label="Abrir opciones de ayuda y WhatsApp"
      >
        <div className="floating-help-ring" />
        <span style={{ fontSize: "18px" }}>💬</span>
        <span>¿Tienes dudas? Escríbenos</span>
      </button>
    </div>
  );
}
