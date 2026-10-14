import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { loginApi } from "../api/endpoints";
import { ApiError, alVencerToken, fijarTokenSesion } from "../api/client";

export interface SesionUsuario {
  usuarioId: number;
  nombre: string;
  rol: string;
  sucursalId: number;
  token: string;
  permisos: string[];
  debeCambiarPassword: boolean;
}

interface AuthContextValue {
  sesion: SesionUsuario | null;
  cargando: boolean;
  error: string | null;
  login: (usuario: string, password: string) => Promise<boolean>;
  logout: () => void;
  tienePermiso: (codigo: string | string[]) => boolean;
  marcarPasswordCambiada: () => void;
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

function sesionInicial(): SesionUsuario | null {
  const guardada = leerSesionGuardada();
  fijarTokenSesion(guardada?.token ?? null); // restaura el JWT en el cliente HTTP tras recargar la página
  return guardada;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<SesionUsuario | null>(sesionInicial);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function logout() {
    setSesion(null);
    fijarTokenSesion(null);
    sessionStorage.removeItem(CLAVE_SESION);
  }

  // Si el Core rechaza el JWT (venció o ya no es válido) en cualquier request, cierra la sesión local.
  useEffect(() => {
    alVencerToken(logout);
  }, []);

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
        token: data.token,
        permisos: data.permisos,
        debeCambiarPassword: data.debeCambiarPassword,
      };
      setSesion(nuevaSesion);
      fijarTokenSesion(data.token);
      sessionStorage.setItem(CLAVE_SESION, JSON.stringify(nuevaSesion));
      return true;
    } catch (ex) {
      setError(ex instanceof ApiError ? ex.message : "No se pudo conectar con el Gateway.");
      return false;
    } finally {
      setCargando(false);
    }
  }

  function tienePermiso(codigo: string | string[]) {
    if (!sesion) return false;
    const codigos = Array.isArray(codigo) ? codigo : [codigo];
    return codigos.some((c) => sesion.permisos.includes(c));
  }

  // Tras un cambio de contraseña exitoso (forzado o voluntario): ya no debe redirigir
  // a la pantalla de cambio obligatorio en esta sesión.
  function marcarPasswordCambiada() {
    setSesion((prev) => {
      if (!prev) return prev;
      const actualizada = { ...prev, debeCambiarPassword: false };
      sessionStorage.setItem(CLAVE_SESION, JSON.stringify(actualizada));
      return actualizada;
    });
  }

  return (
    <AuthContext.Provider value={{ sesion, cargando, error, login, logout, tienePermiso, marcarPasswordCambiada }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth debe usarse dentro de <AuthProvider>");
  return ctx;
}
