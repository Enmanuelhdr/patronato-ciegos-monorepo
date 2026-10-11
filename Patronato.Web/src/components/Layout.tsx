import { Link, NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

interface ItemNav {
  ruta: string;
  etiqueta: string;
  icono: string;
  roles?: string[]; // sin roles = visible para cualquier rol autenticado
}

const NAV: ItemNav[] = [
  { ruta: "/panel", etiqueta: "Panel General", icono: "📊" },
  { ruta: "/panel/beneficiarios", etiqueta: "Beneficiarios", icono: "🧑‍🦯" },
  { ruta: "/panel/consultas", etiqueta: "Consultas Oftalmológicas", icono: "👁️", roles: ["Administrador", "Médico Oftalmólogo", "Recepcionista"] },
  { ruta: "/panel/rehabilitacion", etiqueta: "Rehabilitación", icono: "📚", roles: ["Administrador", "Instructor Rehabilitación", "Recepcionista"] },
  { ruta: "/panel/masajes", etiqueta: "Masajes TACTO", icono: "💆", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/prestamos", etiqueta: "Préstamos de Emprendimiento", icono: "💳", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/ordenes", etiqueta: "Órdenes de Pedido", icono: "📋", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/donaciones", etiqueta: "Donaciones", icono: "🎁", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/voluntarios", etiqueta: "Voluntarios", icono: "🤝", roles: ["Administrador", "Recepcionista"] },
  { ruta: "/panel/transacciones", etiqueta: "Transacciones de Caja", icono: "🧾", roles: ["Administrador"] },
  { ruta: "/panel/sucursales", etiqueta: "Sucursales", icono: "🏢", roles: ["Administrador"] },
  { ruta: "/panel/usuarios", etiqueta: "Empleados y Usuarios", icono: "👥", roles: ["Administrador"] },
];

export function Layout() {
  const { sesion, logout } = useAuth();
  const items = NAV.filter((item) => !item.roles || (sesion && item.roles.includes(sesion.rol)));

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-marca">
          <div className="sidebar-escudo">PNC</div>
          <div>
            <strong>Patronato Nacional</strong>
            <span>de Ciegos</span>
          </div>
        </div>
        <nav>
          {items.map((item) => (
            <NavLink key={item.ruta} to={item.ruta} end={item.ruta === "/panel"} className={({ isActive }) => `nav-item${isActive ? " activo" : ""}`}>
              <span>{item.icono}</span> {item.etiqueta}
            </NavLink>
          ))}
        </nav>
        <Link to="/" className="nav-item" style={{ marginTop: "auto", borderTop: "1px solid rgba(255,255,255,0.12)" }}>
          <span>🌐</span> Ver sitio público
        </Link>
      </aside>

      <div className="contenido">
        <header className="topbar">
          <span className="topbar-origen">Conectado al Gateway · nunca directo al Core</span>
          <div className="topbar-usuario">
            <div>
              <strong>{sesion?.nombre}</strong>
              <span>{sesion?.rol}</span>
            </div>
            <button className="btn btn-secundario" onClick={logout}>
              Cerrar sesión
            </button>
          </div>
        </header>
        <main>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
