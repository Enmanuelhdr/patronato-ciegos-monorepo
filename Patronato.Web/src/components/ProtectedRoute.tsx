import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

// roles/permiso vacíos u omitidos = cualquier usuario autenticado puede entrar.
// "roles" sigue existiendo tal cual para las pantallas clínicas ya existentes
// (Consultas, Rehabilitación, Citas, etc. — no se tocan). Las pantallas nuevas del
// RBAC (Usuarios del Core, Inventario, Accesos Web) usan "permiso" en su lugar.
export function ProtectedRoute({
  children,
  roles,
  permiso,
}: {
  children: React.ReactNode;
  roles?: string[];
  permiso?: string | string[];
}) {
  const { sesion, tienePermiso } = useAuth();

  if (!sesion) return <Navigate to="/login" replace />;
  if (sesion.debeCambiarPassword) return <Navigate to="/cambiar-password" replace />;
  if (roles && roles.length > 0 && !roles.includes(sesion.rol)) {
    return <Navigate to="/panel" replace />;
  }
  if (permiso && !tienePermiso(permiso)) {
    return <Navigate to="/panel" replace />;
  }
  return <>{children}</>;
}
