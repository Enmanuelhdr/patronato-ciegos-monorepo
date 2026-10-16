import { useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { cambiarPasswordPropia } from "../api/endpoints";
import { ApiError } from "../api/client";

// Pantalla de cambio de contraseña: forzada tras el primer inicio de sesión con clave
// temporal (ver Usuario.DebeCambiarPassword en el Core) y también disponible para un
// cambio voluntario en cualquier momento.
export function CambiarPasswordPage() {
  const { sesion, marcarPasswordCambiada, logout } = useAuth();
  const navigate = useNavigate();

  const [passwordActual, setPasswordActual] = useState("");
  const [passwordNueva, setPasswordNueva] = useState("");
  const [confirmar, setConfirmar] = useState("");
  const [verActual, setVerActual] = useState(false);
  const [verNueva, setVerNueva] = useState(false);
  const [verConfirmar, setVerConfirmar] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (!sesion) return <Navigate to="/login" replace />;

  async function enviar(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (passwordNueva !== confirmar) {
      setError("Las contraseñas nuevas no coinciden.");
      return;
    }
    if (passwordNueva.length < 6) {
      setError("La nueva contraseña debe tener al menos 6 caracteres.");
      return;
    }

    setEnviando(true);
    try {
      await cambiarPasswordPropia(passwordActual, passwordNueva);
      marcarPasswordCambiada();
      navigate("/panel", { replace: true });
    } catch (ex) {
      setError(ex instanceof ApiError ? ex.message : "No se pudo cambiar la contraseña.");
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="login-pantalla">
      <div className="login-marca">
        <div className="sidebar-escudo" style={{ width: 54, height: 54, fontSize: 18, background: "#2563eb" }}>
          PNC
        </div>
        <h1>Patronato Nacional de Ciegos</h1>
        <p>
          {sesion.debeCambiarPassword
            ? "Por seguridad, debes establecer una contraseña propia antes de continuar."
            : "Cambia tu contraseña de acceso al portal institucional."}
        </p>
      </div>

      <div className="login-form-panel">
        <form className="login-form" onSubmit={enviar}>
          <h2>Cambiar contraseña</h2>
          <p>Hola, {sesion.nombre}</p>

          {error && <div className="alerta alerta-error">{error}</div>}

          <div className="campo">
            <label>Contraseña actual</label>
            <div style={{ position: "relative" }}>
              <input
                type={verActual ? "text" : "password"}
                required
                value={passwordActual}
                onChange={(e) => setPasswordActual(e.target.value)}
                autoFocus
                style={{ paddingRight: 40, width: "100%" }}
              />
              <button
                type="button"
                onClick={() => setVerActual(!verActual)}
                aria-label={verActual ? "Ocultar" : "Ver"}
                style={{
                  position: "absolute",
                  right: 10,
                  top: "50%",
                  transform: "translateY(-50%)",
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  fontSize: 16,
                  color: "#64748b",
                }}
              >
                {verActual ? "🙈" : "👁️"}
              </button>
            </div>
          </div>
          <div className="campo">
            <label>Contraseña nueva</label>
            <div style={{ position: "relative" }}>
              <input
                type={verNueva ? "text" : "password"}
                required
                value={passwordNueva}
                onChange={(e) => setPasswordNueva(e.target.value)}
                style={{ paddingRight: 40, width: "100%" }}
              />
              <button
                type="button"
                onClick={() => setVerNueva(!verNueva)}
                aria-label={verNueva ? "Ocultar" : "Ver"}
                style={{
                  position: "absolute",
                  right: 10,
                  top: "50%",
                  transform: "translateY(-50%)",
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  fontSize: 16,
                  color: "#64748b",
                }}
              >
                {verNueva ? "🙈" : "👁️"}
              </button>
            </div>
          </div>
          <div className="campo">
            <label>Confirmar contraseña nueva</label>
            <div style={{ position: "relative" }}>
              <input
                type={verConfirmar ? "text" : "password"}
                required
                value={confirmar}
                onChange={(e) => setConfirmar(e.target.value)}
                style={{ paddingRight: 40, width: "100%" }}
              />
              <button
                type="button"
                onClick={() => setVerConfirmar(!verConfirmar)}
                aria-label={verConfirmar ? "Ocultar" : "Ver"}
                style={{
                  position: "absolute",
                  right: 10,
                  top: "50%",
                  transform: "translateY(-50%)",
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  fontSize: 16,
                  color: "#64748b",
                }}
              >
                {verConfirmar ? "🙈" : "👁️"}
              </button>
            </div>
          </div>

          <button className="btn btn-primario" type="submit" disabled={enviando}>
            {enviando ? "Guardando..." : "Guardar contraseña"}
          </button>

          {!sesion.debeCambiarPassword && (
            <button type="button" className="btn btn-secundario" style={{ marginTop: 10 }} onClick={() => navigate("/panel")}>
              Cancelar
            </button>
          )}
          <button type="button" className="login-volver" style={{ background: "none", border: "none", cursor: "pointer" }} onClick={logout}>
            Cerrar sesión
          </button>
        </form>
      </div>
    </div>
  );
}
