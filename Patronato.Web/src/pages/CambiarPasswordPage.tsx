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
            : "Cambia tu contraseña de acceso al Core."}
        </p>
      </div>

      <div className="login-form-panel">
        <form className="login-form" onSubmit={enviar}>
          <h2>Cambiar contraseña</h2>
          <p>Hola, {sesion.nombre}</p>

          {error && <div className="alerta alerta-error">{error}</div>}

          <div className="campo">
            <label>Contraseña actual</label>
            <input type="password" required value={passwordActual} onChange={(e) => setPasswordActual(e.target.value)} autoFocus />
          </div>
          <div className="campo">
            <label>Contraseña nueva</label>
            <input type="password" required value={passwordNueva} onChange={(e) => setPasswordNueva(e.target.value)} />
          </div>
          <div className="campo">
            <label>Confirmar contraseña nueva</label>
            <input type="password" required value={confirmar} onChange={(e) => setConfirmar(e.target.value)} />
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
