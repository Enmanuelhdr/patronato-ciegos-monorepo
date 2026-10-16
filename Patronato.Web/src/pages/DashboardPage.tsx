import { useState, useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { PageHeader, StatCard } from "../components/PageHeader";
import {
  getBeneficiarios,
  getConsultas,
  getCuadreCaja,
  getInventario,
  getMasajesPendientes,
  getOrdenesPendientes,
  getPrestamosActivos,
  getTransacciones,
} from "../api/endpoints";
import { useAuth } from "../context/AuthContext";
import { DataTable, type Columna } from "../components/DataTable";
import type { TransaccionCaja } from "../api/types";

type TipoRango = "hoy" | "7dias" | "mes" | "todo";

function calcularFechasFiltro(rango: TipoRango) {
  const hoy = new Date();
  const formato = (d: Date) => d.toISOString().slice(0, 10);

  if (rango === "hoy") {
    const f = formato(hoy);
    return { fecha: f, inicio: f, fin: f, label: "Hoy (" + f + ")" };
  }
  if (rango === "7dias") {
    const inicio = new Date();
    inicio.setDate(hoy.getDate() - 7);
    return {
      fecha: undefined,
      inicio: formato(inicio),
      fin: formato(hoy),
      label: `Últimos 7 días (${formato(inicio)} al ${formato(hoy)})`,
    };
  }
  if (rango === "mes") {
    const inicio = new Date(hoy.getFullYear(), hoy.getMonth(), 1);
    return {
      fecha: undefined,
      inicio: formato(inicio),
      fin: formato(hoy),
      label: `Este mes (${formato(inicio)} al ${formato(hoy)})`,
    };
  }
  return {
    fecha: undefined,
    inicio: "2020-01-01",
    fin: formato(hoy),
    label: `Histórico acumulado (hasta ${formato(hoy)})`,
  };
}

export function DashboardPage() {
  const { sesion } = useAuth();
  const esAdmin = sesion?.rol === "Administrador";

  const [rango, setRango] = useState<TipoRango>("hoy");

  // Cálculo de fechas según rango seleccionado
  const fechasFiltro = useMemo(() => calcularFechasFiltro(rango), [rango]);

  // Consultas de datos
  const beneficiarios = useQuery({ queryKey: ["beneficiarios"], queryFn: getBeneficiarios });
  const consultas = useQuery({ queryKey: ["consultas-resumen"], queryFn: getConsultas });
  const inventario = useQuery({ queryKey: ["inventario-resumen"], queryFn: getInventario });
  const masajes = useQuery({ queryKey: ["masajes-pendientes"], queryFn: getMasajesPendientes });
  const prestamos = useQuery({ queryKey: ["prestamos-activos", ""], queryFn: () => getPrestamosActivos() });
  const ordenes = useQuery({ queryKey: ["ordenes-pendientes"], queryFn: getOrdenesPendientes });

  const cuadre = useQuery({
    queryKey: ["cuadre-caja", fechasFiltro.inicio, fechasFiltro.fin],
    queryFn: () => getCuadreCaja(undefined, undefined, fechasFiltro.inicio, fechasFiltro.fin),
    enabled: esAdmin,
  });

  const transacciones = useQuery({
    queryKey: ["transacciones-caja-dashboard", fechasFiltro.inicio, fechasFiltro.fin],
    queryFn: () => getTransacciones(undefined, undefined, fechasFiltro.inicio, fechasFiltro.fin),
    enabled: esAdmin,
  });

  // Métricas calculadas para Stephen Few Dashboard
  const datosConceptos = useMemo(() => {
    if (!cuadre.data) return [];
    const p = cuadre.data.porConcepto;
    const total = cuadre.data.totalGeneral > 0 ? cuadre.data.totalGeneral : 1;

    return [
      {
        nombre: "Consultas Oftalmológicas",
        monto: p.consultas,
        porcentaje: (p.consultas / total) * 100,
        color: "#0284c7",
      },
      {
        nombre: "Masajes TACTO",
        monto: p.masajesTacto,
        porcentaje: (p.masajesTacto / total) * 100,
        color: "#059669",
      },
      {
        nombre: "Venta de Insumos (POS)",
        monto: p.ventasProductos,
        porcentaje: (p.ventasProductos / total) * 100,
        color: "#d97706",
      },
      {
        nombre: "Donaciones Institucionales",
        monto: p.donaciones,
        porcentaje: (p.donaciones / total) * 100,
        color: "#7c3aed",
      },
      {
        nombre: "Abonos a Préstamos",
        monto: p.abonosPrestamos,
        porcentaje: (p.abonosPrestamos / total) * 100,
        color: "#2563eb",
      },
    ].sort((a, b) => b.monto - a.monto);
  }, [cuadre.data]);

  const datosMetodos = useMemo(() => {
    if (!cuadre.data) return [];
    const m = cuadre.data.porMetodo;
    const total = cuadre.data.totalGeneral > 0 ? cuadre.data.totalGeneral : 1;

    return [
      { nombre: "Efectivo", monto: m.efectivo, porcentaje: (m.efectivo / total) * 100, color: "#059669" },
      { nombre: "Tarjeta", monto: m.tarjeta, porcentaje: (m.tarjeta / total) * 100, color: "#0284c7" },
      { nombre: "Transferencia", monto: m.transferencia, porcentaje: (m.transferencia / total) * 100, color: "#7c3aed" },
    ];
  }, [cuadre.data]);

  // Cartera de préstamos
  const metricaPrestamos = useMemo(() => {
    const lista = prestamos.data ?? [];
    const totalPrestado = lista.reduce((acc, p) => acc + p.montoAprobado, 0);
    const saldoPendiente = lista.reduce((acc, p) => acc + p.saldoPendiente, 0);
    const recuperado = totalPrestado - saldoPendiente;
    const tasaRecuperacion = totalPrestado > 0 ? (recuperado / totalPrestado) * 100 : 0;
    return { totalPrestado, saldoPendiente, recuperado, tasaRecuperacion };
  }, [prestamos.data]);

  // Exportar resumen a CSV / Excel
  const exportarReporteEjecutivo = () => {
    if (!cuadre.data) return;

    const lineas = [
      `"REPORTE EJECUTIVO DE GESTIÓN INSTITUCIONAL - PATRONATO NACIONAL DE CIEGOS"`,
      `"Período Evaluado:","${fechasFiltro.label}"`,
      `"Generado por:","${sesion?.nombre} (${sesion?.rol})"`,
      `"Fecha de Emisión:","${new Date().toLocaleString()}"`,
      "",
      `"RESUMEN GENERAL DE RECAUDACIÓN"`,
      `"Total General Recaudado:","RD$ ${cuadre.data.totalGeneral.toFixed(2)}"`,
      `"Transacciones Válidas:","${cuadre.data.cantidadTransacciones}"`,
      `"Total Anulado en Auditoría:","RD$ ${(cuadre.data.totalAnulado ?? 0).toFixed(2)}"`,
      "",
      `"DESGLOSE POR CONCEPTO INSTITUCIONAL"`,
      `"Concepto","Monto Recaudado (RD$)","Participación (%)"`,
      ...datosConceptos.map((c) => `"${c.nombre}","${c.monto.toFixed(2)}","${c.porcentaje.toFixed(1)}%"`),
      "",
      `"DESGLOSE POR MÉTODO DE PAGO"`,
      `"Método","Monto (RD$)","Participación (%)"`,
      ...datosMetodos.map((m) => `"${m.nombre}","${m.monto.toFixed(2)}","${m.porcentaje.toFixed(1)}%"`),
      "",
      `"ESTADO DE CARTERA DE PRÉSTAMOS DE EMPRENDIMIENTO"`,
      `"Capital Colocado:","RD$ ${metricaPrestamos.totalPrestado.toFixed(2)}"`,
      `"Capital Recuperado:","RD$ ${metricaPrestamos.recuperado.toFixed(2)}"`,
      `"Saldo Pendiente en Calle:","RD$ ${metricaPrestamos.saldoPendiente.toFixed(2)}"`,
      `"Tasa de Recuperación:","${metricaPrestamos.tasaRecuperacion.toFixed(1)}%"`,
    ];

    const blob = new Blob(["\uFEFF" + lineas.join("\r\n")], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `reporte-ejecutivo-${rango}-${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  // Columnas para la tabla de transacciones recientes
  const columnasTransacciones: Columna<TransaccionCaja>[] = [
    {
      encabezado: "Recibo",
      render: (t) => <strong>{t.numeroRecibo}</strong>,
      valorTexto: (t) => t.numeroRecibo,
    },
    {
      encabezado: "Fecha y Hora",
      render: (t) => new Date(t.fechaHora).toLocaleString([], { dateStyle: "short", timeStyle: "short" }),
      valorTexto: (t) => t.fechaHora,
    },
    {
      encabezado: "Concepto",
      render: (t) => t.concepto,
      valorTexto: (t) => t.concepto,
    },
    {
      encabezado: "Cliente / Beneficiario",
      render: (t) => t.clienteOBeneficiario || "Consumidor Final",
      valorTexto: (t) => t.clienteOBeneficiario || "Consumidor Final",
    },
    {
      encabezado: "Método",
      render: (t) => (
        <span
          className={`tag ${
            t.metodoPago === "Efectivo" ? "tag-verde" : t.metodoPago === "Tarjeta" ? "tag-azul" : "tag-morado"
          }`}
        >
          {t.metodoPago}
        </span>
      ),
      valorTexto: (t) => t.metodoPago,
    },
    {
      encabezado: "Monto",
      render: (t) => <strong>RD$ {t.montoTotal.toFixed(2)}</strong>,
      valorTexto: (t) => t.montoTotal.toFixed(2),
    },
    {
      encabezado: "Cajero",
      render: (t) => t.cajero,
      valorTexto: (t) => t.cajero,
    },
  ];

  const [saludo] = useState(() => {
    const hora = new Date().getHours();
    return hora < 12 ? "¡Buenos días" : hora < 18 ? "¡Buenas tardes" : "¡Buenas noches";
  });

  return (
    <>
      <PageHeader
        titulo={`Panel Ejecutivo · ${sesion?.nombre}`}
        subtitulo="Tablero integral de dirección y operaciones en tiempo real del Patronato."
        accion={
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            {esAdmin && (
              <button
                type="button"
                className="btn btn-secundario"
                onClick={exportarReporteEjecutivo}
                disabled={!cuadre.data}
                aria-label="Descargar reporte ejecutivo en formato CSV y Excel"
              >
                <span aria-hidden="true">📥 </span>
                <span>Exportar Reporte Ejecutivo</span>
              </button>
            )}
            <button
              type="button"
              className="btn btn-primario"
              onClick={() => window.print()}
              aria-label="Imprimir informe en formato PDF"
            >
              <span aria-hidden="true">🖨️ </span>
              <span>Imprimir Informe</span>
            </button>
          </div>
        }
      />

      {/* Banner de Bienvenida y Centro de Mando Operativo */}
      <div
        style={{
          background: "linear-gradient(135deg, #034ea2 0%, #0284c7 100%)",
          borderRadius: 16,
          padding: "24px 28px",
          color: "white",
          marginBottom: 20,
          boxShadow: "0 10px 25px -5px rgba(3, 78, 162, 0.3)",
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          flexWrap: "wrap",
          gap: 16,
        }}
      >
        <div>
          <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 6 }}>
            <span
              style={{
                background: "rgba(255, 255, 255, 0.2)",
                padding: "4px 12px",
                borderRadius: 9999,
                fontSize: 11,
                fontWeight: 700,
                letterSpacing: "0.05em",
                textTransform: "uppercase",
              }}
            >
              {sesion?.rol} · Sede Central
            </span>
            <span style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 12, color: "#bfdbfe" }}>
              <span
                style={{
                  width: 8,
                  height: 8,
                  borderRadius: "50%",
                  background: "#34d399",
                  boxShadow: "0 0 8px #34d399",
                }}
              />
              Base de Datos Conectada en Tiempo Real
            </span>
          </div>
          <h2 style={{ fontSize: 24, fontWeight: 800, margin: "0 0 4px", color: "white" }}>
            {saludo}, {sesion?.nombre}
          </h2>
          <p style={{ margin: 0, fontSize: 13, color: "#e0f2fe" }}>
            Operaciones del Patronato Nacional de Ciegos · República Dominicana
          </p>
        </div>

        {/* Atajos Rápidos */}
        <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
          <Link
            to="/panel/citas"
            className="btn"
            style={{ background: "#ffffff", color: "#034ea2", fontWeight: 700, fontSize: 13 }}
          >
            🗓️ Agendar / Revisar Cita
          </Link>
          <Link
            to="/panel/beneficiarios"
            className="btn"
            style={{ background: "rgba(255, 255, 255, 0.15)", color: "#ffffff", border: "1px solid rgba(255, 255, 255, 0.3)", fontWeight: 700, fontSize: 13 }}
          >
            🧑‍🦯 Beneficiarios
          </Link>
          <Link
            to="/panel/ordenes"
            className="btn"
            style={{ background: "rgba(255, 255, 255, 0.15)", color: "#ffffff", border: "1px solid rgba(255, 255, 255, 0.3)", fontWeight: 700, fontSize: 13 }}
          >
            📋 Emitir Orden a Caja
          </Link>
          <Link
            to="/panel/inventario"
            className="btn"
            style={{ background: "rgba(255, 255, 255, 0.15)", color: "#ffffff", border: "1px solid rgba(255, 255, 255, 0.3)", fontWeight: 700, fontSize: 13 }}
          >
            📦 Inventario
          </Link>
        </div>
      </div>

      {/* Barra de Filtro de Período Temporal (Stephen Few) */}
      <div className="dashboard-toolbar" role="region" aria-label="Filtro temporal del tablero">
        <div className="filtro-rango-grupo">
          <span style={{ fontSize: 12, fontWeight: 700, color: "var(--slate-700)", marginRight: 4 }}>
            Período:
          </span>
          <button
            type="button"
            className={`btn-filtro ${rango === "hoy" ? "activo" : ""}`}
            onClick={() => setRango("hoy")}
          >
            Hoy
          </button>
          <button
            type="button"
            className={`btn-filtro ${rango === "7dias" ? "activo" : ""}`}
            onClick={() => setRango("7dias")}
          >
            Últimos 7 días
          </button>
          <button
            type="button"
            className={`btn-filtro ${rango === "mes" ? "activo" : ""}`}
            onClick={() => setRango("mes")}
          >
            Este mes
          </button>
          <button
            type="button"
            className={`btn-filtro ${rango === "todo" ? "activo" : ""}`}
            onClick={() => setRango("todo")}
          >
            Histórico completo
          </button>
        </div>
        <div style={{ fontSize: 12, color: "var(--slate-500)" }} role="status" aria-live="polite">
          Mostrando datos de: <strong>{fechasFiltro.label}</strong>
        </div>
      </div>

      {/* Tarjetas KPI de Estado Operativo */}
      <div className="stats-grid" role="region" aria-label="Indicadores clave de rendimiento">
        <StatCard
          etiqueta="BENEFICIARIOS REGISTRADOS"
          valor={String(beneficiarios.data?.length ?? "...")}
          tono="azul"
        />
        <StatCard
          etiqueta="CONSULTAS OFTALMOLÓGICAS"
          valor={String(consultas.data?.length ?? "...")}
          tono="azul"
        />
        <StatCard
          etiqueta="CATÁLOGO DE PRODUCTOS / INSUMOS"
          valor={String(inventario.data?.length ?? "...")}
          tono="verde"
        />
        <StatCard
          etiqueta="MASAJES PENDIENTES DE COBRO"
          valor={String(masajes.data?.length ?? "...")}
          tono="ambar"
        />
        <StatCard
          etiqueta="PRÉSTAMOS ACTIVOS"
          valor={String(prestamos.data?.length ?? "...")}
          tono="morado"
        />
        <StatCard
          etiqueta="ÓRDENES PENDIENTES EN CAJA"
          valor={String(ordenes.data?.length ?? "...")}
          tono="verde"
        />
      </div>

      {/* Sección Financiera y Visualizaciones para Dirección / Admin */}
      {esAdmin && (
        <>
          {/* Fila de Tarjetas Financieras del Período */}
          <div className="stats-grid" role="region" aria-label="Métricas financieras del período">
            <StatCard
              etiqueta="RECAUDACIÓN EN EL PERÍODO"
              valor={cuadre.data ? `RD$ ${cuadre.data.totalGeneral.toFixed(2)}` : "..."}
              tono="azul"
            />
            <StatCard
              etiqueta="TRANSACCIONES REGISTRADAS"
              valor={cuadre.data ? String(cuadre.data.cantidadTransacciones) : "..."}
              tono="verde"
            />
            <StatCard
              etiqueta="COBROS EN EFECTIVO"
              valor={cuadre.data ? `RD$ ${cuadre.data.porMetodo.efectivo.toFixed(2)}` : "..."}
              tono="ambar"
            />
            <StatCard
              etiqueta="PAGOS DIGITALES (TARJETA + TRANSF)"
              valor={
                cuadre.data
                  ? `RD$ ${(cuadre.data.porMetodo.tarjeta + cuadre.data.porMetodo.transferencia).toFixed(2)}`
                  : "..."
              }
              tono="morado"
            />
          </div>

          {/* Cuadrícula de Gráficos Comparativos y Tendencias (Stephen Few) */}
          <div className="dashboard-grid-graficos">
            {/* Gráfico 1: Desglose por Concepto */}
            <div className="grafico-card">
              <div className="grafico-card-header">
                <h3>Distribución de Ingresos por Concepto</h3>
                <span>{fechasFiltro.label}</span>
              </div>
              {cuadre.isLoading ? (
                <p style={{ color: "var(--slate-500)", fontSize: 13 }}>Cargando gráfico...</p>
              ) : cuadre.data?.totalGeneral === 0 ? (
                <p style={{ color: "var(--slate-500)", fontSize: 13 }}>No hay recaudación registrada en este período.</p>
              ) : (
                <div className="barra-comparativa">
                  {datosConceptos.map((item) => (
                    <div key={item.nombre} className="barra-fila">
                      <div className="barra-fila-label">
                        <span>{item.nombre}</span>
                        <strong>
                          RD$ {item.monto.toFixed(2)} ({item.porcentaje.toFixed(1)}%)
                        </strong>
                      </div>
                      <div className="barra-track" aria-hidden="true">
                        <div
                          className="barra-progreso"
                          style={{
                            width: `${Math.max(item.porcentaje, 2)}%`,
                            backgroundColor: item.color,
                          }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Gráfico 2: Desglose por Método de Pago */}
            <div className="grafico-card">
              <div className="grafico-card-header">
                <h3>Composición por Medio de Pago</h3>
                <span>Seguridad y Digitalización</span>
              </div>
              {cuadre.isLoading ? (
                <p style={{ color: "var(--slate-500)", fontSize: 13 }}>Cargando gráfico...</p>
              ) : cuadre.data?.totalGeneral === 0 ? (
                <p style={{ color: "var(--slate-500)", fontSize: 13 }}>No hay transacciones registradas en este período.</p>
              ) : (
                <>
                  <div className="barra-comparativa">
                    {datosMetodos.map((item) => (
                      <div key={item.nombre} className="barra-fila">
                        <div className="barra-fila-label">
                          <span>{item.nombre}</span>
                          <strong>
                            RD$ {item.monto.toFixed(2)} ({item.porcentaje.toFixed(1)}%)
                          </strong>
                        </div>
                        <div className="barra-track" aria-hidden="true">
                          <div
                            className="barra-progreso"
                            style={{
                              width: `${Math.max(item.porcentaje, 2)}%`,
                              backgroundColor: item.color,
                            }}
                          />
                        </div>
                      </div>
                    ))}
                  </div>

                  <div
                    style={{
                      marginTop: 18,
                      padding: "10px 14px",
                      background: "var(--slate-100)",
                      borderRadius: 8,
                      fontSize: 12,
                      color: "var(--slate-700)",
                    }}
                  >
                    <strong>Índice de Digitalización: </strong>
                    {cuadre.data?.totalGeneral
                      ? `${(
                          ((cuadre.data.porMetodo.tarjeta + cuadre.data.porMetodo.transferencia) /
                            cuadre.data.totalGeneral) *
                          100
                        ).toFixed(1)}% de cobros electrónicos.`
                      : "0%"}
                  </div>
                </>
              )}
            </div>

            {/* Gráfico 3: Estado de la Cartera de Microcréditos */}
            <div className="grafico-card" style={{ gridColumn: "1 / -1" }}>
              <div className="grafico-card-header">
                <h3>Cartera de Préstamos para Emprendimiento</h3>
                <span>
                  {prestamos.data?.length ?? 0} microcréditos activos · Tasa de Recuperación:{" "}
                  {metricaPrestamos.tasaRecuperacion.toFixed(1)}%
                </span>
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 14 }}>
                <div style={{ background: "#f8fafc", border: "1px solid #e2e8f0", borderRadius: 8, padding: 14 }}>
                  <span style={{ fontSize: 11, color: "var(--slate-500)", fontWeight: 700 }}>CAPITAL COLOCADO</span>
                  <div style={{ fontSize: 20, fontWeight: 700, color: "var(--slate-900)", marginTop: 4 }}>
                    RD$ {metricaPrestamos.totalPrestado.toFixed(2)}
                  </div>
                </div>
                <div style={{ background: "#ecfdf5", border: "1px solid #a7f3d0", borderRadius: 8, padding: 14 }}>
                  <span style={{ fontSize: 11, color: "#065f46", fontWeight: 700 }}>CAPITAL RECUPERADO</span>
                  <div style={{ fontSize: 20, fontWeight: 700, color: "#065f46", marginTop: 4 }}>
                    RD$ {metricaPrestamos.recuperado.toFixed(2)}
                  </div>
                </div>
                <div style={{ background: "#fef3c7", border: "1px solid #fde68a", borderRadius: 8, padding: 14 }}>
                  <span style={{ fontSize: 11, color: "#92400e", fontWeight: 700 }}>SALDO PENDIENTE EN CALLE</span>
                  <div style={{ fontSize: 20, fontWeight: 700, color: "#92400e", marginTop: 4 }}>
                    RD$ {metricaPrestamos.saldoPendiente.toFixed(2)}
                  </div>
                </div>
              </div>

              {/* Barra de progreso de amortización de cartera */}
              <div style={{ marginTop: 14 }}>
                <div style={{ display: "flex", justifyContent: "space-between", fontSize: 12, marginBottom: 4 }}>
                  <span>Progreso de Amortización Institucional</span>
                  <strong>{metricaPrestamos.tasaRecuperacion.toFixed(1)}% recuperado</strong>
                </div>
                <div className="barra-track" aria-hidden="true">
                  <div
                    className="barra-progreso"
                    style={{
                      width: `${Math.min(metricaPrestamos.tasaRecuperacion, 100)}%`,
                      backgroundColor: "#059669",
                    }}
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Tabla de Movimientos del Período */}
          <div className="panel">
            <h2>Movimientos y Auditoría de Caja ({fechasFiltro.label})</h2>
            <DataTable
              columnas={columnasTransacciones}
              filas={transacciones.data ?? []}
              claveFila={(t) => t.id}
              cargando={transacciones.isLoading}
              tituloTabla="Movimientos y transacciones de caja del período"
              nombreArchivo={`transacciones-${rango}`}
              vacio="No se encontraron transacciones registradas en este período."
            />
          </div>
        </>
      )}

      {/* Sección Operativa para Recepción y Personal */}
      {!esAdmin && (
        <>
          {/* Tarjetas de Acceso Rápido y Flujo Operativo */}
          <div className="panel">
            <h2 style={{ marginBottom: 14 }}>Accesos Rápidos de Recepción y Atención</h2>
            <div className="reception-actions-grid">
              <Link to="/panel/citas" className="reception-action-card">
                <span className="action-icon">🗓️</span>
                <div>
                  <strong>Solicitudes de Cita</strong>
                  <p>Revisar y confirmar citas web de oftalmología y masajes</p>
                </div>
              </Link>
              <Link to="/panel/beneficiarios" className="reception-action-card">
                <span className="action-icon">🧑‍🦯</span>
                <div>
                  <strong>Directorio de Beneficiarios</strong>
                  <p>{beneficiarios.data?.length ?? 0} personas registradas con ficha médica</p>
                </div>
              </Link>
              <Link to="/panel/ordenes" className="reception-action-card">
                <span className="action-icon">📋</span>
                <div>
                  <strong>Nueva Orden para Caja</strong>
                  <p>Emitir orden de cobro para bastones, suapers o terapias</p>
                </div>
              </Link>
              <Link to="/panel/masajes" className="reception-action-card">
                <span className="action-icon">💆</span>
                <div>
                  <strong>Centro de Masajes TACTO</strong>
                  <p>{masajes.data?.length ?? 0} masajes en programación</p>
                </div>
              </Link>
            </div>
          </div>

          {/* Tablero de Beneficiarios Recientes */}
          <div className="panel">
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
              <div>
                <h2 style={{ margin: 0 }}>Beneficiarios Registrados Recientemente</h2>
                <p style={{ margin: "4px 0 0", color: "var(--slate-500)", fontSize: 13 }}>
                  Últimos ingresos al sistema institucional con seguimiento de rehabilitación
                </p>
              </div>
              <Link to="/panel/beneficiarios" className="btn btn-secundario">
                Ver todos ({beneficiarios.data?.length ?? 0}) →
              </Link>
            </div>

            <div className="tabla-wrapper">
              <table className="tabla">
                <thead>
                  <tr>
                    <th>Cédula</th>
                    <th>Nombres y Apellidos</th>
                    <th>Condición Visual</th>
                    <th>Sede Asignada</th>
                    <th>Teléfono</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {(beneficiarios.data ?? []).slice(0, 8).map((b) => (
                    <tr key={b.id}>
                      <td><code>{b.cedula}</code></td>
                      <td><strong>{b.nombres} {b.apellidos}</strong></td>
                      <td><span className="tag tag-azul">{b.condicionVisual}</span></td>
                      <td>{b.sedeAsignada}</td>
                      <td>{b.telefono}</td>
                      <td>
                        <span className={`tag ${b.activo ? "tag-verde" : "tag-rojo"}`}>
                          {b.activo ? "Activo" : "Inactivo"}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}

      {/* Accesos y Guía de Operaciones */}
      <div className="panel">
        <h2>Flujo Operativo Institucional</h2>
        <p style={{ color: "var(--slate-500)", fontSize: 13, lineHeight: 1.6 }}>
          <strong>Separación de Funciones: </strong>
          El personal de <strong>Recepción</strong> agenda masajes TACTO, coordina consultas médicas y genera
          órdenes de insumos. La <strong>Administración</strong> evalúa y aprueba los préstamos de emprendimiento.
          La <strong>Caja</strong> se encarga exclusivamente de recibir los pagos y cuotas de servicios ya
          aprobados, emitiendo los comprobantes fiscales y comprobantes con código de transacción auditados.
        </p>
      </div>
    </>
  );
}
