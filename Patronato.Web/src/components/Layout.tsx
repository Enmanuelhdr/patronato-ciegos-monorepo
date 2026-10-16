import { Link, NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

interface ItemNav {
  ruta: string;
  etiqueta: string;
  icono: string;
  roles?: string[]; // guardia por nombre de rol (pantallas clínicas ya existentes)
  permiso?: string | string[]; // guardia por permiso granular (RBAC nuevo)
}

const NAV: ItemNav[] = [
  { ruta: "/panel", etiqueta: "Panel General", icono: "📊" },
  { ruta: "/panel/citas", etiqueta: "Solicitudes de Cita", icono: "🗓️", roles: ["Administrador", "Recepcionista", "MedicoOftalmologo", "InstructorRehabilitacion"] },
  { ruta: "/panel/beneficiarios", etiqueta: "Beneficiarios", icono: "🧑‍🦯" },
  // roles en el formato plano del nombre real de CoreRole (ver Patronato.Core/Entities/CoreRole.cs):
  // es lo que trae sesion.rol desde el login/JWT del Core.
  { ruta: "/panel/consultas", etiqueta: "Consultas Oftalmológicas", icono: "👁️", roles: ["Administrador", "MedicoOftalmologo", "Recepcionista"] },
  { ruta: "/panel/rehabilitacion", etiqueta: "Rehabilitación", icono: "📚", roles: ["Administrador", "InstructorRehabilitacion", "Recepcionista"] },
  { ruta: "/panel/masajes", etiqueta: "Masajes TACTO", icono: "💆", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/prestamos", etiqueta: "Préstamos de Emprendimiento", icono: "💳", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/ordenes", etiqueta: "Órdenes de Pedido", icono: "📋", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/donaciones", etiqueta: "Donaciones", icono: "🎁", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/voluntarios", etiqueta: "Voluntarios", icono: "🤝", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/transacciones", etiqueta: "Transacciones de Caja", icono: "🧾", roles: ["Administrador"] },
  { ruta: "/panel/sucursales", etiqueta: "Sucursales", icono: "🏢", roles: ["Administrador"] },
  { ruta: "/panel/inventario", etiqueta: "Inventario", icono: "📦", permiso: "inventario.ver" },
  { ruta: "/panel/accesos-web", etiqueta: "Accesos Web", icono: "🌐", permiso: "web-usuarios.ver" },
  { ruta: "/panel/usuarios", etiqueta: "Personal y Usuarios", icono: "👥", permiso: "core-users.ver" },
];

export function Layout() {
  const { sesion, logout, tienePermiso } = useAuth();
  const items = NAV.filter((item) => {
    if (item.roles && item.roles.length > 0 && !(sesion && item.roles.includes(sesion.rol))) return false;
    if (item.permiso && !tienePermiso(item.permiso)) return false;
    return true;
  });

  return (
    <div className="layout">
      {/* WCAG 2.2: Enlace de salto para usuarios de teclado y lectores de pantalla */}
      <a href="#main-content" className="skip-link">
        Saltar al contenido principal
      </a>

      <aside className="sidebar" aria-label="Barra lateral">
        <div className="sidebar-marca">
          <div className="sidebar-logo-card">
            <img
              src="/logo-pnc.png"
              alt="Logo Oficial Patronato Nacional de Ciegos"
              className="sidebar-logo-img"
            />
          </div>
          <div>
            <strong style={{ fontSize: 13, lineHeight: 1.2 }}>Patronato Nacional</strong>
            <span style={{ fontSize: 11, color: "#93c5fd" }}>de Ciegos · 1964</span>
          </div>
        </div>
        <nav aria-label="Navegación principal">
          {items.map((item) => (
            <NavLink
              key={item.ruta}
              to={item.ruta}
              end={item.ruta === "/panel"}
              className={({ isActive }) => `nav-item${isActive ? " activo" : ""}`}
            >
              <span aria-hidden="true">{item.icono}</span>
              <span>{item.etiqueta}</span>
            </NavLink>
          ))}
        </nav>
        <Link
          to="/"
          className="nav-item"
          style={{ marginTop: "auto", borderTop: "1px solid rgba(255,255,255,0.12)" }}
        >
          <span aria-hidden="true">🌐</span>
          <span>Ver sitio público</span>
        </Link>
      </aside>

      <div className="contenido">
        <header className="topbar">
          <span className="topbar-origen" role="status">
            <span style={{ color: "var(--verde)", fontWeight: "bold", marginRight: 6 }}>●</span>
            Sistema Institucional Conectado
          </span>
          <div className="topbar-usuario">
            <div>
              <strong>{sesion?.nombre}</strong>
              <span>{sesion?.rol}</span>
            </div>
            <button
              type="button"
              className="btn btn-secundario"
              onClick={logout}
              aria-label="Cerrar sesión actual"
            >
              Cerrar sesión
            </button>
          </div>
        </header>
        <main id="main-content" tabIndex={-1}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
