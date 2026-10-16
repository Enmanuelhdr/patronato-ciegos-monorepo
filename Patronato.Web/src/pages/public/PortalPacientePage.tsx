import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { PublicNavbar } from "../../components/PublicNavbar";
import { PublicFooter } from "../../components/PublicFooter";
import {
  loginPaciente,
  registrarPaciente,
  verificarCorreoPaciente,
  reenviarCodigoPaciente,
  getMisCitasPaciente,
  solicitarCitaPaciente,
  type SesionPaciente,
} from "../../api/endpoints";
import { ApiError } from "../../api/client";
import { EstadoCitaTexto, SedeTexto, TipoServicioCitaTexto, type SolicitudCita } from "../../api/types";

const CLAVE_STORAGE_PACIENTE = "pnc-paciente-sesion";

export function PortalPacientePage() {
  const qc = useQueryClient();

  // Estados de vista: "login" | "registro" | "verificar" | "panel"
  const [vista, setVista] = useState<"login" | "registro" | "verificar" | "panel">("login");
  const [sesion, setSesion] = useState<SesionPaciente | null>(() => {
    try {
      const guardada = localStorage.getItem(CLAVE_STORAGE_PACIENTE);
      return guardada ? JSON.parse(guardada) : null;
    } catch {
      return null;
    }
  });

  // Visibilidad de contraseñas (Toggle 👁️)
  const [verPasswordLogin, setVerPasswordLogin] = useState(false);
  const [verPasswordRegistro, setVerPasswordRegistro] = useState(false);

  // Formulario Login
  const [loginCorreo, setLoginCorreo] = useState("");
  const [loginPassword, setLoginPassword] = useState("");
  const [errorLogin, setErrorLogin] = useState<string | null>(null);

  // Formulario Registro
  const [regNombre, setRegNombre] = useState("");
  const [regCedula, setRegCedula] = useState("");
  const [regTelefono, setRegTelefono] = useState("");
  const [regCorreo, setRegCorreo] = useState("");
  const [regPassword, setRegPassword] = useState("");
  const [errorRegistro, setErrorRegistro] = useState<string | null>(null);

  // Verificación OTP
  const [correoAVerificar, setCorreoAVerificar] = useState("");
  const [codigoOtp, setCodigoOtp] = useState("");
  const [errorOtp, setErrorOtp] = useState<string | null>(null);
  const [mensajeExito, setMensajeExito] = useState<string | null>(null);

  // Modal / Formulario Nueva Cita Rápida
  const [mostrarModalCita, setMostrarModalCita] = useState(false);
  const [nuevaCitaServicio, setNuevaCitaServicio] = useState(3);
  const [nuevaCitaSede, setNuevaCitaSede] = useState(1);
  const [nuevaCitaFecha, setNuevaCitaFecha] = useState("");
  const [nuevaCitaComentario, setNuevaCitaComentario] = useState("");
  const [mensajeCita, setMensajeCita] = useState<string | null>(null);

  useEffect(() => {
    if (sesion) {
      setVista("panel");
    }
  }, [sesion]);

  // Cargar solicitudes del paciente logueado
  const citasQuery = useQuery({
    queryKey: ["paciente-citas", sesion?.correo],
    queryFn: () => getMisCitasPaciente(sesion!.correo),
    enabled: !!sesion?.correo && vista === "panel",
  });

  const citas: SolicitudCita[] = citasQuery.data ?? [];

  // Mutación Login
  const loginMutation = useMutation({
    mutationFn: () => loginPaciente(loginCorreo.trim(), loginPassword),
    onSuccess: (data) => {
      setErrorLogin(null);
      if (data.requiereVerificacion) {
        setCorreoAVerificar(data.correo || loginCorreo.trim());
        setCodigoOtp("");
        setVista("verificar");
        return;
      }
      if (data.token && data.correo && data.nombre) {
        const sesionData: SesionPaciente = {
          id: data.id || 0,
          nombre: data.nombre,
          correo: data.correo,
          telefono: data.telefono || "",
          cedula: data.cedula,
          token: data.token,
        };
        setSesion(sesionData);
        localStorage.setItem(CLAVE_STORAGE_PACIENTE, JSON.stringify(sesionData));
        setVista("panel");
      }
    },
    onError: (err) => {
      setErrorLogin(err instanceof ApiError ? err.message : "Error al iniciar sesión.");
    },
  });

  // Mutación Registro
  const registroMutation = useMutation({
    mutationFn: () =>
      registrarPaciente({
        nombreCompleto: regNombre.trim(),
        correo: regCorreo.trim(),
        telefono: regTelefono.trim(),
        password: regPassword,
        cedula: regCedula.trim() || undefined,
      }),
    onSuccess: (data) => {
      setErrorRegistro(null);
      setCorreoAVerificar(data.correo);
      setCodigoOtp("");
      setMensajeExito(data.mensaje);
      setVista("verificar");
    },
    onError: (err) => {
      setErrorRegistro(err instanceof ApiError ? err.message : "Error al registrar la cuenta.");
    },
  });

  // Mutación Reenvío de Código OTP
  const reenviarMutation = useMutation({
    mutationFn: () => reenviarCodigoPaciente(correoAVerificar),
    onSuccess: (data) => {
      setErrorOtp(null);
      setMensajeExito(data.mensaje);
    },
    onError: (err) => {
      setErrorOtp(err instanceof ApiError ? err.message : "Error al reenviar el código.");
    },
  });

  // Mutación Verificación OTP
  const verificarMutation = useMutation({
    mutationFn: () => verificarCorreoPaciente(correoAVerificar, codigoOtp.trim()),
    onSuccess: (data) => {
      setErrorOtp(null);
      setMensajeExito(data.mensaje);
      setTimeout(() => {
        setVista("login");
        setLoginCorreo(correoAVerificar);
      }, 1500);
    },
    onError: (err) => {
      setErrorOtp(err instanceof ApiError ? err.message : "Código de verificación inválido.");
    },
  });

  // Mutación Solicitar Cita
  const solicitarCitaMutation = useMutation({
    mutationFn: () =>
      solicitarCitaPaciente({
        correo: sesion!.correo,
        tipoServicio: nuevaCitaServicio,
        sedePreferida: nuevaCitaSede,
        fechaPreferida: nuevaCitaFecha ? new Date(nuevaCitaFecha).toISOString() : new Date().toISOString(),
        comentario: nuevaCitaComentario,
      }),
    onSuccess: () => {
      setMensajeCita("¡Solicitud enviada exitosamente! Ya aparece en tu lista.");
      qc.invalidateQueries({ queryKey: ["paciente-citas"] });
      setTimeout(() => {
        setMostrarModalCita(false);
        setMensajeCita(null);
        setNuevaCitaComentario("");
        setNuevaCitaFecha("");
      }, 1800);
    },
  });

  const cerrarSesionPaciente = () => {
    localStorage.removeItem(CLAVE_STORAGE_PACIENTE);
    setSesion(null);
    setVista("login");
  };

  const badgeEstado = (estado: number) => {
    switch (estado) {
      case 1:
        return (
          <span style={{ background: "#fef3c7", color: "#92400e", padding: "4px 10px", borderRadius: 999, fontWeight: 700, fontSize: 12 }}>
            🟡 Pendiente de Confirmación
          </span>
        );
      case 2:
        return (
          <span style={{ background: "#dcfce7", color: "#166534", padding: "4px 10px", borderRadius: 999, fontWeight: 700, fontSize: 12 }}>
            🟢 Confirmada por Recepción
          </span>
        );
      case 3:
        return (
          <span style={{ background: "#fee2e2", color: "#991b1b", padding: "4px 10px", borderRadius: 999, fontWeight: 700, fontSize: 12 }}>
            🔴 Cancelada
          </span>
        );
      default:
        return (
          <span style={{ background: "#f1f5f9", color: "#475569", padding: "4px 10px", borderRadius: 999, fontWeight: 700, fontSize: 12 }}>
            {EstadoCitaTexto[estado as 1 | 2 | 3] ?? "En Proceso"}
          </span>
        );
    }
  };

  return (
    <div className="landing-moderna">
      <PublicNavbar paginaActiva="citas" />

      {/* Hero del Portal */}
      <section className="pagina-publica-hero">
        <div className="pagina-publica-hero-inner">
          <div className="breadcrumb-nav">
            <Link to="/">Inicio</Link>
            <span>/</span>
            <Link to="/agendar-cita">Citas</Link>
            <span>/</span>
            <span className="actual">Portal del Paciente</span>
          </div>

          <div style={{ display: "inline-flex", alignItems: "center", gap: 8, background: "rgba(255,255,255,0.15)", padding: "6px 14px", borderRadius: 20, marginBottom: 12 }}>
            <span style={{ fontSize: 14 }}>🛡️ Portal Seguro del Beneficiario y Paciente</span>
          </div>

          <h1 style={{ fontSize: "2.5rem", fontWeight: 900, marginBottom: 12 }}>
            Portal del Paciente PNC
          </h1>
          <p style={{ maxWidth: 720, fontSize: "1.1rem", opacity: 0.95, lineHeight: 1.6 }}>
            Accede con tu cuenta verificada para consultar en tiempo real todas tus citas médicas, terapias de rehabilitación y solicitudes, sin tener que memorizar ni digitar códigos.
          </p>
        </div>
      </section>

      {/* Contenido Principal */}
      <main className="pagina-publica-main" style={{ maxWidth: 1100, margin: "0 auto", padding: "40px 20px" }}>
        {/* VISTA 1: INICIAR SESIÓN */}
        {vista === "login" && (
          <div style={{ maxWidth: 480, margin: "0 auto", background: "#ffffff", borderRadius: 16, border: "1px solid #e2e8f0", boxShadow: "0 10px 25px -5px rgba(0,0,0,0.05)", padding: 32 }}>
            <div style={{ textAlign: "center", marginBottom: 24 }}>
              <div style={{ width: 56, height: 56, borderRadius: "50%", background: "#eff6ff", color: "#1e40af", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 26, margin: "0 auto 12px" }}>
                👤
              </div>
              <h2 style={{ fontSize: 22, fontWeight: 800, color: "#1e293b", margin: 0 }}>
                Iniciar Sesión como Paciente
              </h2>
              <p style={{ fontSize: 14, color: "#64748b", marginTop: 6 }}>
                Ingresa con tu correo verificado para ver todas tus citas
              </p>
            </div>

            {errorLogin && (
              <div style={{ background: "#fee2e2", border: "1px solid #fca5a5", color: "#991b1b", padding: "10px 14px", borderRadius: 8, marginBottom: 16, fontSize: 14 }}>
                {errorLogin}
              </div>
            )}

            <form
              onSubmit={(e) => {
                e.preventDefault();
                loginMutation.mutate();
              }}
            >
              <div style={{ marginBottom: 16 }}>
                <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                  Correo Electrónico
                </label>
                <input
                  type="email"
                  required
                  placeholder="ejemplo@correo.com"
                  value={loginCorreo}
                  onChange={(e) => setLoginCorreo(e.target.value)}
                  style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                />
              </div>

              <div style={{ marginBottom: 20 }}>
                <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                  Contraseña
                </label>
                <div style={{ position: "relative" }}>
                  <input
                    type={verPasswordLogin ? "text" : "password"}
                    required
                    placeholder="Tu contraseña personal"
                    value={loginPassword}
                    onChange={(e) => setLoginPassword(e.target.value)}
                    style={{ width: "100%", padding: "10px 42px 10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                  />
                  <button
                    type="button"
                    onClick={() => setVerPasswordLogin(!verPasswordLogin)}
                    aria-label={verPasswordLogin ? "Ocultar contraseña" : "Ver contraseña"}
                    title={verPasswordLogin ? "Ocultar contraseña" : "Ver contraseña"}
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
                    {verPasswordLogin ? "🙈" : "👁️"}
                  </button>
                </div>
              </div>

              <button
                type="submit"
                className="btn btn-pnc-primary btn-block"
                disabled={loginMutation.isPending}
                style={{ width: "100%", padding: "12px", fontSize: 15, fontWeight: 700 }}
              >
                {loginMutation.isPending ? "Validando credenciales..." : "Entrar a Mis Citas →"}
              </button>
            </form>

            <div style={{ marginTop: 24, textAlign: "center", borderTop: "1px solid #f1f5f9", paddingTop: 16 }}>
              <span style={{ fontSize: 13, color: "#64748b" }}>¿Aún no tienes cuenta de paciente? </span>
              <button
                type="button"
                onClick={() => setVista("registro")}
                style={{ background: "none", border: "none", color: "#2563eb", fontWeight: 700, cursor: "pointer", fontSize: 13 }}
              >
                Regístrate aquí
              </button>
            </div>
          </div>
        )}

        {/* VISTA 2: REGISTRO DE PACIENTE */}
        {vista === "registro" && (
          <div style={{ maxWidth: 520, margin: "0 auto", background: "#ffffff", borderRadius: 16, border: "1px solid #e2e8f0", boxShadow: "0 10px 25px -5px rgba(0,0,0,0.05)", padding: 32 }}>
            <div style={{ textAlign: "center", marginBottom: 24 }}>
              <div style={{ width: 56, height: 56, borderRadius: "50%", background: "#ecfdf5", color: "#059669", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 26, margin: "0 auto 12px" }}>
                ✨
              </div>
              <h2 style={{ fontSize: 22, fontWeight: 800, color: "#1e293b", margin: 0 }}>
                Crear Cuenta de Paciente
              </h2>
              <p style={{ fontSize: 14, color: "#64748b", marginTop: 6 }}>
                Regístrate con tu correo para vincular y consultar todas tus citas
              </p>
            </div>

            {errorRegistro && (
              <div style={{ background: "#fee2e2", border: "1px solid #fca5a5", color: "#991b1b", padding: "10px 14px", borderRadius: 8, marginBottom: 16, fontSize: 14 }}>
                {errorRegistro}
              </div>
            )}

            <form
              onSubmit={(e) => {
                e.preventDefault();
                if (regPassword.length < 6) {
                  setErrorRegistro("La contraseña debe tener mínimo 6 caracteres.");
                  return;
                }
                registroMutation.mutate();
              }}
            >
              <div style={{ marginBottom: 14 }}>
                <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                  Nombre y Apellidos completos *
                </label>
                <input
                  required
                  placeholder="Ej. Juan Bautista Rosario"
                  value={regNombre}
                  onChange={(e) => setRegNombre(e.target.value)}
                  style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                />
              </div>

              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginBottom: 14 }}>
                <div>
                  <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                    Teléfono Móvil *
                  </label>
                  <input
                    required
                    placeholder="809-555-0101"
                    value={regTelefono}
                    onChange={(e) => setRegTelefono(e.target.value)}
                    style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                  />
                </div>
                <div>
                  <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                    Cédula (Opcional)
                  </label>
                  <input
                    placeholder="001-1234567-8"
                    value={regCedula}
                    onChange={(e) => setRegCedula(e.target.value)}
                    style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                  />
                </div>
              </div>

              <div style={{ marginBottom: 14 }}>
                <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                  Correo Electrónico <span style={{ color: "#dc2626" }}>* (Obligatorio para verificación)</span>
                </label>
                <input
                  type="email"
                  required
                  placeholder="tu.correo@ejemplo.com"
                  value={regCorreo}
                  onChange={(e) => setRegCorreo(e.target.value)}
                  style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                />
              </div>

              <div style={{ marginBottom: 20 }}>
                <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                  Contraseña (mínimo 6 caracteres) *
                </label>
                <div style={{ position: "relative" }}>
                  <input
                    type={verPasswordRegistro ? "text" : "password"}
                    required
                    placeholder="Crea una contraseña segura"
                    value={regPassword}
                    onChange={(e) => setRegPassword(e.target.value)}
                    style={{ width: "100%", padding: "10px 42px 10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                  />
                  <button
                    type="button"
                    onClick={() => setVerPasswordRegistro(!verPasswordRegistro)}
                    aria-label={verPasswordRegistro ? "Ocultar contraseña" : "Ver contraseña"}
                    title={verPasswordRegistro ? "Ocultar contraseña" : "Ver contraseña"}
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
                    {verPasswordRegistro ? "🙈" : "👁️"}
                  </button>
                </div>
              </div>

              <button
                type="submit"
                className="btn btn-pnc-primary btn-block"
                disabled={registroMutation.isPending}
                style={{ width: "100%", padding: "12px", fontSize: 15, fontWeight: 700 }}
              >
                {registroMutation.isPending ? "Registrando cuenta..." : "Crear mi Cuenta y Validar Correo →"}
              </button>
            </form>

            <div style={{ marginTop: 24, textAlign: "center", borderTop: "1px solid #f1f5f9", paddingTop: 16 }}>
              <span style={{ fontSize: 13, color: "#64748b" }}>¿Ya estás registrado? </span>
              <button
                type="button"
                onClick={() => setVista("login")}
                style={{ background: "none", border: "none", color: "#2563eb", fontWeight: 700, cursor: "pointer", fontSize: 13 }}
              >
                Inicia sesión aquí
              </button>
            </div>
          </div>
        )}

        {/* VISTA 3: VALIDACIÓN DE CORREO CON OTP */}
        {vista === "verificar" && (
          <div style={{ maxWidth: 480, margin: "0 auto", background: "#ffffff", borderRadius: 16, border: "1px solid #e2e8f0", boxShadow: "0 10px 25px -5px rgba(0,0,0,0.05)", padding: 32 }}>
            <div style={{ textAlign: "center", marginBottom: 24 }}>
              <div style={{ width: 56, height: 56, borderRadius: "50%", background: "#fef3c7", color: "#b45309", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 26, margin: "0 auto 12px" }}>
                ✉️
              </div>
              <h2 style={{ fontSize: 22, fontWeight: 800, color: "#1e293b", margin: 0 }}>
                Valida tu Correo Electrónico
              </h2>
              <p style={{ fontSize: 14, color: "#64748b", marginTop: 8, lineHeight: 1.5 }}>
                Hemos generado un código de verificación para <strong>{correoAVerificar}</strong>.
              </p>
            </div>

            {mensajeExito && (
              <div style={{ background: "#dcfce7", border: "1px solid #86efac", color: "#166534", padding: "12px", borderRadius: 8, marginBottom: 16, fontSize: 14, textAlign: "center", fontWeight: 700 }}>
                {mensajeExito}
              </div>
            )}

            {errorOtp && (
              <div style={{ background: "#fee2e2", border: "1px solid #fca5a5", color: "#991b1b", padding: "10px 14px", borderRadius: 8, marginBottom: 16, fontSize: 14 }}>
                {errorOtp}
              </div>
            )}

            <div style={{ background: "#f0fdf4", border: "1px solid #bbf7d0", padding: "16px", borderRadius: 10, marginBottom: 20, textAlign: "center" }}>
              <div style={{ fontSize: 24, marginBottom: 6 }}>📬</div>
              <p style={{ margin: 0, fontSize: 13, color: "#166534", fontWeight: 700 }}>
                Hemos enviado un código de seguridad de 6 dígitos a su correo electrónico.
              </p>
              <p style={{ margin: "4px 0 0", fontSize: 12, color: "#15803d" }}>
                Revise su bandeja de entrada (y la carpeta de spam o correo no deseado).
              </p>
            </div>

            <form
              onSubmit={(e) => {
                e.preventDefault();
                verificarMutation.mutate();
              }}
            >
              <div style={{ marginBottom: 20 }}>
                <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6, textAlign: "center" }}>
                  Ingresa el código PIN recibido (6 dígitos)
                </label>
                <input
                  type="text"
                  required
                  maxLength={6}
                  placeholder="------"
                  value={codigoOtp}
                  onChange={(e) => setCodigoOtp(e.target.value.replace(/\D/g, ""))}
                  style={{
                    width: "100%",
                    padding: "12px",
                    borderRadius: 8,
                    border: "2px solid #2563eb",
                    fontSize: 26,
                    fontWeight: 800,
                    textAlign: "center",
                    letterSpacing: 10,
                  }}
                />
              </div>

              <button
                type="submit"
                className="btn btn-pnc-primary btn-block"
                disabled={verificarMutation.isPending || codigoOtp.length < 6}
                style={{ width: "100%", padding: "12px", fontSize: 15, fontWeight: 700 }}
              >
                {verificarMutation.isPending ? "Validando código..." : "Validar Código y Continuar →"}
              </button>
            </form>

            <div style={{ marginTop: 24, textAlign: "center", display: "flex", flexDirection: "column", gap: 12 }}>
              <button
                type="button"
                onClick={() => reenviarMutation.mutate()}
                disabled={reenviarMutation.isPending}
                style={{ background: "none", border: "none", color: "#2563eb", fontSize: 13, fontWeight: 700, cursor: "pointer", textDecoration: "underline" }}
              >
                {reenviarMutation.isPending ? "Reenviando correo..." : "✉️ ¿No recibiste el correo? Reenviar código"}
              </button>

              <button
                type="button"
                onClick={() => setVista("login")}
                style={{ background: "none", border: "none", color: "#64748b", fontSize: 13, cursor: "pointer" }}
              >
                ← Volver al inicio de sesión
              </button>
            </div>
          </div>
        )}

        {/* VISTA 4: PANEL DEL PACIENTE ("MIS CITAS Y SOLICITUDES") */}
        {vista === "panel" && sesion && (
          <div>
            {/* Header del Paciente */}
            <div
              style={{
                background: "linear-gradient(135deg, #1e3a8a 0%, #1e40af 100%)",
                borderRadius: 16,
                padding: "24px 30px",
                color: "#ffffff",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                flexWrap: "wrap",
                gap: 20,
                marginBottom: 30,
                boxShadow: "0 10px 25px -5px rgba(30,58,138,0.3)",
              }}
            >
              <div>
                <div style={{ fontSize: 13, opacity: 0.85, fontWeight: 600 }}>CUENTA DE PACIENTE VERIFICADA</div>
                <h2 style={{ fontSize: 26, fontWeight: 800, margin: "4px 0 6px" }}>
                  Hola, {sesion.nombre}
                </h2>
                <div style={{ display: "flex", gap: 16, fontSize: 13, opacity: 0.9, flexWrap: "wrap" }}>
                  <span>✉️ {sesion.correo}</span>
                  {sesion.telefono && <span>📞 {sesion.telefono}</span>}
                  {sesion.cedula && <span>🪪 Cédula: {sesion.cedula}</span>}
                </div>
              </div>

              <div style={{ display: "flex", gap: 12 }}>
                <button
                  type="button"
                  className="btn"
                  onClick={() => setMostrarModalCita(true)}
                  style={{ background: "#10b981", color: "#ffffff", fontWeight: 700, padding: "10px 18px", border: "none" }}
                >
                  ➕ Solicitar Nueva Cita
                </button>
                <button
                  type="button"
                  className="btn"
                  onClick={cerrarSesionPaciente}
                  style={{ background: "rgba(255,255,255,0.2)", color: "#ffffff", fontWeight: 700, padding: "10px 18px", border: "1px solid rgba(255,255,255,0.3)" }}
                >
                  Cerrar Sesión
                </button>
              </div>
            </div>

            {/* Modal de Solicitud de Cita Rápida */}
            {mostrarModalCita && (
              <div
                style={{
                  position: "fixed",
                  top: 0,
                  left: 0,
                  right: 0,
                  bottom: 0,
                  background: "rgba(15,23,42,0.6)",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  zIndex: 1000,
                  padding: 20,
                }}
              >
                <div style={{ background: "#ffffff", borderRadius: 16, width: "100%", maxWidth: 540, padding: 30, boxShadow: "0 20px 25px -5px rgba(0,0,0,0.2)" }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
                    <h3 style={{ fontSize: 20, fontWeight: 800, color: "#1e293b", margin: 0 }}>
                      Nueva Solicitud de Cita
                    </h3>
                    <button
                      type="button"
                      onClick={() => setMostrarModalCita(false)}
                      style={{ background: "none", border: "none", fontSize: 20, cursor: "pointer", color: "#64748b" }}
                    >
                      ✕
                    </button>
                  </div>

                  {mensajeCita && (
                    <div style={{ background: "#dcfce7", color: "#166534", padding: "10px 14px", borderRadius: 8, marginBottom: 16, fontWeight: 700, fontSize: 14 }}>
                      {mensajeCita}
                    </div>
                  )}

                  <form
                    onSubmit={(e) => {
                      e.preventDefault();
                      solicitarCitaMutation.mutate();
                    }}
                  >
                    <div style={{ marginBottom: 14 }}>
                      <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                        Servicio Requerido
                      </label>
                      <select
                        value={nuevaCitaServicio}
                        onChange={(e) => setNuevaCitaServicio(Number(e.target.value))}
                        style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                      >
                        <option value={3}>Masajes TACTO (Terapeutas Invidentes)</option>
                        <option value={4}>Tiflotecnología e Insumos Visuales</option>
                        <option value={1}>Evaluación Oftalmológica de Prevención</option>
                        <option value={2}>Orientación y Movilidad (Bastón / Braille)</option>
                      </select>
                    </div>

                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginBottom: 14 }}>
                      <div>
                        <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                          Sede Preferida
                        </label>
                        <select
                          value={nuevaCitaSede}
                          onChange={(e) => setNuevaCitaSede(Number(e.target.value))}
                          style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                        >
                          <option value={1}>Santo Domingo (Sede Central)</option>
                          <option value={2}>Santiago (Regional Norte)</option>
                          <option value={3}>Barahona (Regional Sur)</option>
                        </select>
                      </div>

                      <div>
                        <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                          Fecha Tentativa
                        </label>
                        <input
                          type="date"
                          required
                          value={nuevaCitaFecha}
                          min={new Date().toISOString().slice(0, 10)}
                          onChange={(e) => setNuevaCitaFecha(e.target.value)}
                          style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                        />
                      </div>
                    </div>

                    <div style={{ marginBottom: 20 }}>
                      <label style={{ display: "block", fontSize: 13, fontWeight: 700, color: "#334155", marginBottom: 6 }}>
                        Notas o motivo de la consulta
                      </label>
                      <textarea
                        rows={3}
                        placeholder="Ej. Chequeo preventivo de presión ocular o consulta para taller"
                        value={nuevaCitaComentario}
                        onChange={(e) => setNuevaCitaComentario(e.target.value)}
                        style={{ width: "100%", padding: "10px 14px", borderRadius: 8, border: "1px solid #cbd5e1", fontSize: 14 }}
                      />
                    </div>

                    <div style={{ display: "flex", justifyContent: "flex-end", gap: 12 }}>
                      <button
                        type="button"
                        onClick={() => setMostrarModalCita(false)}
                        className="btn btn-secundario"
                      >
                        Cancelar
                      </button>
                      <button
                        type="submit"
                        className="btn btn-primario"
                        disabled={solicitarCitaMutation.isPending}
                      >
                        {solicitarCitaMutation.isPending ? "Enviando..." : "Enviar Solicitud"}
                      </button>
                    </div>
                  </form>
                </div>
              </div>
            )}

            {/* Lista de Solicitudes y Citas */}
            <div style={{ background: "#ffffff", borderRadius: 16, border: "1px solid #e2e8f0", padding: 24, boxShadow: "0 4px 6px -1px rgba(0,0,0,0.05)" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20, flexWrap: "wrap", gap: 12 }}>
                <div>
                  <h3 style={{ fontSize: 18, fontWeight: 800, color: "#1e293b", margin: 0 }}>
                    Historial de Solicitudes y Citas
                  </h3>
                  <p style={{ fontSize: 13, color: "#64748b", margin: "4px 0 0" }}>
                    Todas las citas agendadas bajo tu cuenta ({sesion.correo}) se actualizan automáticamente aquí.
                  </p>
                </div>

                <button
                  type="button"
                  onClick={() => citasQuery.refetch()}
                  className="btn btn-secundario"
                  style={{ fontSize: 13, padding: "6px 12px" }}
                >
                  🔄 Actualizar
                </button>
              </div>

              {citasQuery.isLoading ? (
                <div style={{ textAlign: "center", padding: "40px 0", color: "#64748b" }}>
                  <div className="spinner-sm" style={{ margin: "0 auto 10px" }} />
                  Cargando tus citas...
                </div>
              ) : citas.length === 0 ? (
                <div style={{ textAlign: "center", padding: "60px 20px", background: "#f8fafc", borderRadius: 12, border: "1px dashed #cbd5e1" }}>
                  <div style={{ fontSize: 36, marginBottom: 8 }}>📅</div>
                  <h4 style={{ fontSize: 16, fontWeight: 700, color: "#334155", margin: 0 }}>
                    No tienes solicitudes registradas aún
                  </h4>
                  <p style={{ fontSize: 13, color: "#64748b", maxWidth: 420, margin: "6px auto 16px" }}>
                    Haz clic en el botón de abajo para solicitar tu primera cita médica, terapia de rehabilitación o masaje TACTO.
                  </p>
                  <button
                    type="button"
                    className="btn btn-primario"
                    onClick={() => setMostrarModalCita(true)}
                  >
                    ➕ Solicitar Mi Primera Cita
                  </button>
                </div>
              ) : (
                <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
                  {citas.map((c) => (
                    <div
                      key={c.id}
                      style={{
                        border: "1px solid #e2e8f0",
                        borderRadius: 12,
                        padding: "16px 20px",
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        flexWrap: "wrap",
                        gap: 16,
                        background: "#ffffff",
                        transition: "all 0.2s ease",
                      }}
                    >
                      <div style={{ flex: "1 1 320px" }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 6 }}>
                          <span style={{ fontSize: 12, fontWeight: 700, color: "#2563eb", background: "#eff6ff", padding: "2px 8px", borderRadius: 6 }}>
                            SOL-{String(c.id).padStart(6, "0")}
                          </span>
                          {badgeEstado(c.estado)}
                        </div>
                        <h4 style={{ fontSize: 16, fontWeight: 800, color: "#0f172a", margin: "4px 0" }}>
                          {TipoServicioCitaTexto[c.tipoServicio as 1 | 2 | 3 | 4] ?? "Servicio Institucional"}
                        </h4>
                        <div style={{ fontSize: 13, color: "#475569", display: "flex", gap: 16, flexWrap: "wrap", marginTop: 4 }}>
                          <span>📍 {SedeTexto[c.sedePreferida as 1 | 2 | 3] ?? "Sede Central"}</span>
                          <span>🗓️ Fecha: {new Date(c.fechaPreferida).toLocaleDateString("es-DO", { year: "numeric", month: "long", day: "numeric" })}</span>
                        </div>
                        {c.comentario && (
                          <div style={{ fontSize: 12, color: "#64748b", marginTop: 6, fontStyle: "italic" }}>
                            "{c.comentario}"
                          </div>
                        )}
                      </div>

                      <div style={{ display: "flex", gap: 10 }}>
                        <button
                          type="button"
                          className="btn btn-secundario"
                          onClick={() => window.print()}
                          style={{ fontSize: 12, padding: "6px 12px" }}
                        >
                          🖨️ Imprimir
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </main>

      <PublicFooter />
    </div>
  );
}
