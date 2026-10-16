import { useState } from "react";
import { Link } from "react-router-dom";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import { FloatingHelpButton } from "../../components/FloatingHelpButton";

const PRESETS = [600, 1500, 3000, 6000, 12000, 25000];

const TESTIMONIOS = [
  {
    nombre: "Rafael Santana",
    rol: "Terapeuta Masajista en Centro TACTO",
    historia:
      "Perdí la visión a los 26 años en un accidente. Sentí que mi vida productiva había terminado. Llegar al Patronato me devolvió la dignidad. Me certifiqué en masoterapia y hoy mantengo a mis dos hijos con mis ingresos como masajista profesional.",
    fotoEmoji: "💆‍♂️",
    tiempo: "5 años en el Patronato",
  },
  {
    nombre: "Altagracia Guzmán",
    rol: "Supervisora del Taller de Trapeadores Ecológicos",
    historia:
      "En el taller de economía circular transformamos toneladas de recortes de algodón que antes iban a los vertederos en trapeadores de primera calidad. Trabajo con mis manos, gano mi sueldo y además cuido el medio ambiente dominicano.",
    fotoEmoji: "🧵",
    tiempo: "7 años en el Patronato",
  },
  {
    nombre: "Carlos Manuel De la Cruz",
    rol: "Graduado en Informática Adaptada y Bastón Blanco",
    historia:
      "Aprender a usar la computadora con lector de pantalla NVDA y desplazarme solo por Santo Domingo con mi bastón blanco me permitió ingresar a la universidad a estudiar Derecho. El Patronato no te da lástima, te da alas para volar.",
    fotoEmoji: "🎓",
    tiempo: "Graduado 2024",
  },
];

export function ImpactoPublicPage() {
  const [montoDonacion, setMontoDonacion] = useState(3000);

  // Fórmulas de impacto institucional
  const bastones = Math.floor(montoDonacion / 600);
  const consultas = Math.floor(montoDonacion / 1200);
  const librasHiloReciclado = Math.floor(montoDonacion * 0.06);
  const semanasBeca = (montoDonacion / 1500).toFixed(1);
  const galonesAguaAhorrados = Math.floor(librasHiloReciclado * 18);
  const cirugiasApadrinadas = (montoDonacion / 18000).toFixed(1);

  return (
    <div className="landing-moderna">
      <PublicNavbar paginaActiva="impacto" />

      {/* Hero Dedicado de Impacto */}
      <section className="pagina-publica-hero">
        <div className="pagina-publica-hero-inner">
          <div className="breadcrumb-nav">
            <Link to="/">Inicio</Link>
            <span>/</span>
            <span className="breadcrumb-actual">Impacto Eco-Solidario</span>
          </div>
          <span className="hero-pill-badge">🌱 Gestión Sostenible y Economía Circular</span>
          <h1 className="pagina-hero-titulo">
            Calculadora de <span className="texto-destacado-verde">Huella Social y Ecológica</span>
          </h1>
          <p className="pagina-hero-subtitulo">
            En el Patronato Nacional de Ciegos unimos la inclusión de personas con discapacidad visual con la protección del medio ambiente.
            Cada aporte y cada producto ecológico adquirido genera resultados tangibles y medibles para la República Dominicana.
          </p>
        </div>
      </section>

      {/* Calculadora Interactiva de Huella */}
      <section className="seccion-calculadora-dedicada">
        <div className="impacto-container">
          <div className="calculadora-card-principal">
            <div className="calculadora-header">
              <span className="calculadora-badge">🧮 Simulación de Impacto Directo</span>
              <h2 className="calculadora-titulo">¿Cuánto deseas simular o aportar?</h2>
              <p className="calculadora-subtitulo">
                Mueve el control deslizante o elige un monto sugerido para ver exactamente cómo se transforma en autonomía y sostenibilidad.
              </p>
            </div>

            {/* Selector de Monto */}
            <div className="monto-selector-box">
              <div className="monto-actual-display">
                <span className="moneda-simbolo">RD$</span>
                <span className="moneda-cifra">{montoDonacion.toLocaleString()}</span>
                <span className="moneda-pais">DOP</span>
              </div>

              <input
                type="range"
                min="500"
                max="50000"
                step="250"
                value={montoDonacion}
                onChange={(e) => setMontoDonacion(Number(e.target.value))}
                className="slider-monto"
                aria-label="Seleccionar monto en pesos dominicanos"
              />

              <div className="presets-botones-fila">
                {PRESETS.map((p) => (
                  <button
                    key={p}
                    type="button"
                    className={`btn-preset ${montoDonacion === p ? "activo" : ""}`}
                    onClick={() => setMontoDonacion(p)}
                  >
                    RD$ {p.toLocaleString()}
                  </button>
                ))}
              </div>
            </div>

            {/* Métricas Resultantes en Tiempo Real */}
            <div className="impacto-metricas-grid">
              <div className="tarjeta-impacto tono-azul">
                <div className="impacto-icono">🦯</div>
                <div className="impacto-cifra">{bastones}</div>
                <div className="impacto-etiqueta">Bastones Blancos</div>
                <p className="impacto-desc">
                  Entregados con entrenamiento personalizado de orientación y movilidad en calles dominicanas.
                </p>
              </div>

              <div className="tarjeta-impacto tono-celeste">
                <div className="impacto-icono">👁️</div>
                <div className="impacto-cifra">{consultas}</div>
                <div className="impacto-etiqueta">Consultas Oftalmológicas</div>
                <p className="impacto-desc">
                  Evaluaciones completas con fondo de ojo y presión intraocular para personas de escasos recursos.
                </p>
              </div>

              <div className="tarjeta-impacto tono-verde">
                <div className="impacto-icono">🌱</div>
                <div className="impacto-cifra">{librasHiloReciclado} lbs</div>
                <div className="impacto-etiqueta">Algodón Reciclado</div>
                <p className="impacto-desc">
                  Hilazas industriales rescatadas de vertederos y convertidas en trapeadores por invidentes.
                </p>
              </div>

              <div className="tarjeta-impacto tono-ambar">
                <div className="impacto-icono">📚</div>
                <div className="impacto-cifra">{semanasBeca}</div>
                <div className="impacto-etiqueta">Semanas de Beca Braille</div>
                <p className="impacto-desc">
                  Alimentación, transporte y docencia en sistema Braille, matemática y tecnología adaptada.
                </p>
              </div>

              <div className="tarjeta-impacto tono-cian">
                <div className="impacto-icono">💧</div>
                <div className="impacto-cifra">{galonesAguaAhorrados.toLocaleString()} gal</div>
                <div className="impacto-etiqueta">Agua Ahorrada</div>
                <p className="impacto-desc">
                  Evitamos el consumo hídrico del cultivo de algodón virgen gracias al reciclaje textil local.
                </p>
              </div>

              <div className="tarjeta-impacto tono-morado">
                <div className="impacto-icono">🏥</div>
                <div className="impacto-cifra">{cirugiasApadrinadas}</div>
                <div className="impacto-etiqueta">Cirugías de Catarata</div>
                <p className="impacto-desc">
                  Equivalente proporcional para apadrinar cirugías con lente intraocular que devuelven la visión.
                </p>
              </div>
            </div>

            {/* Acción Solidaria */}
            <div className="impacto-cta-box">
              <div className="impacto-cta-textos">
                <strong>¿Deseas realizar una donación institucional o apadrinamiento?</strong>
                <p>
                  Emitimos recibos de donación deducibles de impuestos según la Ley 122-05 para personas físicas y jurídicas en República Dominicana.
                </p>
              </div>
              <div className="impacto-cta-botones">
                <Link to="/agendar-cita" className="btn btn-pnc-green">
                  <span>🤝 Contactar para Donaciones</span>
                </Link>
                <Link to="/productos" className="btn btn-pnc-primary">
                  <span>🛒 Comprar Productos Ecológicos</span>
                </Link>
              </div>
            </div>
          </div>

          {/* Testimonios Reales de Impacto */}
          <div className="seccion-testimonios-bloque">
            <h3 className="testimonios-titulo-seccion">Voces de Superación y Autonomía</h3>
            <p className="testimonios-subtitulo-seccion">
              Conoce a quienes día a día demuestran que la ceguera no limita el talento ni las ganas de aportar al país.
            </p>

            <div className="testimonios-grid">
              {TESTIMONIOS.map((t, idx) => (
                <div key={idx} className="testimonio-card">
                  <div className="testimonio-header">
                    <span className="testimonio-avatar" aria-hidden="true">{t.fotoEmoji}</span>
                    <div>
                      <strong className="testimonio-nombre">{t.nombre}</strong>
                      <span className="testimonio-rol">{t.rol}</span>
                      <span className="testimonio-tiempo">{t.tiempo}</span>
                    </div>
                  </div>
                  <blockquote className="testimonio-cita">
                    "{t.historia}"
                  </blockquote>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <PublicFooter />
      <FloatingHelpButton />
    </div>
  );
}
