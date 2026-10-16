import { useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function LoginPage() {
  const { sesion, login, cargando, error } = useAuth();
  const [usuario, setUsuario] = useState("");
  const [password, setPassword] = useState("");
  const [mostrarPassword, setMostrarPassword] = useState(false);

  const [errorLocal, setErrorLocal] = useState<string | null>(null);

  if (sesion) return <Navigate to="/panel" replace />;

  async function enviar(e: React.FormEvent) {
    e.preventDefault();
    setErrorLocal(null);
    const u = usuario.trim();
    if (!u || !password) return;

    if (u.toLowerCase() === "cajero") {
      setErrorLocal(
        "Acceso Denegado: El perfil de Cajero opera exclusivamente desde la aplicación de escritorio de Caja y Facturación POS. En la web únicamente acceden Administrador, Recepción y Especialistas."
      );
      return;
    }

    await login(u, password);
  }

  const setCredencialRapida = (u: string, p: string) => {
    setErrorLocal(null);
    setUsuario(u);
    setPassword(p);
  };

  const errorAMostrar = errorLocal || error;

  return (
    <div className="login-wrapper">
      <div className="login-backdrop-decorations" aria-hidden="true">
        <div className="decor-orb decor-orb-1" />
        <div className="decor-orb decor-orb-2" />
        <div className="decor-wave" />
      </div>

      <div className="login-card-container">
        <div className="login-card">
          {/* Encabezado Institucional con Logo Oficial */}
          <div className="login-card-header">
            <div className="login-logo-container">
              <img
                src="/logo-pnc.png"
                alt="Logo Oficial Patronato Nacional de Ciegos"
                className="login-logo-img"
              />
            </div>
            <h1 className="login-card-title">Patronato Nacional de Ciegos</h1>
            <p className="login-card-subtitle">
              Portal Institucional de Servicios y Gestión Hospitalaria
            </p>
            <div className="login-divider-pill">
              <span>República Dominicana · Fundado en 1964</span>
            </div>
          </div>

          {/* Formulario */}
          <form className="login-card-form" onSubmit={enviar}>
            <div className="login-form-title-group">
              <h2>Iniciar Sesión</h2>
              <p>Acceso para personal administrativo, recepción y especialistas médicos</p>
            </div>

            {errorAMostrar && (
              <div className="alerta alerta-error" role="alert" style={{ marginBottom: 18 }}>
                <span style={{ marginRight: 6 }}>⚠️</span> {errorAMostrar}
              </div>
            )}

            <div className="campo">
              <label htmlFor="login-usuario">Nombre de usuario</label>
              <div className="input-with-icon">
                <span className="input-icon" aria-hidden="true">👤</span>
                <input
                  id="login-usuario"
                  value={usuario}
                  onChange={(e) => setUsuario(e.target.value)}
                  placeholder="Ej: admin, recepcion, medico.oftalmo"
                  autoComplete="username"
                  autoFocus
                  required
                />
              </div>
            </div>

            <div className="campo">
              <label htmlFor="login-password">Contraseña institucional</label>
              <div className="input-with-icon" style={{ position: "relative" }}>
                <span className="input-icon" aria-hidden="true">🔒</span>
                <input
                  id="login-password"
                  type={mostrarPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Ingresa tu contraseña"
                  autoComplete="current-password"
                  required
                  style={{ paddingRight: 40 }}
                />
                <button
                  type="button"
                  onClick={() => setMostrarPassword(!mostrarPassword)}
                  aria-label={mostrarPassword ? "Ocultar contraseña" : "Ver contraseña"}
                  title={mostrarPassword ? "Ocultar contraseña" : "Ver contraseña"}
                  style={{
                    position: "absolute",
                    right: 10,
                    top: "50%",
                    transform: "translateY(-50%)",
                    background: "none",
                    border: "none",
                    cursor: "pointer",
                    fontSize: 16,
                    padding: 4,
                    color: "var(--slate-500)",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                >
                  {mostrarPassword ? "🙈" : "👁️"}
                </button>
              </div>
            </div>

            <button
              className="btn btn-pnc-primary btn-block"
              type="submit"
              disabled={cargando}
              style={{ marginTop: 8 }}
            >
              {cargando ? (
                <>
                  <span className="spinner-sm" aria-hidden="true" />
                  <span>Verificando credenciales...</span>
                </>
              ) : (
                <>
                  <span>Ingresar al Sistema</span>
                  <span aria-hidden="true"> →</span>
                </>
              )}
            </button>

            {/* Accesos rápidos para evaluación/pruebas */}
            <div className="login-demo-pills">
              <span className="demo-label">Atajos de acceso para evaluación:</span>
              <div className="demo-buttons">
                <button
                  type="button"
                  className="btn-demo-pill"
                  onClick={() => setCredencialRapida("admin", "admin123")}
                  title="Cargar usuario Administrador (admin / admin123)"
                >
                  👑 Administrador
                </button>
                <button
                  type="button"
                  className="btn-demo-pill"
                  onClick={() => setCredencialRapida("recepcion", "recepcion123")}
                  title="Cargar usuario Recepción (recepcion / recepcion123)"
                >
                  📋 Recepción
                </button>
                <button
                  type="button"
                  className="btn-demo-pill"
                  onClick={() => setCredencialRapida("medico", "medico123")}
                  title="Cargar Especialista Médico Oftalmólogo (medico / medico123)"
                >
                  🩺 Médico Oftalmólogo
                </button>
              </div>
            </div>

            <div className="login-card-footer">
              <Link to="/" className="login-link-volver">
                ← Volver a la página principal pública
              </Link>
            </div>
          </form>

          {/* Pie institucional */}
          <div className="login-security-notice">
            <span aria-hidden="true">🛡️</span> Sesión protegida con auditoría de acceso institucional · RNC 4-01-00623-1
          </div>
        </div>
      </div>
    </div>
  );
}
