import { useState } from "react";
import { Link } from "react-router-dom";

export interface TickerItem {
  id: string;
  categoria: string;
  titulo: string;
  descripcion: string;
  impacto: string;
  enlace: string;
  colorGradiente: string;
  icono: string;
  imagenUrl: string;
  entidad: string;
}

export const ACTIVIDADES_TICKER: TickerItem[] = [
  {
    id: "movilidad",
    categoria: "Rehabilitación Urbana",
    titulo: "Movilidad Autónoma con Bastón Blanco",
    descripcion: "Entrenamiento en técnicas de rastreo, localización espacial, cruce de calles y desplazamiento seguro e independiente en toda la República Dominicana.",
    impacto: "Más de 500 beneficiarios desplazándose con autonomía",
    enlace: "/como-funciona",
    colorGradiente: "linear-gradient(135deg, #701a75 0%, #86198f 50%, #c026d3 100%)",
    icono: "🦯",
    imagenUrl: "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?auto=format&fit=crop&w=800&q=80",
    entidad: "Alcaldía D.N. & Patronato PNC",
  },
  {
    id: "conadis",
    categoria: "Alianza Gubernamental",
    titulo: "Certificación y Carnetización CONADIS",
    descripcion: "Convenio con el Consejo Nacional de Discapacidad para registro oficial, carnetización con código QR, exoneración en el Metro/OMSA y acceso a derechos.",
    impacto: "Convenio Marco Estado Dominicano & PNC",
    enlace: "/como-funciona",
    colorGradiente: "linear-gradient(135deg, #1e3a8a 0%, #1e40af 50%, #0284c7 100%)",
    icono: "🏛️",
    imagenUrl: "https://images.unsplash.com/photo-1577495508048-b635879837f1?auto=format&fit=crop&w=800&q=80",
    entidad: "CONADIS • República Dominicana",
  },
  {
    id: "oftalmologia",
    categoria: "Salud Pública Preventiva",
    titulo: "Operativos Oftalmológicos Nacionales",
    descripcion: "Jornadas médicas conjuntas con el Ministerio de Salud Pública (MISPAS) y el Servicio Nacional de Salud (SNS) para diagnóstico temprano y prevención de ceguera.",
    impacto: "+85 consultas mensuales en sedes y operativos barriales",
    enlace: "/agendar-cita",
    colorGradiente: "linear-gradient(135deg, #0f766e 0%, #0e7490 50%, #0284c7 100%)",
    icono: "👁️",
    imagenUrl: "https://images.unsplash.com/photo-1579684385127-1ef15d508118?auto=format&fit=crop&w=800&q=80",
    entidad: "MISPAS / SNS Dominicano",
  },
  {
    id: "superate",
    categoria: "Políticas Sociales del Estado",
    titulo: "Inclusión y Bonos Supérate Discapacidad",
    descripcion: "Articulación con el Gabinete de Políticas Sociales de la Presidencia de la República para subsidios alimentarios, bonos de vivienda y acompañamiento integral.",
    impacto: "Protección social para familias de beneficiarios",
    enlace: "/impacto",
    colorGradiente: "linear-gradient(135deg, #065f46 0%, #047857 50%, #059669 100%)",
    icono: "🤝",
    imagenUrl: "https://images.unsplash.com/photo-1531206715517-5c0ba140b2b8?auto=format&fit=crop&w=800&q=80",
    entidad: "Supérate • Presidencia de la República",
  },
  {
    id: "tacto",
    categoria: "Inserción Laboral",
    titulo: "Masajes TACTO Terapéuticos",
    descripcion: "Terapeutas no videntes certificados internacionalmente brindando masajes descontracturantes y relajantes con contratos en instituciones del Estado y privadas.",
    impacto: "Más de 70 masajistas graduados con empleo formal",
    enlace: "/servicios",
    colorGradiente: "linear-gradient(135deg, #134e4a 0%, #0f766e 50%, #14b8a6 100%)",
    icono: "💆‍♂️",
    imagenUrl: "https://images.unsplash.com/photo-1544161515-4ab6ce6db874?auto=format&fit=crop&w=800&q=80",
    entidad: "Centro de Masajes TACTO",
  },
  {
    id: "braille",
    categoria: "Educación Táctil",
    titulo: "Alfabetización en Sistema Braille",
    descripcion: "Enseñanza de lectura y signografía táctil con regleta, punzón, máquinas Perkins y ábaco Crammer para cálculo matemático.",
    impacto: "Biblioteca adaptada con +40 títulos dominicanos",
    enlace: "/servicios",
    colorGradiente: "linear-gradient(135deg, #991b1b 0%, #b91c1c 50%, #ea580c 100%)",
    icono: "📖",
    imagenUrl: "https://images.unsplash.com/photo-1544717305-2782549b5136?auto=format&fit=crop&w=800&q=80",
    entidad: "Biblioteca Inclusiva PNC",
  },
  {
    id: "minerd",
    categoria: "Educación Inclusiva",
    titulo: "Libros en Braille y Macrotipos para Escuelas",
    descripcion: "Producción y transcripción de libros de texto y guías de estudio escolar en convenio con el Ministerio de Educación (MINERD) para escuelas de todo el país.",
    impacto: "Materiales adaptados en los 122 distritos escolares",
    enlace: "/servicios",
    colorGradiente: "linear-gradient(135deg, #1e1b4b 0%, #312e81 50%, #4338ca 100%)",
    icono: "🏫",
    imagenUrl: "https://images.unsplash.com/photo-1509062522246-3755977927d7?auto=format&fit=crop&w=800&q=80",
    entidad: "MINERD • Educación Dominicana",
  },
  {
    id: "suapers",
    categoria: "Taller Protegido Ecológico",
    titulo: "Suapers de Algodón 100% Reciclado",
    descripcion: "Producción de trapeadores ecológicos elaborados por personas con discapacidad visual con hilazas recicladas, abasteciendo dependencias del Estado.",
    impacto: "100% Sostenible y Economía Circular Dominicana",
    enlace: "/productos",
    colorGradiente: "linear-gradient(135deg, #1e3a8a 0%, #1d4ed8 50%, #0284c7 100%)",
    icono: "🌱",
    imagenUrl: "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=800&q=80",
    entidad: "Taller Protegido de Producción PNC",
  },
  {
    id: "computacion",
    categoria: "Tiflotecnología Digital",
    titulo: "Cómputo con Lectores NVDA y JAWS",
    descripcion: "Capacitación en uso de herramientas ofimáticas, navegación web accesible e inteligencia artificial adaptada para empleo profesional calificado.",
    impacto: "Aulas informáticas en Santo Domingo y Santiago",
    enlace: "/servicios",
    colorGradiente: "linear-gradient(135deg, #312e81 0%, #4338ca 50%, #6366f1 100%)",
    icono: "💻",
    imagenUrl: "https://images.unsplash.com/photo-1531482615713-2afd69097998?auto=format&fit=crop&w=800&q=80",
    entidad: "Aulas Tecnológicas Inclusivas",
  },
  {
    id: "infantil",
    categoria: "Atención Temprana",
    titulo: "Estimulación Visual y Desarrollo Infantil",
    descripcion: "Acompañamiento a niños y niñas desde la primera infancia junto a sus familias para potenciar el remanente visual y el desarrollo psicomotor.",
    impacto: "Atención integral a familias y centros educativos",
    enlace: "/como-funciona",
    colorGradiente: "linear-gradient(135deg, #831843 0%, #9d174d 50%, #db2777 100%)",
    icono: "🧸",
    imagenUrl: "https://images.unsplash.com/photo-1485546246426-74dc88dec4d9?auto=format&fit=crop&w=800&q=80",
    entidad: "Unidad Infantil & Familia PNC",
  },
];

export function ImageTicker() {
  const [actividadSeleccionada, setActividadSeleccionada] = useState<TickerItem | null>(null);

  // Duplicamos el array para lograr un scroll infinito continuo y fluido
  const trackItems = [...ACTIVIDADES_TICKER, ...ACTIVIDADES_TICKER];

  return (
    <section className="ticker-section" aria-label="Actividades, Alianzas Estatales y Programas del Patronato">
      <div className="ticker-header">
        <span className="ticker-eyebrow">
          <span>🇩🇴 Alianzas con el Estado y la Sociedad Civil</span>
        </span>
        <h2 className="ticker-titulo">Inclusión Social, Rehabilitación y Compromiso Nacional</h2>
        <p className="ticker-subtitulo">
          Conoce cómo el Patronato Nacional de Ciegos une esfuerzos con CONADIS, Presidencia, MISPAS y MINERD para transformar la vida de personas con discapacidad visual en República Dominicana.
        </p>
      </div>

      <div className="ticker-marquee-wrapper">
        <div className="ticker-track">
          {trackItems.map((actividad, index) => (
            <article
              key={`${actividad.id}-${index}`}
              className="ticker-card"
              onClick={() => setActividadSeleccionada(actividad)}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  setActividadSeleccionada(actividad);
                }
              }}
            >
              <div className="ticker-card-img-wrap">
                <img
                  src={actividad.imagenUrl}
                  alt={actividad.titulo}
                  className="ticker-card-bg-glow"
                  loading="lazy"
                  onError={(e) => {
                    // Fallback visual si la red bloquea la imagen externa
                    (e.target as HTMLElement).style.display = "none";
                  }}
                />
                <div className="ticker-card-overlay" />
                <span className="ticker-badge-categoria">{actividad.categoria}</span>
                <span className="ticker-badge-entidad">{actividad.entidad}</span>
                <div className="ticker-floating-icon">
                  {actividad.icono}
                </div>
              </div>

              <div className="ticker-card-body">
                <h3 className="ticker-card-title">{actividad.titulo}</h3>
                <p className="ticker-card-desc">{actividad.descripcion}</p>
                <div className="ticker-card-footer">
                  <span className="ticker-card-stat">⭐ {actividad.impacto}</span>
                  <span className="ticker-card-link">Ver más →</span>
                </div>
              </div>
            </article>
          ))}
        </div>
      </div>

      {/* Modal interactivo al hacer clic en una tarjeta del ticker */}
      {actividadSeleccionada && (
        <div
          role="dialog"
          aria-modal="true"
          style={{
            position: "fixed",
            inset: 0,
            background: "rgba(15, 23, 42, 0.8)",
            backdropFilter: "blur(8px)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 10000,
            padding: "20px",
          }}
          onClick={() => setActividadSeleccionada(null)}
        >
          <div
            style={{
              background: "#ffffff",
              borderRadius: "24px",
              maxWidth: "540px",
              width: "100%",
              overflow: "hidden",
              boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
              animation: "modal-fade-up 0.25s ease-out",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div
              style={{
                position: "relative",
                height: "220px",
                background: actividadSeleccionada.colorGradiente,
                overflow: "hidden",
                display: "flex",
                flexDirection: "column",
                justifyContent: "flex-end",
                padding: "20px 24px",
              }}
            >
              <img
                src={actividadSeleccionada.imagenUrl}
                alt={actividadSeleccionada.titulo}
                style={{
                  position: "absolute",
                  inset: 0,
                  width: "100%",
                  height: "100%",
                  objectFit: "cover",
                  opacity: 0.45,
                }}
              />
              <div
                style={{
                  position: "absolute",
                  inset: 0,
                  background: "linear-gradient(180deg, rgba(15,23,42,0.2) 0%, rgba(15,23,42,0.9) 100%)",
                }}
              />
              <button
                type="button"
                onClick={() => setActividadSeleccionada(null)}
                style={{
                  position: "absolute",
                  top: "16px",
                  right: "16px",
                  background: "rgba(255, 255, 255, 0.25)",
                  backdropFilter: "blur(6px)",
                  border: "none",
                  color: "white",
                  width: "36px",
                  height: "36px",
                  borderRadius: "50%",
                  cursor: "pointer",
                  fontSize: "18px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  zIndex: 2,
                }}
                aria-label="Cerrar modal"
              >
                ✕
              </button>

              <div style={{ position: "relative", zIndex: 1 }}>
                <span
                  style={{
                    display: "inline-block",
                    padding: "4px 10px",
                    background: "rgba(255, 255, 255, 0.2)",
                    backdropFilter: "blur(6px)",
                    borderRadius: "20px",
                    fontSize: "11px",
                    fontWeight: 700,
                    color: "#ffffff",
                    marginBottom: "8px",
                    textTransform: "uppercase",
                    letterSpacing: "0.5px",
                  }}
                >
                  {actividadSeleccionada.categoria} • {actividadSeleccionada.entidad}
                </span>
                <h3 style={{ fontSize: "20px", fontWeight: 800, color: "#ffffff", margin: 0, textShadow: "0 2px 6px rgba(0,0,0,0.5)" }}>
                  {actividadSeleccionada.icono} {actividadSeleccionada.titulo}
                </h3>
              </div>
            </div>

            <div style={{ padding: "24px 28px" }}>
              <p style={{ color: "#334155", fontSize: "14px", lineHeight: "1.6", marginBottom: "18px" }}>
                {actividadSeleccionada.descripcion}
              </p>

              <div
                style={{
                  background: "#f8fafc",
                  borderLeft: "4px solid #0284c7",
                  padding: "12px 16px",
                  borderRadius: "0 8px 8px 0",
                  marginBottom: "22px",
                }}
              >
                <span style={{ fontSize: "12px", color: "#64748b", fontWeight: 600, display: "block" }}>
                  Impacto en la Comunidad:
                </span>
                <strong style={{ fontSize: "14px", color: "#0369a1" }}>
                  ⭐ {actividadSeleccionada.impacto}
                </strong>
              </div>

              <div style={{ display: "flex", gap: "10px" }}>
                <Link
                  to={actividadSeleccionada.enlace}
                  className="btn btn-pnc-primary"
                  style={{ flex: 1, textAlign: "center", textDecoration: "none" }}
                  onClick={() => setActividadSeleccionada(null)}
                >
                  Conocer Más del Programa
                </Link>
                <button
                  type="button"
                  className="btn btn-pnc-outline"
                  onClick={() => setActividadSeleccionada(null)}
                >
                  Cerrar
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
