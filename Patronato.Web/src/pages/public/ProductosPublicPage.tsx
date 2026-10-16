import { useState, useMemo } from "react";
import { Link } from "react-router-dom";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import { FloatingHelpButton } from "../../components/FloatingHelpButton";

interface ProductoCatalogo {
  id: number;
  nombre: string;
  categoria: "limpieza" | "artesania" | "bienestar" | "tecnica";
  categoriaLabel: string;
  icono: string;
  precio: number;
  precioTexto: string;
  descripcion: string;
  impactoSocial: string;
  impactoAmbiental: string;
  disponibilidad: string;
}

const PRODUCTOS: ProductoCatalogo[] = [
  {
    id: 1,
    nombre: "Trapeador Ecológico PNC (Hilo 100% Reciclado)",
    categoria: "limpieza",
    categoriaLabel: "Limpieza Sostenible",
    icono: "🧹",
    precio: 180,
    precioTexto: "RD$ 180.00",
    descripcion:
      "Confeccionado artesanalmente por operarios invidentes utilizando hilazas de algodón 100% recicladas de la industria textil dominicana. Gran poder de absorción y durabilidad prolongada.",
    impactoSocial: "Genera empleo digno directo para más de 30 familias de beneficiarios.",
    impactoAmbiental: "Evita que 1.2 libras de residuo textil terminen en vertederos.",
    disponibilidad: "Entrega inmediata al por mayor y detalle",
  },
  {
    id: 2,
    nombre: "Desinfectante Multiuso Biodegradable (Pino & Eucalipto)",
    categoria: "limpieza",
    categoriaLabel: "Limpieza Sostenible",
    icono: "🧴",
    precio: 150,
    precioTexto: "RD$ 150.00 / Galón",
    descripcion:
      "Fórmula ecológica limpiadora concentrada formulada con tensioactivos biodegradables y aceites esenciales naturales. Libre de fosfatos y no tóxico para mascotas.",
    impactoSocial: "Elaborado y envasado en los talleres de capacitación laboral del Patronato.",
    impactoAmbiental: "100% biodegradable en aguas residuales con envase retornable.",
    disponibilidad: "Galones y bidones de 5 galones",
  },
  {
    id: 3,
    nombre: "Cesta y Bolso Tejido en Macramé",
    categoria: "artesania",
    categoriaLabel: "Artesanías en Fibras",
    icono: "🧺",
    precio: 650,
    precioTexto: "RD$ 650.00",
    descripcion:
      "Piezas decorativas y utilitarias tejidas a mano con hebras de yute y algodón natural. Diseños elegantes ideales para compras, decoración de interiores o regalos corporativos.",
    impactoSocial: "Ingreso íntegro para artesanas con discapacidad visual.",
    impactoAmbiental: "Reemplaza permanentemente el uso de fundas plásticas desechables.",
    disponibilidad: "Piezas únicas y pedidos por encargo",
  },
  {
    id: 4,
    nombre: "Cojín Ergonómico de Aromaterapia Relajante",
    categoria: "bienestar",
    categoriaLabel: "Bienestar & Salud",
    icono: "🌿",
    precio: 450,
    precioTexto: "RD$ 450.00",
    descripcion:
      "Confeccionado con retazos de telas seleccionadas y relleno de semillas de linaza y hierbas aromáticas dominicanas (albahaca, menta y eucalipto). Apto para calentar o enfriar.",
    impactoSocial: "Hecho a mano por el taller ocupacional de habilidades manuales.",
    impactoAmbiental: "Aprovecha al 100% recortes de tela de alta calidad de diseñadores locales.",
    disponibilidad: "En stock permanente",
  },
  {
    id: 5,
    nombre: "Bastón Blanco Plegable de 4 Tramos con Puntera Giratoria",
    categoria: "tecnica",
    categoriaLabel: "Ayudas Técnicas",
    icono: "🦯",
    precio: 950,
    precioTexto: "RD$ 950.00 (Tarifa Subsidiada)",
    descripcion:
      "Instrumento fundamental de orientación y movilidad para personas ciegas. Fabricado en aluminio aeronáutico ultraligero con cinta reflectante de alta visibilidad para la noche y mango ergonómico.",
    impactoSocial: "Proporciona independencia y seguridad en la vía pública.",
    impactoAmbiental: "Materiales de máxima durabilidad diseñados para resistir años de uso diario.",
    disponibilidad: "Diferentes medidas según estatura (110cm - 135cm)",
  },
  {
    id: 6,
    nombre: "Regleta y Punzón para Escritura Braille",
    categoria: "tecnica",
    categoriaLabel: "Ayudas Técnicas",
    icono: "📝",
    precio: 400,
    precioTexto: "RD$ 400.00",
    descripcion:
      "Regleta metálica de 4 líneas y 28 celdas con punzón de mango anatómico para escritura manual en sistema Braille sobre cartulina o papel especial.",
    impactoSocial: "Acceso básico a la alfabetización y la comunicación escrita universal.",
    impactoAmbiental: "Construcción en aluminio reciclable de vida útil indefinida.",
    disponibilidad: "En stock en nuestras tres sedes",
  },
  {
    id: 7,
    nombre: "Jabón Artesanal de Avena y Miel de Abejas",
    categoria: "bienestar",
    categoriaLabel: "Bienestar & Salud",
    icono: "🧼",
    precio: 120,
    precioTexto: "RD$ 120.00",
    descripcion:
      "Barra de jabón exfoliante suave enriquecida con avena integral y miel pura dominicana, saponificada en frío sin conservantes ni químicos agresivos.",
    impactoSocial: "Producido en el taller ocupacional para el desarrollo de la motricidad fina.",
    impactoAmbiental: "Empaque de papel kraft reciclado sin film plástico.",
    disponibilidad: "Cajas individuales y paquetes de 3 unidades",
  },
  {
    id: 8,
    nombre: "Portavasos y Manteles Individuales en Fibra de Plátano",
    categoria: "artesania",
    categoriaLabel: "Artesanías en Fibras",
    icono: "☕",
    precio: 350,
    precioTexto: "RD$ 350.00 (Set de 4)",
    descripcion:
      "Tejidos con fibras extraídas del vástago de la mata de plátano tras la cosecha en fincas del Cibao. Textura rústica y protección térmica para mesas.",
    impactoSocial: "Vínculo directo con agricultores y artesanos de la Filial Santiago.",
    impactoAmbiental: "Aprovechamiento de biomasa agrícola que usualmente se quema.",
    disponibilidad: "Set de 4 portavasos en caja artesanal",
  },
];

export function ProductosPublicPage() {
  const [categoria, setCategoria] = useState<string>("todos");
  const [busqueda, setBusqueda] = useState<string>("");
  const [productoSeleccionado, setProductoSeleccionado] = useState<ProductoCatalogo | null>(null);
  const [cantidad, setCantidad] = useState<number>(1);
  const [sedeEntrega, setSedeEntrega] = useState<string>("Sede Central Santo Domingo");

  const productosFiltrados = useMemo(() => {
    return PRODUCTOS.filter((p) => {
      const coincideCat = categoria === "todos" || p.categoria === categoria;
      const coincideBusqueda =
        busqueda.trim() === "" ||
        p.nombre.toLowerCase().includes(busqueda.toLowerCase()) ||
        p.descripcion.toLowerCase().includes(busqueda.toLowerCase()) ||
        p.categoriaLabel.toLowerCase().includes(busqueda.toLowerCase());
      return coincideCat && coincideBusqueda;
    });
  }, [categoria, busqueda]);

  return (
    <div className="landing-moderna">
      <PublicNavbar paginaActiva="productos" />

      {/* Hero Dedicado de Productos */}
      <section className="pagina-publica-hero">
        <div className="pagina-publica-hero-inner">
          <div className="breadcrumb-nav">
            <Link to="/">Inicio</Link>
            <span>/</span>
            <span className="breadcrumb-actual">Artesanías y Productos Ecológicos</span>
          </div>
          <span className="hero-pill-badge">🛍️ Calidad Hecha con el Corazón</span>
          <h1 className="pagina-hero-titulo">
            Catálogo de Productos <span className="texto-destacado-verde">Ecológicos y Artesanías</span>
          </h1>
          <p className="pagina-hero-subtitulo">
            Cada producto que compras apoya directamente el empleo digno de personas ciegas y promueve la economía circular en la República Dominicana.
            Disponibles para compras individuales y pedidos corporativos a gran escala.
          </p>
        </div>
      </section>

      {/* Sección del Catálogo con Filtros Reactivos */}
      <section className="seccion-catalogo-dedicada">
        <div className="catalogo-container">
          {/* Barra de Filtros y Búsqueda */}
          <div className="catalogo-toolbar">
            <div className="catalogo-categorias-pills">
              <button
                type="button"
                className={`btn-categoria-pill ${categoria === "todos" ? "activa" : ""}`}
                onClick={() => setCategoria("todos")}
              >
                Todos ({PRODUCTOS.length})
              </button>
              <button
                type="button"
                className={`btn-categoria-pill ${categoria === "limpieza" ? "activa" : ""}`}
                onClick={() => setCategoria("limpieza")}
              >
                🧹 Limpieza Ecológica
              </button>
              <button
                type="button"
                className={`btn-categoria-pill ${categoria === "artesania" ? "activa" : ""}`}
                onClick={() => setCategoria("artesania")}
              >
                🧺 Artesanías en Macramé
              </button>
              <button
                type="button"
                className={`btn-categoria-pill ${categoria === "bienestar" ? "activa" : ""}`}
                onClick={() => setCategoria("bienestar")}
              >
                🌿 Bienestar & Relax
              </button>
              <button
                type="button"
                className={`btn-categoria-pill ${categoria === "tecnica" ? "activa" : ""}`}
                onClick={() => setCategoria("tecnica")}
              >
                🦯 Ayudas Técnicas
              </button>
            </div>

            <div className="catalogo-buscador-input">
              <span className="buscador-icono" aria-hidden="true">🔍</span>
              <input
                type="search"
                placeholder="Buscar por producto, material o categoría..."
                value={busqueda}
                onChange={(e) => setBusqueda(e.target.value)}
                aria-label="Buscar producto en el catálogo"
              />
              {busqueda && (
                <button
                  type="button"
                  className="btn-limpiar-busqueda"
                  onClick={() => setBusqueda("")}
                >
                  ✕
                </button>
              )}
            </div>
          </div>

          {/* Grid de Productos */}
          <div className="productos-catalogo-grid">
            {productosFiltrados.map((p) => (
              <div key={p.id} className="producto-catalogo-card">
                <div className="producto-card-top">
                  <div className="producto-icono-grande" aria-hidden="true">{p.icono}</div>
                  <span className="producto-categoria-badge">{p.categoriaLabel}</span>
                </div>

                <div className="producto-card-cuerpo">
                  <h3 className="producto-titulo">{p.nombre}</h3>
                  <div className="producto-precio-destacado">{p.precioTexto}</div>
                  <p className="producto-descripcion">{p.descripcion}</p>

                  <div className="producto-impacto-pill-box">
                    <div className="impacto-linea">
                      <span className="impacto-icono-mini">🤝</span>
                      <span><strong>Impacto Social:</strong> {p.impactoSocial}</span>
                    </div>
                    <div className="impacto-linea">
                      <span className="impacto-icono-mini">🌱</span>
                      <span><strong>Impacto Eco:</strong> {p.impactoAmbiental}</span>
                    </div>
                  </div>
                </div>

                <div className="producto-card-acciones">
                  <button
                    type="button"
                    className="btn btn-pnc-green btn-block"
                    onClick={() => setProductoSeleccionado(p)}
                  >
                    <span>Pedir / Cotizar Producto</span>
                  </button>
                </div>
              </div>
            ))}
          </div>

          {productosFiltrados.length === 0 && (
            <div className="catalogo-vacio-box">
              <p style={{ fontSize: 32 }}>🔍</p>
              <h4>No se encontraron productos coincidentes</h4>
              <p>Intenta con otro término de búsqueda o selecciona "Todos" en las categorías.</p>
              <button
                type="button"
                className="btn btn-secundario"
                onClick={() => {
                  setCategoria("todos");
                  setBusqueda("");
                }}
              >
                Restablecer Filtros
              </button>
            </div>
          )}

          {/* Banner de Compras Corporativas B2B */}
          <div className="banner-compras-corporativas">
            <div className="banner-corp-textos">
              <span className="banner-tag">🏢 Responsabilidad Social Empresarial (RSE)</span>
              <h3>¿Tu empresa necesita suministros de limpieza con impacto social?</h3>
              <p>
                Abastece tu negocio, oficina o escuela con nuestros trapeadores y desinfectantes ecológicos.
                Generamos factura con comprobante fiscal (NCF), certificación de compra con impacto social y tarifas preferenciales por volumen.
              </p>
            </div>
            <div className="banner-corp-contacto">
              <a href="tel:8095087511" className="btn btn-pnc-primary">
                <span>📞 Cotización Corporativa: (809) 508-7511</span>
              </a>
              <Link to="/sedes" className="btn btn-pnc-outline-white">
                <span>Visitar Nuestras Sedes</span>
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Modal Interactivo de Compra / Pedido */}
      {productoSeleccionado && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal-contenido-pedido" style={{ maxWidth: 540 }}>
            <div className="modal-pedido-header">
              <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                <span style={{ fontSize: 32 }}>{productoSeleccionado.icono}</span>
                <div>
                  <h3 style={{ margin: 0, fontSize: 18 }}>{productoSeleccionado.nombre}</h3>
                  <span style={{ color: "var(--verde)", fontWeight: 800, fontSize: 15 }}>
                    {productoSeleccionado.precioTexto}
                  </span>
                </div>
              </div>
              <button
                type="button"
                className="btn-cerrar-modal"
                onClick={() => setProductoSeleccionado(null)}
              >
                ✕
              </button>
            </div>

            <div className="modal-pedido-body">
              <p style={{ margin: "0 0 16px", fontSize: 13, color: "#64748b" }}>
                {productoSeleccionado.descripcion}
              </p>

              {/* Selector interactivo de cantidad */}
              <div style={{ background: "#f8fafc", padding: "16px", borderRadius: 12, border: "1px solid #e2e8f0", marginBottom: 16 }}>
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
                  <span style={{ fontSize: 14, fontWeight: 700, color: "#1e293b" }}>Cantidad a ordenar:</span>
                  <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <button
                      type="button"
                      onClick={() => setCantidad(Math.max(1, cantidad - 1))}
                      style={{
                        width: 34,
                        height: 34,
                        borderRadius: 8,
                        border: "1px solid #cbd5e1",
                        background: "white",
                        fontWeight: 800,
                        fontSize: 16,
                        cursor: "pointer",
                      }}
                    >
                      −
                    </button>
                    <span style={{ fontSize: 16, fontWeight: 800, minWidth: 28, textAlign: "center" }}>
                      {cantidad}
                    </span>
                    <button
                      type="button"
                      onClick={() => setCantidad(cantidad + 1)}
                      style={{
                        width: 34,
                        height: 34,
                        borderRadius: 8,
                        border: "1px solid #cbd5e1",
                        background: "white",
                        fontWeight: 800,
                        fontSize: 16,
                        cursor: "pointer",
                      }}
                    >
                      +
                    </button>
                  </div>
                </div>

                {/* Sede de entrega / retiro */}
                <div>
                  <label htmlFor="sede-retiro-select" style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#1e293b", marginBottom: 6 }}>
                    Sede preferida de entrega o retiro:
                  </label>
                  <select
                    id="sede-retiro-select"
                    value={sedeEntrega}
                    onChange={(e) => setSedeEntrega(e.target.value)}
                    style={{
                      width: "100%",
                      padding: "8px 12px",
                      borderRadius: 8,
                      border: "1px solid #cbd5e1",
                      fontSize: 13,
                    }}
                  >
                    <option value="Sede Central Santo Domingo">Sede Central Santo Domingo (Zona Universitaria)</option>
                    <option value="Regional Norte Santiago">Regional Norte Santiago (Villa Olga)</option>
                    <option value="Regional Sur Barahona">Regional Sur Barahona (Jaime Mota)</option>
                    <option value="Envío a Domicilio / Mensajería">Envío a Domicilio / Empresa (A coordinar)</option>
                  </select>
                </div>

                {/* Resumen de cálculo en tiempo real */}
                <div style={{ marginTop: 16, paddingTop: 12, borderTop: "1px solid #e2e8f0", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <span style={{ fontSize: 14, fontWeight: 700, color: "#475569" }}>Total Estimado:</span>
                  <span style={{ fontSize: 20, fontWeight: 800, color: "var(--verde)" }}>
                    RD$ {(productoSeleccionado.precio * cantidad).toLocaleString("es-DO")}.00
                  </span>
                </div>
              </div>

              <div className="contacto-pedido-box" style={{ padding: "10px 14px", fontSize: 12 }}>
                <div>
                  <strong>🌱 Impacto Social Directo:</strong>
                  <p style={{ margin: 0 }}>{productoSeleccionado.impactoSocial}</p>
                </div>
              </div>
            </div>

            <div className="modal-pedido-footer" style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
              <button
                type="button"
                className="btn btn-secundario"
                onClick={() => setProductoSeleccionado(null)}
              >
                Cancelar
              </button>
              <a
                href={`https://wa.me/18095087511?text=${encodeURIComponent(
                  `Hola Patronato Nacional de Ciegos, deseo ordenar ${cantidad} unidad(es) de "${productoSeleccionado.nombre}" por un total de RD$ ${(productoSeleccionado.precio * cantidad).toLocaleString("es-DO")}.00 para retirar en: ${sedeEntrega}.`
                )}`}
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-pnc-green"
                style={{ display: "flex", alignItems: "center", gap: 6, fontWeight: 700 }}
              >
                <span>💬 Ordenar por WhatsApp Directo</span>
              </a>
            </div>
          </div>
        </div>
      )}

      <PublicFooter />
      <FloatingHelpButton />
    </div>
  );
}
