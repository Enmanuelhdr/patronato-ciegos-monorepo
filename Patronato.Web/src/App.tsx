import { Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { LandingPage } from "./pages/LandingPage";
import { LoginPage } from "./pages/LoginPage";
import { CambiarPasswordPage } from "./pages/CambiarPasswordPage";
import { AgendarCitaPage } from "./pages/AgendarCitaPage";
import { ServiciosPublicPage } from "./pages/public/ServiciosPublicPage";
import { SimuladorPublicPage } from "./pages/public/SimuladorPublicPage";
import { ImpactoPublicPage } from "./pages/public/ImpactoPublicPage";
import { ProductosPublicPage } from "./pages/public/ProductosPublicPage";
import { SedesPublicPage } from "./pages/public/SedesPublicPage";
import { ComoFuncionaPage } from "./pages/public/ComoFuncionaPage";
import { VerificarPage } from "./pages/public/VerificarPage";
import { InduccionPersonalPage } from "./pages/public/InduccionPersonalPage";
import { PortalPacientePage } from "./pages/public/PortalPacientePage";
import { CitasPage } from "./pages/CitasPage";
import { DashboardPage } from "./pages/DashboardPage";
import { BeneficiariosPage } from "./pages/BeneficiariosPage";
import { ConsultasPage } from "./pages/ConsultasPage";
import { RehabilitacionPage } from "./pages/RehabilitacionPage";
import { MasajesPage } from "./pages/MasajesPage";
import { PrestamosPage } from "./pages/PrestamosPage";
import { OrdenesPage } from "./pages/OrdenesPage";
import { DonacionesPage } from "./pages/DonacionesPage";
import { VoluntariosPage } from "./pages/VoluntariosPage";
import { TransaccionesPage } from "./pages/TransaccionesPage";
import { SucursalesPage } from "./pages/SucursalesPage";
import { UsuariosPage } from "./pages/UsuariosPage";
import { InventarioPage } from "./pages/InventarioPage";
import { AccesosWebPage } from "./pages/AccesosWebPage";

const SOLO_ADMIN = ["Administrador"];
const RECEPCION_O_ADMIN = ["Administrador", "Recepcionista"];
const GESTIONA_CITAS = ["Administrador", "Recepcionista", "MedicoOftalmologo", "InstructorRehabilitacion"];

export default function App() {
  return (
    <Routes>
      {/* Páginas públicas independientes con navegación fluida */}
      <Route path="/" element={<LandingPage />} />
      <Route path="/como-funciona" element={<ComoFuncionaPage />} />
      <Route path="/verificar" element={<VerificarPage />} />
      <Route path="/induccion" element={<InduccionPersonalPage />} />
      <Route path="/servicios" element={<ServiciosPublicPage />} />
      <Route path="/simulador" element={<SimuladorPublicPage />} />
      <Route path="/impacto" element={<ImpactoPublicPage />} />
      <Route path="/productos" element={<ProductosPublicPage />} />
      <Route path="/sedes" element={<SedesPublicPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/agendar-cita" element={<AgendarCitaPage />} />
      <Route path="/portal-paciente" element={<PortalPacientePage />} />
      <Route path="/mis-citas" element={<PortalPacientePage />} />
      {/* Fuera del <Layout>/ProtectedRoute a propósito: si estuviera adentro, un usuario
          con debeCambiarPassword=true entraría en un redirect loop contra sí misma. */}
      <Route path="/cambiar-password" element={<CambiarPasswordPage />} />

      {/* Portal interno del personal (Recepción / Administración) */}
      <Route
        path="/panel"
        element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="citas" element={<ProtectedRoute roles={GESTIONA_CITAS}><CitasPage /></ProtectedRoute>} />
        <Route path="beneficiarios" element={<BeneficiariosPage />} />
        <Route path="consultas" element={<ConsultasPage />} />
        <Route path="rehabilitacion" element={<RehabilitacionPage />} />
        <Route path="masajes" element={<ProtectedRoute roles={RECEPCION_O_ADMIN}><MasajesPage /></ProtectedRoute>} />
        <Route path="prestamos" element={<ProtectedRoute roles={RECEPCION_O_ADMIN}><PrestamosPage /></ProtectedRoute>} />
        <Route path="ordenes" element={<ProtectedRoute roles={RECEPCION_O_ADMIN}><OrdenesPage /></ProtectedRoute>} />
        <Route path="donaciones" element={<ProtectedRoute roles={RECEPCION_O_ADMIN}><DonacionesPage /></ProtectedRoute>} />
        <Route path="voluntarios" element={<ProtectedRoute roles={RECEPCION_O_ADMIN}><VoluntariosPage /></ProtectedRoute>} />
        <Route path="transacciones" element={<ProtectedRoute roles={SOLO_ADMIN}><TransaccionesPage /></ProtectedRoute>} />
        <Route path="sucursales" element={<ProtectedRoute roles={SOLO_ADMIN}><SucursalesPage /></ProtectedRoute>} />
        <Route path="inventario" element={<ProtectedRoute permiso="inventario.ver"><InventarioPage /></ProtectedRoute>} />
        <Route path="accesos-web" element={<ProtectedRoute permiso="web-usuarios.ver"><AccesosWebPage /></ProtectedRoute>} />
        <Route path="usuarios" element={<ProtectedRoute permiso="core-users.ver"><UsuariosPage /></ProtectedRoute>} />
      </Route>
    </Routes>
  );
}
