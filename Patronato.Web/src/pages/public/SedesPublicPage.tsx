import { Link } from "react-router-dom";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import { FloatingHelpButton } from "../../components/FloatingHelpButton";

const SEDES_DETALLADAS = [
  {
    id: 1,
    nombre: "Sede Central Santo Domingo",
    region: "Distrito Nacional · Sede Principal",
    direccion: "Av. Dr. Bernardo Correa y Cidrón Esq. Huáscar Tejeda, Zona Universitaria (Próximo a la UASD y estación del Metro Amín Abel Hasbún).",
    telefonos: ["(809) 508-7511", "(809) 533-2833", "(809) 533-1811"],
    correo: "info@patronatociegos.org.do",
    horario: "Lunes a Viernes: 8:00 AM – 4:00 PM",
    servicios: [
      "Consultas oftalmológicas generales y especializadas",
      "Centro de Masajes TACTO (Cabinas climatizadas)",
      "Rehabilitación integral: Braille, Bastón Blanco y AVD",
      "Laboratorio de Informática Adaptada (JAWS, NVDA)",
      "Talleres de confección de trapeadores y artesanías",
      "Fondo rotatorio de microcréditos para emprendedores",
      "Biblioteca y hemeroteca Braille con audiolibros",
    ],
    mapaUrl: "https://maps.google.com/?q=Patronato+Nacional+de+Ciegos+Santo+Domingo",
    fotoBadge: "🏛️",
  },
  {
    id: 2,
    nombre: "Filial Santiago de los Caballeros",
    region: "Región del Cibao",
    direccion: "Av. Juan Pablo Duarte, Sector Villa Olga, Santiago de los Caballeros.",
    telefonos: ["(809) 575-9735", "(809) 575-9736"],
    correo: "filialsantiago@patronatociegos.org.do",
    horario: "Lunes a Viernes: 8:00 AM – 3:30 PM",
    servicios: [
      "Consultas preventivas de salud ocular y refracción",
      "Rehabilitación funcional y técnicas de orientación y movilidad",
      "Centro de Masajes TACTO Filial Cibao",
      "Talleres artesanales y de economía circular",
      "Asistencia psicológica y trabajo social familiar",
    ],
    mapaUrl: "https://maps.google.com/?q=Patronato+Nacional+de+Ciegos+Santiago",
    fotoBadge: "🏢",
  },
  {
    id: 3,
    nombre: "Filial Barahona",
    region: "Región Suroeste",
    direccion: "Calle Duarte No. 42, Casco Urbano, Barahona.",
    telefonos: ["(809) 524-2210"],
    correo: "filialbarahona@patronatociegos.org.do",
    horario: "Lunes a Viernes: 8:00 AM – 2:30 PM",
    servicios: [
      "Operativos móviles comunitarios de detección de catarata y glaucoma",
      "Capacitación básica en bastón blanco y sistema Braille",
      "Canalización de ayudas técnicas y lentes correctivos",
      "Acompañamiento a comunidades rurales del Sur profundo",
    ],
    mapaUrl: "https://maps.google.com/?q=Patronato+Nacional+de+Ciegos+Barahona",
    fotoBadge: "🌴",
  },
];

export function SedesPublicPage() {
  return (
    <div className="landing-moderna">
      <PublicNavbar paginaActiva="sedes" />

      {/* Hero Dedicado de Sedes */}
      <section className="pagina-publica-hero">
        <div className="pagina-publica-hero-inner">
          <div className="breadcrumb-nav">
            <Link to="/">Inicio</Link>
            <span>/</span>
            <span className="breadcrumb-actual">Sedes Nacionales</span>
          </div>
          <span className="hero-pill-badge">🇩🇴 Presencia en Todo el Territorio Nacional</span>
          <h1 className="pagina-hero-titulo">
            Nuestras Sedes <span className="texto-destacado-verde">e Instalaciones de Servicio</span>
          </h1>
          <p className="pagina-hero-subtitulo">
            Contamos con sedes preparadas con accesibilidad universal, cabinas de masajes, consultorios médicos
            y aulas de capacitación en Santo Domingo, Santiago y la región Sur para recibirte con calidez humana.
          </p>
        </div>
      </section>

      {/* Listado de Sedes */}
      <section className="seccion-sedes-dedicada">
        <div className="sedes-container">
          <div className="sedes-grid-tarjetas">
            {SEDES_DETALLADAS.map((sede) => (
              <div key={sede.id} className="sede-tarjeta-card">
                <div className="sede-card-top">
                  <div className="sede-icono-grande">{sede.fotoBadge}</div>
                  <div>
                    <span className="sede-region-tag">{sede.region}</span>
                    <h2 className="sede-nombre-titulo">{sede.nombre}</h2>
                  </div>
                </div>

                <div className="sede-card-cuerpo">
                  <div className="sede-dato-bloque">
                    <span className="sede-etiqueta">📍 Dirección:</span>
                    <p className="sede-valor">{sede.direccion}</p>
                  </div>

                  <div className="sede-dato-bloque">
                    <span className="sede-etiqueta">📞 Teléfonos Directos:</span>
                    <div className="sede-telefonos-lista">
                      {sede.telefonos.map((tel, idx) => (
                        <a key={idx} href={`tel:${tel.replace(/\D/g, "")}`} className="sede-tel-link">
                          {tel}
                        </a>
                      ))}
                    </div>
                  </div>

                  <div className="sede-dato-bloque">
                    <span className="sede-etiqueta">🕒 Horario de Atención:</span>
                    <p className="sede-valor">{sede.horario}</p>
                  </div>

                  <div className="sede-servicios-caja">
                    <strong className="sede-servicios-titulo">Servicios disponibles en esta sede:</strong>
                    <ul className="sede-servicios-lista">
                      {sede.servicios.map((s, idx) => (
                        <li key={idx}>
                          <span className="check-azul">✓</span>
                          <span>{s}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>

                <div className="sede-card-acciones">
                  <Link
                    to={`/agendar-cita?sede=${sede.id}`}
                    className="btn btn-pnc-green btn-block"
                  >
                    <span>📅 Agendar Cita en {sede.nombre.split(" ")[1] ?? "esta Sede"}</span>
                  </Link>
                  <a
                    href={sede.mapaUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="btn btn-secundario btn-block"
                  >
                    <span>🗺️ Cómo Llegar (Google Maps)</span>
                  </a>
                </div>
              </div>
            ))}
          </div>

          {/* Banner de Accesibilidad y Transporte */}
          <div className="banner-accesibilidad-sedes">
            <div className="banner-acc-icon">🚇</div>
            <div className="banner-acc-textos">
              <h4>Accesibilidad y Transporte Público</h4>
              <p>
                Nuestra Sede Central en Santo Domingo cuenta con franjas podotáctiles en aceras conectadas directamente con la Estación Amín Abel Hasbún de la Línea 1 del Metro de Santo Domingo, facilitando el acceso autónomo de beneficiarios invidentes.
              </p>
            </div>
            <Link to="/agendar-cita" className="btn btn-pnc-primary">
              <span>Solicitar Acompañamiento</span>
            </Link>
          </div>
        </div>
      </section>

      <PublicFooter />
      <FloatingHelpButton />
    </div>
  );
}
