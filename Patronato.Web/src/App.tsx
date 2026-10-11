import { Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { LandingPage } from "./pages/LandingPage";
import { LoginPage } from "./pages/LoginPage";
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

const SOLO_ADMIN = ["Administrador"];
const RECEPCION_O_ADMIN = ["Administrador", "Recepcionista"];

export default function App() {
  return (
    <Routes>
      {/* Pública: lo primero que ve cualquier visitante, sin necesidad de iniciar sesión */}
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<LoginPage />} />

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
        <Route path="usuarios" element={<ProtectedRoute roles={SOLO_ADMIN}><UsuariosPage /></ProtectedRoute>} />
      </Route>
    </Routes>
  );
}
