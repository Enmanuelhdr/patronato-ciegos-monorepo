import { createContext, useContext, useState, type ReactNode } from "react";
import { loginApi } from "../api/endpoints";
import { ApiError } from "../api/client";

export interface SesionUsuario {
  usuarioId: number;
  nombre: string;
  rol: string;
  sucursalId: number;
}

interface AuthContextValue {
  sesion: SesionUsuario | null;
  cargando: boolean;
  error: string | null;
  login: (usuario: string, password: string) => Promise<boolean>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const CLAVE_SESION = "patronato-web-sesion";

function leerSesionGuardada(): SesionUsuario | null {
  try {
    const raw = sessionStorage.getItem(CLAVE_SESION);
    return raw ? (JSON.parse(raw) as SesionUsuario) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<SesionUsuario | null>(leerSesionGuardada());
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function login(usuario: string, password: string) {
    setCargando(true);
    setError(null);
    try {
      const data = await loginApi(usuario, password);
      const nuevaSesion: SesionUsuario = {
        usuarioId: data.usuarioId,
        nombre: data.nombre,
        rol: data.rol,
        sucursalId: data.sucursalId,
      };
      setSesion(nuevaSesion);
      sessionStorage.setItem(CLAVE_SESION, JSON.stringify(nuevaSesion));
      return true;
    } catch (ex) {
      setError(ex instanceof ApiError ? ex.message : "No se pudo conectar con el Gateway.");
      return false;
    } finally {
      setCargando(false);
    }
  }

  function logout() {
    setSesion(null);
    sessionStorage.removeItem(CLAVE_SESION);
  }

  return (
    <AuthContext.Provider value={{ sesion, cargando, error, login, logout }}>{children}</AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth debe usarse dentro de <AuthProvider>");
  return ctx;
}
