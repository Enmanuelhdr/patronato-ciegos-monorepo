// Cliente HTTP único de toda la Web: apunta siempre al API Gateway, nunca al Core.
// El Gateway reenvía lo que no es un código transaccional directo al Core agregando él
// mismo la clave (X-Gateway-Key); la Web no la conoce ni la necesita.

const GATEWAY_URL = (import.meta.env.VITE_GATEWAY_URL ?? "http://localhost:57171").replace(/\/$/, "");

// Token JWT de la sesión activa (el Core lo valida con [Authorize] en los endpoints
// exclusivos de Recepción/Admin). AuthContext lo fija al iniciar/cerrar sesión.
let token: string | null = null;
let alExpirarSesion: (() => void) | null = null;

export function fijarTokenSesion(nuevoToken: string | null) {
  token = nuevoToken;
}

export function alVencerToken(callback: () => void) {
  alExpirarSesion = callback;
}

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

async function solicitar<T>(ruta: string, init?: RequestInit): Promise<T> {
  const respuesta = await fetch(`${GATEWAY_URL}${ruta}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      // Identifica el origen real ante el Gateway (ver ReverseProxyService/Program.cs);
      // por defecto ya asume "Web" si no se manda, pero se deja explícito.
      "X-Origen": "Web",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!respuesta.ok) {
    if (respuesta.status === 401 && token) {
      // El Core rechazó el JWT (venció o ya no es válido): cierra la sesión local.
      alExpirarSesion?.();
    }

    const texto = await respuesta.text().catch(() => "");
    let mensaje = texto;
    try {
      const json = JSON.parse(texto);
      mensaje = json.mensaje ?? json.title ?? texto;
    } catch {
      // el Core a veces responde texto plano (p.ej. NotFound("..."))
    }
    throw new ApiError(mensaje || `Error ${respuesta.status}`, respuesta.status);
  }

  if (respuesta.status === 204) return undefined as T;

  const texto = await respuesta.text();
  return (texto ? JSON.parse(texto) : undefined) as T;
}

export const api = {
  get: <T>(ruta: string) => solicitar<T>(ruta),
  post: <T>(ruta: string, body?: unknown) =>
    solicitar<T>(ruta, { method: "POST", body: body !== undefined ? JSON.stringify(body) : undefined }),
  put: <T>(ruta: string, body?: unknown) =>
    solicitar<T>(ruta, { method: "PUT", body: body !== undefined ? JSON.stringify(body) : undefined }),
  delete: <T>(ruta: string) => solicitar<T>(ruta, { method: "DELETE" }),
};
