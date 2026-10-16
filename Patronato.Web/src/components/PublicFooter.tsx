import { Link } from "react-router-dom";

const ANIO_ACTUAL = new Date().getFullYear();

export function PublicFooter() {

  return (
    <footer className="landing-footer-moderno">
      <div className="landing-footer-grid">
        {/* Columna 1: Identidad Institucional y Logo */}
        <div className="footer-col-marca">
          <div className="footer-marca-header">
            <div className="footer-logo-container">
              <img
                src="/logo-pnc.png"
                alt="Logo Oficial Patronato Nacional de Ciegos"
                className="footer-logo-img"
              />
            </div>
            <div>
              <strong className="footer-marca-nombre">Patronato Nacional de Ciegos</strong>
              <span className="footer-marca-rnc">RNC: 4-01-00623-1 · Sin Fines de Lucro</span>
            </div>
          </div>
          <p className="footer-descripcion">
            Institución pionera fundada en 1964 dedicada a la rehabilitación, educación, salud visual,
            capacitación laboral y defensa de los derechos de las personas ciegas y de baja visión en la República Dominicana.
          </p>
          <div className="footer-eco-badge">
            <span>🌱 Comprometidos con la Economía Circular y el Empleo Inclusivo</span>
          </div>
        </div>

        {/* Columna 2: Navegación Rápida */}
        <div className="footer-col">
          <h4 className="footer-col-titulo">Navegación Institucional</h4>
          <ul className="footer-links">
            <li><Link to="/como-funciona">✨ ¿Cómo Funciona el Patronato?</Link></li>
            <li><Link to="/verificar">🔍 Consultar y Verificar Cita / Recibo</Link></li>
            <li><Link to="/induccion">🤝 Portal de Inducción al Personal</Link></li>
            <li><Link to="/servicios">Servicios Médicos y Rehabilitación</Link></li>
            <li><Link to="/simulador">Simulador Visual de Empatía</Link></li>
            <li><Link to="/impacto">Calculadora de Impacto Social</Link></li>
            <li><Link to="/productos">Artesanías y Productos Sostenibles</Link></li>
            <li><Link to="/sedes">Sedes Santo Domingo, Santiago y Barahona</Link></li>
            <li><Link to="/agendar-cita">Agendar Cita en Línea</Link></li>
          </ul>
        </div>

        {/* Columna 3: Sedes y Contacto */}
        <div className="footer-col">
          <h4 className="footer-col-titulo">Sedes y Contacto Directo</h4>
          <div className="footer-contacto-item">
            <strong>Sede Central Santo Domingo:</strong>
            <span>Av. Correa y Cidrón Esq. Huáscar Tejeda, Zona Universitaria.</span>
            <span>Teléfonos: (809) 508-7511 / (809) 533-2833</span>
          </div>
          <div className="footer-contacto-item" style={{ marginTop: 10 }}>
            <strong>Filial Santiago:</strong>
            <span>Av. Juan Pablo Duarte, Villa Olga, Santiago.</span>
            <span>Teléfono: (809) 575-9735</span>
          </div>
          <div className="footer-contacto-item" style={{ marginTop: 10 }}>
            <strong>Horarios de Atención:</strong>
            <span>Lunes a Viernes: 8:00 AM – 4:00 PM</span>
          </div>
        </div>
      </div>

      <div className="footer-bottom-bar">
        <span>© 1964 – {ANIO_ACTUAL} Patronato Nacional de Ciegos, Inc. Todos los derechos reservados.</span>
        <div className="footer-bottom-links">
          <span>Transparencia y Rendición de Cuentas</span>
          <span>•</span>
          <Link to="/login">Acceso Restringido Personal</Link>
        </div>
      </div>
    </footer>
  );
}
