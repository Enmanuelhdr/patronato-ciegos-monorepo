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
  { ruta: "/panel/usuarios", etiqueta: "Usuarios del Core", icono: "👥", permiso: "core-users.ver" },
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
