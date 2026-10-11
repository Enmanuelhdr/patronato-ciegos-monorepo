import { useQuery } from "@tanstack/react-query";
import { PageHeader, StatCard } from "../components/PageHeader";
import { getBeneficiarios, getCuadreCaja, getMasajesPendientes, getOrdenesPendientes, getPrestamosActivos } from "../api/endpoints";
import { useAuth } from "../context/AuthContext";

export function DashboardPage() {
  const { sesion } = useAuth();
  const esAdmin = sesion?.rol === "Administrador";

  const beneficiarios = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const masajes = useQuery({ queryKey: ["masajes-pendientes"], queryFn: getMasajesPendientes });
  const prestamos = useQuery({ queryKey: ["prestamos-activos", ""], queryFn: () => getPrestamosActivos() });
  const ordenes = useQuery({ queryKey: ["ordenes-pendientes"], queryFn: getOrdenesPendientes });
  const cuadre = useQuery({ queryKey: ["cuadre-caja"], queryFn: () => getCuadreCaja(), enabled: esAdmin });

  return (
    <>
      <PageHeader
        titulo={`Bienvenido, ${sesion?.nombre}`}
        subtitulo="Vista general del Patronato. Todos los datos vienen en vivo del Core a través del Gateway."
      />

      <div className="stats-grid">
        <StatCard etiqueta="BENEFICIARIOS REGISTRADOS" valor={String(beneficiarios.data?.length ?? "...")} tono="azul" />
        <StatCard etiqueta="MASAJES PENDIENTES DE COBRO" valor={String(masajes.data?.length ?? "...")} tono="ambar" />
        <StatCard etiqueta="PRÉSTAMOS ACTIVOS" valor={String(prestamos.data?.length ?? "...")} tono="morado" />
        <StatCard etiqueta="ÓRDENES PENDIENTES EN CAJA" valor={String(ordenes.data?.length ?? "...")} tono="verde" />
      </div>

      {esAdmin && (
        <div className="panel">
          <h2>Cuadre de caja de hoy</h2>
          {cuadre.isLoading && <p>Cargando...</p>}
          {cuadre.data && (
            <div className="stats-grid" style={{ marginBottom: 0 }}>
              <StatCard etiqueta="TOTAL RECAUDADO HOY" valor={`RD$ ${cuadre.data.totalGeneral.toFixed(2)}`} tono="azul" />
              <StatCard etiqueta="TRANSACCIONES" valor={String(cuadre.data.cantidadTransacciones)} tono="verde" />
              <StatCard etiqueta="EFECTIVO" valor={`RD$ ${cuadre.data.porMetodo.efectivo.toFixed(2)}`} tono="ambar" />
              <StatCard etiqueta="TARJETA + TRANSFERENCIA" valor={`RD$ ${(cuadre.data.porMetodo.tarjeta + cuadre.data.porMetodo.transferencia).toFixed(2)}`} tono="morado" />
            </div>
          )}
        </div>
      )}

      <div className="panel">
        <h2>Accesos rápidos de Recepción</h2>
        <p style={{ color: "#64748b", fontSize: 13 }}>
          Agenda masajes TACTO, otorga préstamos de emprendimiento y genera órdenes de pedido desde sus
          respectivas secciones en el menú. El cobro de todas estas operaciones se realiza exclusivamente en
          la Caja (terminal de cajero).
        </p>
      </div>
    </>
  );
}
