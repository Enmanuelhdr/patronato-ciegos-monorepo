import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { useAuth } from "../context/AuthContext";
import { ApiError } from "../api/client";
import {
  deshabilitarWebUser,
  forzarCierreSesionWebUser,
  getHistorialWebUser,
  getWebUsers,
  habilitarWebUser,
  resetearPasswordWebUser,
  type FiltrosWebUsers,
} from "../api/endpoints";
import type { WebUser } from "../api/types";

// Auditoría/gestión, desde el Core, de las cuentas de la plataforma Web pública
// (clientes/beneficiarios externos). El registro/login público real todavía no existe
// (ver WebUser.cs en el Core) — esta pantalla administra esa tabla y queda lista para
// cuando ese portal de autoservicio se construya.
export function AccesosWebPage() {
  const qc = useQueryClient();
  const { tienePermiso } = useAuth();
  const puedeGestionar = tienePermiso("web-usuarios.gestionar");

  const [busqueda, setBusqueda] = useState("");
  const [estadoFiltro, setEstadoFiltro] = useState<"" | "Activo" | "Suspendido">("");
  const filtros: FiltrosWebUsers = { busqueda: busqueda || undefined, estado: estadoFiltro || undefined };

  const webUsers = useQuery({ queryKey: ["web-users", filtros], queryFn: () => getWebUsers(filtros) });

  const [error, setError] = useState<string | null>(null);
  const [passwordGenerada, setPasswordGenerada] = useState<{ nombre: string; password: string } | null>(null);
  const [historialDe, setHistorialDe] = useState<WebUser | null>(null);
  const historial = useQuery({
    queryKey: ["web-user-historial", historialDe?.id],
    queryFn: () => getHistorialWebUser(historialDe!.id),
    enabled: !!historialDe,
  });

  const invalidar = () => qc.invalidateQueries({ queryKey: ["web-users"] });

  const habilitar = useMutation({ mutationFn: habilitarWebUser, onSuccess: invalidar, onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo habilitar la cuenta.") });
  const deshabilitar = useMutation({ mutationFn: deshabilitarWebUser, onSuccess: invalidar, onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo deshabilitar la cuenta.") });
  const forzarLogout = useMutation({
    mutationFn: forzarCierreSesionWebUser,
    onSuccess: (r) => { setError(null); alert(r.mensaje); },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo forzar el cierre de sesión."),
  });
  const resetear = useMutation({
    mutationFn: async (w: WebUser) => ({ nombre: w.nombreCompleto, ...(await resetearPasswordWebUser(w.id)) }),
    onSuccess: (r) => { invalidar(); setPasswordGenerada({ nombre: r.nombre, password: r.passwordTemporal }); },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo resetear la contraseña."),
  });

  const columnas: Columna<WebUser>[] = [
    { encabezado: "Nombre", render: (w) => w.nombreCompleto },
    { encabezado: "Correo", render: (w) => w.correo },
    { encabezado: "Teléfono", render: (w) => w.telefono ?? "—" },
    { encabezado: "Estado", render: (w) => <span className={`tag ${w.estado === "Activo" ? "tag-verde" : "tag-gris"}`}>{w.estado}</span> },
    { encabezado: "Registrado", render: (w) => new Date(w.fechaRegistro).toLocaleDateString() },
    { encabezado: "Último acceso", render: (w) => (w.ultimoLogin ? new Date(w.ultimoLogin).toLocaleString() : "Nunca") },
    {
      encabezado: "Acciones",
      render: (w) => (
        <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
          <button className="btn btn-secundario" onClick={() => setHistorialDe(w)}>Historial</button>
          {puedeGestionar && (
            <>
              {w.estado === "Activo" ? (
                <button className="btn btn-peligro" disabled={deshabilitar.isPending} onClick={() => deshabilitar.mutate(w.id)}>Deshabilitar</button>
              ) : (
                <button className="btn btn-exito" disabled={habilitar.isPending} onClick={() => habilitar.mutate(w.id)}>Habilitar</button>
              )}
              <button className="btn btn-secundario" disabled={resetear.isPending} onClick={() => resetear.mutate(w)}>Resetear clave</button>
              <button
                className="btn btn-secundario"
                disabled={forzarLogout.isPending}
                onClick={() => { if (confirm(`¿Forzar el cierre de todas las sesiones activas de "${w.nombreCompleto}"?`)) forzarLogout.mutate(w.id); }}
              >
                Forzar cierre de sesión
              </button>
            </>
          )}
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Accesos a la Plataforma Web"
        subtitulo="Audita y gestiona las cuentas de clientes/beneficiarios de la plataforma web pública."
      />

      {error && <div className="alerta alerta-error">{error}</div>}
      {passwordGenerada && (
        <div className="alerta alerta-exito" style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <span>Clave temporal para <strong>{passwordGenerada.nombre}</strong>: <code>{passwordGenerada.password}</code> — comunícasela al cliente.</span>
          <button className="btn btn-secundario" onClick={() => setPasswordGenerada(null)}>Cerrar</button>
        </div>
      )}

      <div className="panel" style={{ marginBottom: 16 }}>
        <div className="form-grid">
          <div className="campo">
            <label>Buscar (nombre o correo)</label>
            <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} />
          </div>
          <div className="campo">
            <label>Estado</label>
            <select value={estadoFiltro} onChange={(e) => setEstadoFiltro(e.target.value as typeof estadoFiltro)}>
              <option value="">Todos</option>
              <option value="Activo">Activo</option>
              <option value="Suspendido">Suspendido</option>
            </select>
          </div>
        </div>
      </div>

      {historialDe && (
        <div className="panel">
          <h2>Historial de accesos — {historialDe.nombreCompleto}</h2>
          <DataTable
            columnas={[
              { encabezado: "Fecha", render: (h) => new Date(h.fechaHora).toLocaleString() },
              { encabezado: "Resultado", render: (h) => <span className={`tag ${h.exitoso ? "tag-verde" : "tag-rojo"}`}>{h.exitoso ? "Exitoso" : "Fallido"}</span> },
              { encabezado: "IP", render: (h) => h.ip ?? "—" },
            ]}
            filas={historial.data ?? []}
            claveFila={(h) => h.id}
            cargando={historial.isLoading}
            vacio="Sin accesos registrados todavía."
          />
          <button className="btn btn-secundario" style={{ marginTop: 10 }} onClick={() => setHistorialDe(null)}>Cerrar historial</button>
        </div>
      )}

      <DataTable columnas={columnas} filas={webUsers.data ?? []} claveFila={(w) => w.id} cargando={webUsers.isLoading} />
    </>
  );
}
