import { useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function LoginPage() {
  const { sesion, login, cargando, error } = useAuth();
  const [usuario, setUsuario] = useState("recepcion");
  const [password, setPassword] = useState("recepcion123");

  if (sesion) return <Navigate to="/panel" replace />;

  async function enviar(e: React.FormEvent) {
    e.preventDefault();
    await login(usuario, password);
  }

  return (
    <div className="login-pantalla">
      <div className="login-marca">
        <div className="sidebar-escudo" style={{ width: 54, height: 54, fontSize: 18, background: "#2563eb" }}>
          PNC
        </div>
        <h1>Patronato Nacional de Ciegos</h1>
        <p>
          Portal de Recepción y Administración. Gestiona beneficiarios, consultas, rehabilitación, préstamos,
          masajes TACTO, donaciones, voluntariado y reportes de caja — todo a través del API Gateway.
        </p>
      </div>

      <div className="login-form-panel">
        <form className="login-form" onSubmit={enviar}>
          <h2>Iniciar sesión</h2>
          <p>Acceso para personal del Patronato</p>

          {error && <div className="alerta alerta-error">{error}</div>}

          <div className="campo">
            <label>Usuario</label>
            <input value={usuario} onChange={(e) => setUsuario(e.target.value)} autoFocus />
          </div>
          <div className="campo">
            <label>Contraseña</label>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>

          <button className="btn btn-primario" type="submit" disabled={cargando}>
            {cargando ? "Conectando..." : "Entrar"}
          </button>

          <Link to="/" className="login-volver">← Volver al sitio público</Link>
        </form>
      </div>
    </div>
  );
}
