import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

// roles vacío/omitido = cualquier usuario autenticado puede entrar.
export function ProtectedRoute({ children, roles }: { children: React.ReactNode; roles?: string[] }) {
  const { sesion } = useAuth();

  if (!sesion) return <Navigate to="/login" replace />;
  if (roles && roles.length > 0 && !roles.includes(sesion.rol)) {
    return <Navigate to="/" replace />;
  }
  return <>{children}</>;
}
