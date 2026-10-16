import { useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getBeneficiarios, getSucursales, consultarSolicitudes } from "../api/endpoints";
import { EstadoCitaTexto, SedeTexto, TipoServicioCitaTexto, type SolicitudCita } from "../api/types";
import { PublicNavbar } from "../components/PublicNavbar";
import { PublicFooter } from "../components/PublicFooter";
import { ImageTicker } from "../components/ImageTicker";
import { FloatingHelpButton } from "../components/FloatingHelpButton";

// Programas institucionales oficiales de patronatociegos.org.do
const PROGRAMAS = [
  {
    icono: "👁️",
    titulo: "Prevención de Ceguera y Salud Ocular",
    tag: "Salud Preventiva",
    texto: "Operativos médicos y consultas oftalmológicas especializadas para detectar y tratar a tiempo condiciones como glaucoma, cataratas y retinopatía en comunidades vulnerables.",
  },
  {
    icono: "🦯",
    titulo: "Rehabilitación Integral",
    tag: "Autonomía y Dignidad",
    texto: "Capacitación funcional y profesional en sistema Braille, orientación y movilidad con bastón blanco, habilidades de vida diaria e informática adaptada con lectores de pantalla.",
  },
  {
    icono: "💆",
    titulo: "Centro de Masajes TACTO",
    tag: "Inserción Laboral",
    texto: "Terapias de masaje relajante, antiestrés y descontracturante brindadas por terapeutas invidentes altamente capacitados y certificados con excelencia profesional.",
  },
  {
    icono: "🌱",
    titulo: "Talleres Ecológicos y Artesanías",
    tag: "Sostenibilidad Verde",
    texto: "Producción de trapeadores con hilo 100% de algodón reciclado, limpiadores biodegradables y artesanías en macramé, promoviendo la economía circular y el empleo digno.",
  },
  {
    icono: "💳",
    titulo: "Microcréditos para Emprendimiento",
    tag: "Autosuficiencia",
    texto: "Financiamiento solidario sin intereses abusivos para que personas con discapacidad visual desarrollen colmados, panaderías, artesanías y negocios familiares propios.",
  },
  {
    icono: "🤝",
    titulo: "Voluntariado y Donaciones",
    tag: "Solidaridad Nacional",
    texto: "Red de aliados individuales y empresariales que apadrinan cirugías oftalmológicas, donan insumos y acompañan a nuestros beneficiarios en actividades formativas.",
  },
];

// Productos artesanales y ecológicos
const PRODUCTOS_ECO = [
  {
    id: 1,
    nombre: "Trapeador Ecológico PNC (Hilo 100% Reciclado)",
    categoria: "Limpieza Sostenible",
    icono: "🧹",
    descripcion: "Elaborado a mano por beneficiarios invidentes usando hilazas de algodón recuperadas. Alta absorción y durabilidad.",
    impacto: "Ahorra 12 galones de agua en confección y reutiliza 1.2 lbs de textil.",
    precio: "RD$ 180.00",
  },
  {
    id: 2,
    nombre: "Desinfectante Multiuso Biodegradable (Pino & Lavanda)",
    categoria: "Hogar Ecológico",
    icono: "🧴",
    descripcion: "Fórmula ecológica concentrada a base de extractos naturales. Seguro para mascotas, libre de fosfatos y no tóxico.",
    impacto: "100% biodegradable en aguas residuales y envase retornable.",
    precio: "RD$ 150.00 / Galón",
  },
  {
    id: 3,
    nombre: "Cesta y Bolso en Macramé Tejido",
    categoria: "Artesanía en Fibras",
    icono: "🧺",
    descripcion: "Piezas decorativas y utilitarias tejidas con fibras de yute y algodón natural. Resistencia y belleza artesanal dominicana.",
    impacto: "Sustituye fundas plásticas y genera ingreso directo al artesano.",
    precio: "RD$ 650.00",
  },
  {
    id: 4,
    nombre: "Cojín Ergonómico de Aromaterapia",
    categoria: "Bienestar & Relax",
    icono: "🌿",
    descripcion: "Confeccionado con retazos textiles recuperados y relleno de semillas seleccionadas con hierbas aromáticas relajantes.",
    impacto: "Reutilización de descartes textiles de alta calidad.",
    precio: "RD$ 450.00",
  },
];

export function LandingPage() {
  const beneficiarios = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const sucursales = useQuery({ queryKey: ["sucursales"], queryFn: getSucursales });

  // Estado del Simulador Visual de Empatía
  const [condicionVisual, setCondicionVisual] = useState<
    "normal" | "cataratas" | "glaucoma" | "retinopatia" | "baja-vision"
  >("normal");
  const [severidad, setSeveridad] = useState(65);

  // Estado de la Calculadora de Impacto Social y Ecológico
  const [montoDonacion, setMontoDonacion] = useState(3000);

  // Estado de la Consulta Rápida de Citas
  const [terminoCita, setTerminoCita] = useState("");
  const [buscandoCita, setBuscandoCita] = useState(false);
  const [citasEncontradas, setCitasEncontradas] = useState<SolicitudCita[] | null>(null);
  const [errorCita, setErrorCita] = useState<string | null>(null);

  const consultarCitaRapida = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!terminoCita.trim()) return;
    setBuscandoCita(true);
    setErrorCita(null);
    try {
      const res = await consultarSolicitudes(terminoCita.trim());
      setCitasEncontradas(res);
    } catch {
      setErrorCita("No fue posible consultar la solicitud en este momento. Intenta nuevamente.");
    } finally {
      setBuscandoCita(false);
    }
  };

  // Cálculo del impacto social y ecológico según donación
  const bastones = Math.floor(montoDonacion / 600);
  const consultas = Math.floor(montoDonacion / 1200);
  const librasHiloReciclado = Math.floor(montoDonacion * 0.05);
  const semanasBeca = (montoDonacion / 1500).toFixed(1);

  return (
    <div className="landing-moderna">
      <PublicNavbar paginaActiva="inicio" />

      {/* 3. Hero Principal con Efectos de Luz */}
      <section className="landing-hero-moderno">
        <div className="hero-fondo-degrade" aria-hidden="true" />
        <div className="hero-contenido-grid">
          <div className="hero-textos">
            <div className="hero-badge-pill">
              <span className="badge-icono">🏛️</span>
              <span>60+ Años Iluminando Vidas y Generando Autonomía</span>
            </div>
            <h1 className="hero-titulo">
              La ceguera no te impide <span className="texto-destacado-verde">ver con el corazón</span>
            </h1>
            <p className="hero-descripcion">
              Acompañamos a personas ciegas y de baja visión en todo el territorio nacional hacia su
              plena independencia personal, laboral y comunitaria, integrando talleres de economía circular y
              servicios oftalmológicos de calidad.
            </p>

            <div className="hero-cta-group">
              <Link to="/agendar-cita" className="btn btn-pnc-hero-primary">
                <span>Agendar Consulta u Oftalmología</span>
                <span aria-hidden="true"> →</span>
              </Link>
              <Link to="/como-funciona" className="btn btn-pnc-hero-secundary">
                <span>✨ ¿Cómo Funciona? (Paso a Paso)</span>
              </Link>
              <Link to="/verificar" className="btn btn-pnc-hero-secundary">
                <span>🔍 Verificar Cita o Recibo</span>
              </Link>
              <Link to="/simulador" className="btn btn-pnc-hero-secundary">
                <span>👁️ Simulador Visual</span>
              </Link>
              <Link to="/impacto" className="btn btn-pnc-hero-eco">
                <span>🌱 Calcular Huella Solidaria</span>
              </Link>
            </div>

            {/* Micro métricas de confianza */}
            <div className="hero-micro-confianza">
              <div className="micro-item">
                <span className="micro-check">✓</span>
                <span>Atención médica subsidiada</span>
              </div>
              <div className="micro-item">
                <span className="micro-check">✓</span>
                <span>Terapeutas TACTO certificados</span>
              </div>
              <div className="micro-item">
                <span className="micro-check">✓</span>
                <span>Talleres de algodón reciclado</span>
              </div>
            </div>
          </div>

          {/* Tarjetas de Estadísticas Reales */}
          <div className="hero-metricas-panel">
            <div className="kpi-card kpi-azul">
              <div className="kpi-icono">👥</div>
              <div className="kpi-cuerpo">
                <span className="kpi-numero">{beneficiarios.data ? beneficiarios.data.length : "80+"}</span>
                <span className="kpi-rotulo">Beneficiarios Activos en Acompañamiento</span>
                <span className="kpi-pie">En programas de Braille, movilidad y oficios</span>
              </div>
            </div>

            <div className="kpi-card kpi-verde">
              <div className="kpi-icono">🏢</div>
              <div className="kpi-cuerpo">
                <span className="kpi-numero">{sucursales.data ? sucursales.data.length : "3"}</span>
                <span className="kpi-rotulo">Sedes a Nivel Nacional</span>
                <span className="kpi-pie">Santo Domingo, Santiago y Barahona</span>
              </div>
            </div>

            <div className="kpi-card kpi-ambar">
              <div className="kpi-icono">♻️</div>
              <div className="kpi-cuerpo">
                <span className="kpi-numero">100%</span>
                <span className="kpi-rotulo">Talleres de Economía Circular</span>
                <span className="kpi-pie">Trapeadores ecológicos y reciclaje textil</span>
              </div>
            </div>

            <div className="kpi-card kpi-morado">
              <div className="kpi-icono">💆</div>
              <div className="kpi-cuerpo">
                <span className="kpi-numero">TACTO</span>
                <span className="kpi-rotulo">Centro de Masajes Terapéuticos</span>
                <span className="kpi-pie">Empleo digno para masajistas invidentes</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 4. Simulador Interactivo de Empatía Visual (¡Interactúa con la página!) */}
      <section id="simulador" className="seccion-bloque seccion-simulador">
        <div className="seccion-encabezado text-center">
          <span className="seccion-subtitulo-pill">Experiencia Interactiva de Concienciación</span>
          <h2 className="seccion-titulo">Simulador de Empatía Visual</h2>
          <p className="seccion-descripcion">
            Experimenta en primera persona cómo perciben el entorno las personas con distintas condiciones visuales
            y comprende la importancia vital del diagnóstico oportuno.
          </p>
        </div>

        <div className="simulador-contenedor">
          {/* Selector de condiciones */}
          <div className="simulador-controles">
            <span className="controles-label">Selecciona una condición ocular:</span>
            <div className="simulador-tabs">
              <button
                type="button"
                className={`simulador-tab-btn ${condicionVisual === "normal" ? "activo" : ""}`}
                onClick={() => setCondicionVisual("normal")}
              >
                👁️ Visión Normal
              </button>
              <button
                type="button"
                className={`simulador-tab-btn ${condicionVisual === "cataratas" ? "activo" : ""}`}
                onClick={() => setCondicionVisual("cataratas")}
              >
                🌫️ Cataratas
              </button>
              <button
                type="button"
                className={`simulador-tab-btn ${condicionVisual === "glaucoma" ? "activo" : ""}`}
                onClick={() => setCondicionVisual("glaucoma")}
              >
                🔍 Glaucoma (Túnel)
              </button>
              <button
                type="button"
                className={`simulador-tab-btn ${condicionVisual === "retinopatia" ? "activo" : ""}`}
                onClick={() => setCondicionVisual("retinopatia")}
              >
                🌑 Retinopatía Diabética
              </button>
              <button
                type="button"
                className={`simulador-tab-btn ${condicionVisual === "baja-vision" ? "activo" : ""}`}
                onClick={() => setCondicionVisual("baja-vision")}
              >
                🔅 Baja Visión
              </button>
            </div>

            {/* Slider de severidad */}
            {condicionVisual !== "normal" && (
              <div className="simulador-slider-group">
                <div className="slider-header">
                  <label htmlFor="severidad-slider">Nivel de Progresión de la Condición:</label>
                  <span className="slider-valor-badge">{severidad}%</span>
                </div>
                <input
                  id="severidad-slider"
                  type="range"
                  min="25"
                  max="100"
                  value={severidad}
                  onChange={(e) => setSeveridad(Number(e.target.value))}
                  className="slider-input"
                />
                <div className="slider-pistas">
                  <span>Inicial</span>
                  <span>Moderada</span>
                  <span>Avanzada</span>
                </div>
              </div>
            )}
          </div>

          {/* Visor de simulación */}
          <div className="simulador-visor-card">
            <div className="visor-escena-wrapper">
              {/* Imagen de fondo representativa de Santo Domingo / entorno cotidiano */}
              <div
                className={`visor-capa-simulada condicion-${condicionVisual}`}
                style={{
                  filter:
                    condicionVisual === "normal"
                      ? "none"
                      : condicionVisual === "cataratas"
                      ? `blur(${Math.round(severidad * 0.08)}px) contrast(${100 - severidad * 0.4}%) brightness(${100 + severidad * 0.2}%)`
                      : condicionVisual === "baja-vision"
                      ? `blur(${Math.round(severidad * 0.05)}px) contrast(60%) grayscale(${Math.round(severidad * 0.5)}%)`
                      : undefined,
                }}
              >
                <div className="escena-elementos">
                  <div className="escena-calle-decorativa">
                    <div className="escena-cielo">
                      <div className="sol-radiante" />
                    </div>
                    <div className="escena-ciudad">
                      <div className="edificio ed-1">🏢 Sede PNC</div>
                      <div className="edificio ed-2">🏫 Escuela</div>
                      <div className="edificio ed-3">🏥 Hospital</div>
                    </div>
                    <div className="escena-calle">
                      <div className="paso-peatonal" />
                      <div className="persona-caminando">🚶 Peatón cruzando con bastón blanco</div>
                      <div className="senal-transito">🛑 Pare</div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Capas especiales para Glaucoma y Retinopatía */}
              {condicionVisual === "glaucoma" && (
                <div
                  className="capa-glaucoma-tunel"
                  style={{
                    background: `radial-gradient(circle at center, transparent ${Math.max(10, 45 - severidad * 0.35)}%, rgba(10, 15, 30, 0.95) ${Math.max(25, 75 - severidad * 0.3)}%, black 100%)`,
                  }}
                  aria-hidden="true"
                />
              )}

              {condicionVisual === "retinopatia" && (
                <div className="capa-retinopatia-escotomas" aria-hidden="true">
                  <div className="escotoma sc-1" style={{ opacity: severidad / 100 }} />
                  <div className="escotoma sc-2" style={{ opacity: severidad / 100 }} />
                  <div className="escotoma sc-3" style={{ opacity: severidad / 100 }} />
                  <div className="escotoma sc-4" style={{ opacity: severidad / 100 }} />
                  <div className="escotoma sc-5" style={{ opacity: severidad / 100 }} />
                </div>
              )}

              {/* Indicador de simulación activa */}
              <div className="visor-badge-estado">
                <span>Modo de visualización:</span>
                <strong>
                  {condicionVisual === "normal" && "Visión Óptima 20/20"}
                  {condicionVisual === "cataratas" && `Cataratas (${severidad}% opacidad)`}
                  {condicionVisual === "glaucoma" && `Glaucoma (${severidad}% pérdida periférica)`}
                  {condicionVisual === "retinopatia" && `Retinopatía (${severidad}% manchas retinianas)`}
                  {condicionVisual === "baja-vision" && `Baja Visión (${severidad}% pérdida de contraste)`}
                </strong>
              </div>
            </div>

            {/* Tarjeta explicativa de la condición seleccionada */}
            <div className="visor-explicacion">
              <div className="explicacion-header">
                <h3>
                  {condicionVisual === "normal" && "👁️ Visión Clara y Definida"}
                  {condicionVisual === "cataratas" && "🌫️ Cataratas (Opacificación del Cristalino)"}
                  {condicionVisual === "glaucoma" && "🔍 Glaucoma (Pérdida Silenciosa del Campo Visual)"}
                  {condicionVisual === "retinopatia" && "🌑 Retinopatía Diabética (Daño Vascular Ocular)"}
                  {condicionVisual === "baja-vision" && "🔅 Baja Visión o Pérdida de Contraste Severa"}
                </h3>
              </div>
              <p className="explicacion-cuerpo">
                {condicionVisual === "normal" &&
                  "La luz enfoca perfectamente en la retina. Aprecia los colores, las distancias y los detalles con nitidez. El chequeo oftalmológico anual mantiene tu salud ocular protegida."}
                {condicionVisual === "cataratas" &&
                  "El cristalino natural se torna turbio y lechoso, provocando visión nublada similar a mirar por una ventana empañada, halos de luz molestos al anochecer y desvanecimiento de colores. ¡Es operable y tratable!"}
                {condicionVisual === "glaucoma" &&
                  "Conocido como el ladrón silencioso de la vista. Aumenta la presión intraocular y destruye gradualmente el nervio óptico desde los extremos hacia el centro. Sin dolor inicial, pero irreversible sin control."}
                {condicionVisual === "retinopatia" &&
                  "La diabetes mal regulada daña los diminutos vasos sanguíneos que alimentan la retina, provocando pequeñas hemorragias, manchas oscuras flotantes (escotomas) y distorsión de la lectura."}
                {condicionVisual === "baja-vision" &&
                  "Condición donde la persona conserva un remanente visual que no puede corregirse con lentes convencionales. En el Patronato brindamos lupas especiales, telescopios y entrenamiento en ayudas ópticas."}
              </p>
              <div className="explicacion-footer">
                <span className="explicacion-cta-texto">
                  ¿Tú o un familiar sienten molestias visuales? La prevención salva ojos.
                </span>
                <Link to="/agendar-cita" className="btn btn-pnc-small">
                  Agendar Examen Visual Preventivo
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 5. Calculadora Interactiva de Huella Social y Ecológica */}
      <section id="impacto" className="seccion-bloque seccion-impacto-eco">
        <div className="seccion-encabezado text-center">
          <span className="seccion-subtitulo-pill verde">🌱 Compromiso Social & Sostenibilidad</span>
          <h2 className="seccion-titulo">Calculadora de Impacto Solidario y Ecológico</h2>
          <p className="seccion-descripcion">
            Cada aporte no solo cambia la vida de un beneficiario invidente en República Dominicana, sino que también
            impulsa nuestros talleres verdes de economía circular y reciclaje textil.
          </p>
        </div>

        <div className="calculadora-card">
          <div className="calculadora-control-col">
            <label className="calculadora-label">Selecciona o desliza el monto de tu aporte solidario:</label>
            <div className="monto-destacado-display">
              <span className="moneda">RD$</span>
              <span className="cifra">{montoDonacion.toLocaleString("es-DO")}</span>
              <span className="periodo">/ aporte voluntario</span>
            </div>

            <input
              type="range"
              min="500"
              max="15000"
              step="250"
              value={montoDonacion}
              onChange={(e) => setMontoDonacion(Number(e.target.value))}
              className="calculadora-slider"
            />

            <div className="calculadora-presets">
              <span className="presets-etiqueta">Montos sugeridos:</span>
              <div className="presets-botones">
                {[500, 1500, 3000, 6000, 12000].map((m) => (
                  <button
                    key={m}
                    type="button"
                    className={`btn-preset ${montoDonacion === m ? "activo" : ""}`}
                    onClick={() => setMontoDonacion(m)}
                  >
                    RD$ {m.toLocaleString("es-DO")}
                  </button>
                ))}
              </div>
            </div>

            <div className="eco-nota-transparencia">
              <span>🌿 100% deducible de impuestos bajo la Ley Dominicana de ONGs.</span>
            </div>
          </div>

          {/* Resultados de la Huella */}
          <div className="calculadora-resultados-col">
            <h4 className="resultados-titulo">Con tu aporte de RD$ {montoDonacion.toLocaleString("es-DO")} haces posible:</h4>
            
            <div className="resultados-grid">
              <div className="resultado-item">
                <div className="res-icono">🦯</div>
                <div className="res-datos">
                  <span className="res-valor">{bastones > 0 ? bastones : 1}</span>
                  <span className="res-label">Bastón(es) Blanco(s) de Orientación</span>
                  <span className="res-detalle">Herramienta clave de movilidad e independencia</span>
                </div>
              </div>

              <div className="resultado-item">
                <div className="res-icono">🩺</div>
                <div className="res-datos">
                  <span className="res-valor">{consultas > 0 ? consultas : 1}</span>
                  <span className="res-label">Consulta(s) Oftalmológica(s) Completa(s)</span>
                  <span className="res-detalle">Con gotas dilatadoras y fondo de ojo preventivo</span>
                </div>
              </div>

              <div className="resultado-item eco">
                <div className="res-icono">♻️</div>
                <div className="res-datos">
                  <span className="res-valor">{librasHiloReciclado} lbs</span>
                  <span className="res-label">Algodón Reciclado Procesado</span>
                  <span className="res-detalle">Convertido en trapeadores por artesanos invidentes</span>
                </div>
              </div>

              <div className="resultado-item">
                <div className="res-icono">📚</div>
                <div className="res-datos">
                  <span className="res-valor">{semanasBeca}</span>
                  <span className="res-label">Semanas de Formación Integral</span>
                  <span className="res-detalle">Clases de Braille, macramé y computación adaptada</span>
                </div>
              </div>
            </div>

            <div className="calculadora-cta">
              <Link to="/agendar-cita?tab=consultar" className="btn btn-pnc-green btn-block">
                <span>Sumar Mi Apoyo al Patronato</span>
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* 6. Catálogo de Productos Ecológicos y Artesanales */}
      <section id="productos-eco" className="seccion-bloque">
        <div className="seccion-encabezado text-center">
          <span className="seccion-subtitulo-pill verde">Economía Circular & Empleo Digno</span>
          <h2 className="seccion-titulo">Artesanías y Productos Ecológicos PNC</h2>
          <p className="seccion-descripcion">
            Cada compra sostiene directamente los puestos de trabajo de personas con discapacidad visual
            en nuestros talleres productivos en Santo Domingo y sedes regionales.
          </p>
        </div>

        <div className="productos-grid">
          {PRODUCTOS_ECO.map((prod) => (
            <div key={prod.id} className="producto-card">
              <div className="prod-badge-cat">{prod.categoria}</div>
              <div className="prod-icono-header">{prod.icono}</div>
              <h3 className="prod-nombre">{prod.nombre}</h3>
              <p className="prod-descripcion">{prod.descripcion}</p>
              
              <div className="prod-impacto-pill">
                <span className="eco-leaf">🌱</span>
                <span>{prod.impacto}</span>
              </div>

              <div className="prod-footer">
                <span className="prod-precio">{prod.precio}</span>
                <span className="prod-origen">Taller Inclusivo PNC</span>
              </div>
            </div>
          ))}
        </div>

        <div className="productos-banner-pedidos">
          <div className="pedidos-textos">
            <h4>¿Tu empresa o institución necesita insumos de limpieza ecológicos al por mayor?</h4>
            <p>
              Suministramos trapeadores reciclados y desinfectantes biodegradables a bancos, colegios,
              hospitales e industrias dominicanas con factura con valor fiscal (NCF).
            </p>
          </div>
          <a href="tel:8095087511" className="btn btn-pnc-primary">
            📞 Cotizar Pedido Institucional
          </a>
        </div>
      </section>

      {/* 7. Consulta Rápida de Solicitudes y Citas */}
      <section id="consultar-cita" className="seccion-bloque seccion-consulta-rapida">
        <div className="consulta-card-wrapper">
          <div className="consulta-header">
            <span className="consulta-badge">🔍 Autoservicio del Paciente</span>
            <h2>Consulta el Estado de tu Cita u Operativo</h2>
            <p>
              Ingresa tu número de teléfono o tu código de solicitud para verificar la fecha,
              sede y estado de tu cita médica o sesión de rehabilitación.
            </p>
          </div>

          <form className="consulta-form" onSubmit={consultarCitaRapida}>
            <div className="consulta-input-group">
              <input
                type="text"
                value={terminoCita}
                onChange={(e) => setTerminoCita(e.target.value)}
                placeholder="Ingresa tu teléfono (ej: 809-555-0101) o código"
                required
              />
              <button type="submit" className="btn btn-pnc-primary" disabled={buscandoCita}>
                {buscandoCita ? "Buscando..." : "Consultar Cita"}
              </button>
            </div>
          </form>

          {errorCita && (
            <div className="alerta alerta-error" style={{ marginTop: 16 }}>
              {errorCita}
            </div>
          )}

          {/* Resultados de búsqueda */}
          {citasEncontradas && (
            <div className="consulta-resultados-box">
              {citasEncontradas.length === 0 ? (
                <div className="consulta-vacia">
                  <span className="vacia-icono">ℹ️</span>
                  <p>No se encontraron citas con el término ingresado. Verifica tu número o agenda una nueva cita.</p>
                  <Link to="/agendar-cita" className="btn btn-pnc-small">
                    Agendar Nueva Cita
                  </Link>
                </div>
              ) : (
                <div className="citas-lista">
                  <h4>Solicitudes Encontradas ({citasEncontradas.length}):</h4>
                  <div className="citas-cards-grid">
                    {citasEncontradas.map((c) => (
                      <div key={c.id} className="cita-tarjeta">
                        <div className="cita-tarjeta-header">
                          <span className="cita-codigo">Solicitud #{c.id}</span>
                          <span className={`badge-estado estado-${c.estado}`}>
                            {EstadoCitaTexto[c.estado] ?? "Pendiente"}
                          </span>
                        </div>
                        <div className="cita-tarjeta-cuerpo">
                          <p><strong>Paciente:</strong> {c.nombreSolicitante}</p>
                          <p><strong>Servicio:</strong> {TipoServicioCitaTexto[c.tipoServicio]}</p>
                          <p><strong>Sede:</strong> {SedeTexto[c.sedePreferida]}</p>
                          <p><strong>Fecha preferida:</strong> {new Date(c.fechaPreferida).toLocaleDateString("es-DO")}</p>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </section>

      {/* 8. Líneas de Acción y Programas */}
      <section id="servicios" className="seccion-bloque seccion-programas">
        <div className="seccion-encabezado text-center">
          <span className="seccion-subtitulo-pill">Nuestra Misión en Acción</span>
          <h2 className="seccion-titulo">Servicios y Líneas de Atención</h2>
          <p className="seccion-descripcion">
            Abordamos la discapacidad visual con un modelo integral: desde la prevención clínica hasta
            el desarrollo de habilidades productivas y la inserción laboral.
          </p>
        </div>

        <div className="programas-grid">
          {PROGRAMAS.map((prog) => (
            <div key={prog.titulo} className="programa-card">
              <div className="prog-tag">{prog.tag}</div>
              <div className="prog-icono">{prog.icono}</div>
              <h3 className="prog-titulo">{prog.titulo}</h3>
              <p className="prog-texto">{prog.texto}</p>
            </div>
          ))}
        </div>
      </section>

      {/* 9. Nuestras Sedes Nacionales */}
      <section id="sedes" className="seccion-bloque seccion-sedes">
        <div className="seccion-encabezado text-center">
          <span className="seccion-subtitulo-pill">Presencia Territorial</span>
          <h2 className="seccion-titulo">Nuestras Sedes</h2>
          <p className="seccion-descripcion">
            Instalaciones preparadas con especialistas, consultorios oftalmológicos y talleres formativos.
          </p>
        </div>

        <div className="sedes-grid">
          <div className="sede-card destacado">
            <div className="sede-badge">Sede Central</div>
            <h3>🏢 Sede Santo Domingo</h3>
            <p className="sede-direccion">Av. Dr. Bernardo Correa y Cidrón Esq. Ing. Huáscar Tejeda, Zona Universitaria, Santo Domingo, D.N.</p>
            <p className="sede-telefono">📞 (809) 508-7511 / (809) 533-2833</p>
            <p className="sede-horario">🕒 Lunes a Viernes: 8:00 AM – 4:00 PM</p>
          </div>

          <div className="sede-card">
            <div className="sede-badge">Filial Norte</div>
            <h3>🏢 Filial Santiago</h3>
            <p className="sede-direccion">Calle Del Sol Esq. Sabana Larga, Centro Histórico, Santiago de los Caballeros.</p>
            <p className="sede-telefono">📞 (809) 575-9735</p>
            <p className="sede-horario">🕒 Lunes a Viernes: 8:00 AM – 3:30 PM</p>
          </div>

          <div className="sede-card">
            <div className="sede-badge">Filial Sur</div>
            <h3>🏢 Filial Barahona</h3>
            <p className="sede-direccion">Calle Nuestra Señora del Rosario #45, Barahona.</p>
            <p className="sede-telefono">📞 (809) 524-4343</p>
            <p className="sede-horario">🕒 Lunes a Viernes: 8:00 AM – 2:00 PM</p>
          </div>
        </div>
      </section>

      {/* 9.5. Ticker Infinito de Imágenes y Programas */}
      <ImageTicker />

      {/* 10. Pie de Página Institucional */}
      <PublicFooter />

      {/* 11. Botón Flotante de Dudas y Asistencia WhatsApp */}
      <FloatingHelpButton />
    </div>
  );
}
