import { useState } from "react";
import { Link } from "react-router-dom";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import { FloatingHelpButton } from "../../components/FloatingHelpButton";

type CondicionVisual = "normal" | "cataratas" | "glaucoma" | "retinopatia" | "baja-vision";

const FECHA_HOY = new Date().toLocaleDateString();

interface InfoPatologia {
  nombre: string;
  subtitulo: string;
  descripcion: string;
  sintomas: string[];
  prevencion: string;
  recomendacion: string;
}

const PATOLOGIAS: Record<CondicionVisual, InfoPatologia> = {
  normal: {
    nombre: "Visión Estándar y Salud Ocular Normal",
    subtitulo: "Agudeza visual 20/20 con campo visual completo de 180°",
    descripcion:
      "Percepción nítida de formas, colores, bordes y contrastes en condiciones normales de luz natural y artificial.",
    sintomas: ["Enfoque claro de objetos cercanos y lejanos", "Percepción periférica sin obstrucciones", "Visión cromática completa"],
    prevencion: "Se recomienda un examen oftalmológico preventivo anual a partir de los 40 años o cada 2 años en personas jóvenes.",
    recomendacion: "Mantén chequeos regulares para detectar silenciosamente alteraciones antes de que afecten tu visión.",
  },
  cataratas: {
    nombre: "Catarata Senil o Traumática",
    subtitulo: "Opacificación progresiva del cristalino ocular",
    descripcion:
      "El cristalino natural del ojo se vuelve turbio o blanquecino, provocando que la persona vea como a través de un cristal empañado, escarchado o con niebla espesa.",
    sintomas: [
      "Visión nublada, borrosa o difusa en todo el campo",
      "Colores atenuados o amarillentos",
      "Destellos molestos alrededor de las luces nocturnas",
      "Dificultad progresiva para leer y reconocer rostros",
    ],
    prevencion: "Uso de gafas de sol con protección UV400, no fumar y control estricto de la glucemia en diabéticos.",
    recomendacion:
      "La catarata es reversible mediante cirugía ambulatoria con implante de lente intraocular (LIO). En el Patronato coordinamos operativos de diagnóstico y cirugías subsidiadas.",
  },
  glaucoma: {
    nombre: "Glaucoma de Ángulo Abierto (Ladrón Silencioso de la Vista)",
    subtitulo: "Daño irreversible al nervio óptico por presión intraocular elevada",
    descripcion:
      "Enfermedad progresiva y asintomática en sus etapas tempranas que destruye lentamente las fibras del nervio óptico, reduciendo la visión periférica hasta dejar únicamente un túnel estrecho.",
    sintomas: [
      "Pérdida imperceptible de la visión en los bordes o extremos",
      "Efecto de visión a través de un tubo o cañón (visión tubular)",
      "Tropiezos frecuentes con escalones o marcos de puertas",
      "Puntos ciegos laterales que el cerebro intenta compensar",
    ],
    prevencion:
      "Medición de presión intraocular (tonometría) y examen de fondo de ojo cada año, especialmente con antecedentes familiares o personas de ascendencia afrodescendiente.",
    recomendacion:
      "El daño del glaucoma no se recupera, pero con gotas hipotensoras o trabeculoplastia láser se previene la ceguera total. ¡El diagnóstico a tiempo salva la visión!",
  },
  retinopatia: {
    nombre: "Retinopatía Diabética Proliferativa",
    subtitulo: "Deterioro y hemorragias en los microvasos de la retina por diabetes",
    descripcion:
      "La glucosa alta y descontrolada daña los vasos sanguíneos que nutren la retina, causando fugas de sangre, exudados y cicatrices que generan manchas negras flotantes (escotomas).",
    sintomas: [
      "Manchas negras oscuras o 'moscas volantes' que bloquean áreas visuales",
      "Áreas vacías o sombras en partes del campo visual",
      "Fluctuaciones bruscas de visión de un día a otro",
      "Visión nocturna severamente disminuida",
    ],
    prevencion:
      "Control riguroso de la hemoglobina glicosilada (HbA1c < 7%), control de presión arterial y revisión de retina con pupila dilatada al menos una vez al año.",
    recomendacion:
      "Tratamientos con fotocoagulación láser o inyecciones intravítreas detienen el avance si se aplican a tiempo. La educación y rehabilitación del Patronato ayudan a superar el impacto funcional.",
  },
  "baja-vision": {
    nombre: "Baja Visión Severa y Degeneración Macular",
    subtitulo: "Agudeza visual inferior a 20/70 que no corrige con lentes convencionales",
    descripcion:
      "Discapacidad visual significativa donde la persona conserva un remanente visual valioso. A menudo afecta el centro de la mirada (mácula), impidiendo ver detalles finos, letras o rostros.",
    sintomas: [
      "Mancha borrosa u oscura fija en el centro de la visión",
      "Líneas rectas que se perciben onduladas o distorsionadas",
      "Necesidad de acercarse excesivamente a pantallas y textos",
      "Dificultad extrema con los cambios de luz y deslumbramiento",
    ],
    prevencion: "Dieta rica en antioxidantes (luteína, zeaxantina), protección solar y evitar el tabaquismo activo y pasivo.",
    recomendacion:
      "En el Patronato enseñamos a potenciar el remanente visual mediante lupas telescópicas, filtros de contraste, iluminación dirigida y entrenamiento en orientación.",
  },
};

export function SimuladorPublicPage() {
  const [condicion, setCondicion] = useState<CondicionVisual>("normal");
  const [severidad, setSeveridad] = useState(65);
  const [escena, setEscena] = useState<"calle" | "lectura">("calle");

  const info = PATOLOGIAS[condicion];

  // Cálculo del estilo visual reactivo según la patología
  const getSimuladorStyle = () => {
    const factor = severidad / 100;
    switch (condicion) {
      case "cataratas":
        return {
          filter: `blur(${Math.max(1, factor * 9)}px) contrast(${Math.max(0.4, 1 - factor * 0.55)}) brightness(${
            1 + factor * 0.15
          }) sepia(${factor * 0.35})`,
        };
      case "glaucoma":
        return {
          filter: `contrast(${1 + factor * 0.2})`,
        };
      case "retinopatia":
        return {
          filter: `blur(${factor * 2}px) contrast(${1 - factor * 0.2})`,
        };
      case "baja-vision":
        return {
          filter: `blur(${Math.max(1.5, factor * 7)}px) contrast(${Math.max(0.5, 1 - factor * 0.4)})`,
        };
      default:
        return {};
    }
  };

  return (
    <div className="landing-moderna">
      <PublicNavbar paginaActiva="simulador" />

      {/* Hero Dedicado del Simulador */}
      <section className="pagina-publica-hero">
        <div className="pagina-publica-hero-inner">
          <div className="breadcrumb-nav">
            <Link to="/">Inicio</Link>
            <span>/</span>
            <span className="breadcrumb-actual">Simulador Visual de Empatía</span>
          </div>
          <span className="hero-pill-badge">👁️ Experiencia de Concienciación e Inclusión</span>
          <h1 className="pagina-hero-titulo">
            Simulador Interactivo de <span className="texto-destacado-verde">Empatía Visual</span>
          </h1>
          <p className="pagina-hero-subtitulo">
            Comprende en primera persona cómo perciben el entorno las personas con distintas condiciones visuales.
            La prevención oftalmológica oportuna y la empatía ciudadana salvan vidas y construyen una sociedad accesible.
          </p>
        </div>
      </section>

      {/* Cuerpo Interactivo del Simulador */}
      <section className="seccion-simulador-dedicada">
        <div className="simulador-container">
          {/* Controles de Selección de Patología */}
          <div className="selector-patologias-wrapper">
            <label className="selector-etiqueta">Selecciona una condición ocular:</label>
            <div className="patologias-grid-botones">
              <button
                type="button"
                className={`btn-patologia ${condicion === "normal" ? "activa" : ""}`}
                onClick={() => setCondicion("normal")}
              >
                <span className="patologia-icono">👁️</span>
                <span className="patologia-nombre">Visión Normal</span>
              </button>
              <button
                type="button"
                className={`btn-patologia ${condicion === "cataratas" ? "activa" : ""}`}
                onClick={() => setCondicion("cataratas")}
              >
                <span className="patologia-icono">🌫️</span>
                <span className="patologia-nombre">Cataratas</span>
              </button>
              <button
                type="button"
                className={`btn-patologia ${condicion === "glaucoma" ? "activa" : ""}`}
                onClick={() => setCondicion("glaucoma")}
              >
                <span className="patologia-icono">🌑</span>
                <span className="patologia-nombre">Glaucoma (Túnel)</span>
              </button>
              <button
                type="button"
                className={`btn-patologia ${condicion === "retinopatia" ? "activa" : ""}`}
                onClick={() => setCondicion("retinopatia")}
              >
                <span className="patologia-icono">🩸</span>
                <span className="patologia-nombre">Retinopatía Diabética</span>
              </button>
              <button
                type="button"
                className={`btn-patologia ${condicion === "baja-vision" ? "activa" : ""}`}
                onClick={() => setCondicion("baja-vision")}
              >
                <span className="patologia-icono">👓</span>
                <span className="patologia-nombre">Baja Visión Severa</span>
              </button>
            </div>

            {/* Selector de Severidad y Escena */}
            {condicion !== "normal" && (
              <div className="simulador-ajustes-fila">
                <div className="control-severidad">
                  <div className="severidad-header">
                    <span>Nivel de avance / severidad:</span>
                    <strong>{severidad}% ({severidad < 40 ? "Leve" : severidad < 75 ? "Moderada" : "Avanzada"})</strong>
                  </div>
                  <input
                    type="range"
                    min="15"
                    max="100"
                    value={severidad}
                    onChange={(e) => setSeveridad(Number(e.target.value))}
                    className="slider-severidad"
                    aria-label="Ajustar severidad de la condición simulada"
                  />
                </div>

                <div className="control-escena">
                  <span>Escenario de prueba:</span>
                  <div className="escena-botones-toggle">
                    <button
                      type="button"
                      className={`btn-toggle ${escena === "calle" ? "activo" : ""}`}
                      onClick={() => setEscena("calle")}
                    >
                      🏙️ Cruce Peatonal
                    </button>
                    <button
                      type="button"
                      className={`btn-toggle ${escena === "lectura" ? "activo" : ""}`}
                      onClick={() => setEscena("lectura")}
                    >
                      📖 Lectura y Letrero
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Visor de Simulación Visual */}
          <div className="visor-simulacion-card">
            <div className="visor-marco">
              <div className="visor-pantalla-area" style={getSimuladorStyle()}>
                {escena === "calle" ? (
                  <div className="escena-interactiva-calle">
                    <div className="cielo-urbano">
                      <div className="sol-radiante-animado" />
                      <div className="nube nube-1">☁️</div>
                      <div className="nube nube-2">☁️</div>
                    </div>
                    <div className="horizonte-edificios">
                      <div className="edificio ed-hospital">
                        <span>🏥 HOSPITAL</span>
                      </div>
                      <div className="edificio ed-banco">
                        <span>🏛️ PATRONATO</span>
                      </div>
                      <div className="edificio ed-tienda">
                        <span>🏪 COLMADO</span>
                      </div>
                    </div>
                    <div className="calle-asfalto">
                      <div className="paso-cebra-rayas" />
                      <div className="senial-transito">
                        <span className="senial-icono">🚸</span>
                        <span>PASO PEATONAL</span>
                      </div>
                      <div className="peaton-animado">
                        <span className="peaton-icono">🧑‍🦯</span>
                        <span className="peaton-etiqueta">Persona con Bastón</span>
                      </div>
                      <div className="vehiculo-animado">
                        <span className="vehiculo-icono">🚗</span>
                      </div>
                      <div className="semaforo-decorativo">
                        <div className="luz luz-roja activa" />
                        <div className="luz luz-amarilla" />
                        <div className="luz luz-verde" />
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="escena-interactiva-lectura">
                    <div className="receta-medica-simulada">
                      <div className="receta-encabezado">
                        <strong>RECETA OFTALMOLÓGICA · PATRONATO NACIONAL DE CIEGOS</strong>
                        <span>Fecha: {FECHA_HOY}</span>
                      </div>
                      <div className="receta-cuerpo">
                        <p><strong>Paciente:</strong> María Encarnación · 58 años</p>
                        <p><strong>Diagnóstico:</strong> Presión intraocular elevada OD 24 mmHg / OI 22 mmHg</p>
                        <p><strong>Tratamiento:</strong> Gotas hipotensoras 1 gota cada 12 horas en ambos ojos.</p>
                        <div className="receta-alerta">
                          ⚠️ IMPORTANTE: No suspenda las gotas. Su próxima cita de control de campo visual es el día 25.
                        </div>
                      </div>
                      <div className="cartel-senalitica">
                        <span>SALIDA DE EMERGENCIA ➔</span>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              {/* Capa de Máscara de Glaucoma (Efecto Túnel Negro Periférico) */}
              {condicion === "glaucoma" && (
                <div
                  className="mascara-tunel-glaucoma"
                  style={{
                    background: `radial-gradient(circle at 50% 50%, transparent ${Math.max(
                      12,
                      55 - (severidad / 100) * 45
                    )}%, rgba(10, 15, 29, 0.98) ${Math.max(25, 75 - (severidad / 100) * 45)}%)`,
                  }}
                  aria-hidden="true"
                />
              )}

              {/* Capa de Escotomas de Retinopatía (Hemorragias y Manchas Negras) */}
              {condicion === "retinopatia" && (
                <div className="mascara-escotomas-retina" aria-hidden="true">
                  <div className="escotoma escotoma-1" style={{ opacity: severidad / 100 }} />
                  <div className="escotoma escotoma-2" style={{ opacity: (severidad / 100) * 0.9 }} />
                  <div className="escotoma escotoma-3" style={{ opacity: (severidad / 100) * 0.8 }} />
                  <div className="escotoma escotoma-4" style={{ opacity: (severidad / 100) * 0.95 }} />
                </div>
              )}

              {/* Capa de Escotoma Central de Degeneración Macular (Baja Visión) */}
              {condicion === "baja-vision" && (
                <div
                  className="mascara-escotoma-central"
                  style={{
                    opacity: (severidad / 100) * 0.92,
                  }}
                  aria-hidden="true"
                />
              )}
            </div>

            {/* Ficha Médica y Didáctica */}
            <div className="ficha-didactica-medica">
              <div className="ficha-header">
                <div>
                  <span className="ficha-badge">{info.nombre}</span>
                  <h3 className="ficha-subtitulo">{info.subtitulo}</h3>
                </div>
                <div className="ficha-estado-pill">
                  {condicion === "normal" ? "🟢 Ojo Saludable" : "🟡 Requiere Valoración"}
                </div>
              </div>

              <p className="ficha-descripcion">{info.descripcion}</p>

              <div className="ficha-detalles-grid">
                <div className="ficha-col-sintomas">
                  <strong>Síntomas característicos:</strong>
                  <ul>
                    {info.sintomas.map((s, idx) => (
                      <li key={idx}>• {s}</li>
                    ))}
                  </ul>
                </div>
                <div className="ficha-col-prevencion">
                  <strong>Pautas de prevención y control:</strong>
                  <p>{info.prevencion}</p>
                  <div className="ficha-consejo-box">
                    <strong>💡 Recomendación del Patronato:</strong>
                    <p>{info.recomendacion}</p>
                  </div>
                </div>
              </div>

              <div className="ficha-cta-fila">
                <Link to="/agendar-cita?servicio=3" className="btn btn-pnc-green">
                  <span>📅 Agendar Examen Preventivo en el Patronato</span>
                </Link>
                <Link to="/servicios" className="btn btn-secundario">
                  <span>Ver Todos los Servicios Médicos</span>
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>

      <PublicFooter />
      <FloatingHelpButton />
    </div>
  );
}
