import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { PageHeader, StatCard } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { getCuadreCaja, getTransacciones } from "../api/endpoints";
import type { TransaccionCaja } from "../api/types";

export function TransaccionesPage() {
  const [fecha, setFecha] = useState(() => new Date().toISOString().slice(0, 10));

  const transacciones = useQuery({ queryKey: ["transacciones", fecha], queryFn: () => getTransacciones(fecha) });
  const cuadre = useQuery({ queryKey: ["cuadre-caja", fecha], queryFn: () => getCuadreCaja(fecha) });

  const columnas: Columna<TransaccionCaja>[] = [
    {
      encabezado: "Hora",
      render: (t) => new Date(t.fechaHora).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
      valorTexto: (t) => new Date(t.fechaHora).toLocaleTimeString(),
    },
    {
      encabezado: "No. Recibo",
      render: (t) => (
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          <strong>{t.numeroRecibo}</strong>
          {t.esAnulada && (
            <span
              className="tag tag-rojo"
              title={`Anulada por ${t.anuladoPor ?? "Supervisor"}: ${t.motivoAnulacion ?? "Sin motivo"}`}
            >
              ANULADO
            </span>
          )}
        </div>
      ),
      valorTexto: (t) => `${t.numeroRecibo} ${t.esAnulada ? "ANULADO" : ""}`,
    },
    {
      encabezado: "Tipo",
      render: (t) => t.tipoTransaccion,
      valorTexto: (t) => t.tipoTransaccion,
    },
    {
      encabezado: "Concepto",
      render: (t) => (
        <div>
          <div>{t.concepto}</div>
          {t.esAnulada && t.motivoAnulacion && (
            <div style={{ fontSize: 11, color: "var(--rojo)", marginTop: 2 }}>
              Motivo anulación: {t.motivoAnulacion}
            </div>
          )}
        </div>
      ),
      valorTexto: (t) => `${t.concepto} ${t.motivoAnulacion ?? ""}`,
    },
    {
      encabezado: "Cliente/Beneficiario",
      render: (t) => t.clienteOBeneficiario ?? "Consumidor Final",
      valorTexto: (t) => t.clienteOBeneficiario ?? "Consumidor Final",
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
      render: (t) => (
        <strong style={{ color: t.esAnulada ? "var(--slate-500)" : "inherit", textDecoration: t.esAnulada ? "line-through" : "none" }}>
          RD$ {t.montoTotal.toFixed(2)}
        </strong>
      ),
      valorTexto: (t) => t.montoTotal.toFixed(2),
    },
    {
      encabezado: "Cajero",
      render: (t) => t.cajero,
      valorTexto: (t) => t.cajero,
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Transacciones de Caja"
        subtitulo="Reporte administrativo y auditoría de recaudación de caja."
        accion={
          <div className="campo">
            <label htmlFor="input-fecha-transacciones">Fecha de consulta</label>
            <input
              id="input-fecha-transacciones"
              type="date"
              value={fecha}
              onChange={(e) => setFecha(e.target.value)}
            />
          </div>
        }
      />

      {cuadre.data && (
        <div className="stats-grid" role="region" aria-label="Resumen de cuadre de caja">
          <StatCard etiqueta="TOTAL DEL DÍA" valor={`RD$ ${cuadre.data.totalGeneral.toFixed(2)}`} tono="azul" />
          <StatCard etiqueta="VENTAS POS" valor={`RD$ ${cuadre.data.porConcepto.ventasProductos.toFixed(2)}`} tono="verde" />
          <StatCard etiqueta="MASAJES TACTO" valor={`RD$ ${cuadre.data.porConcepto.masajesTacto.toFixed(2)}`} tono="morado" />
          <StatCard etiqueta="ABONOS + DONACIONES" valor={`RD$ ${(cuadre.data.porConcepto.abonosPrestamos + cuadre.data.porConcepto.donaciones).toFixed(2)}`} tono="ambar" />
          <StatCard etiqueta="CONSULTAS OFTALMOLÓGICAS" valor={`RD$ ${cuadre.data.porConcepto.consultas.toFixed(2)}`} tono="cyan" />
        </div>
      )}

      {cuadre.data?.cantidadAnuladas !== undefined && cuadre.data.cantidadAnuladas > 0 && (
        <div className="alerta alerta-ambar" role="status" aria-live="polite">
          <strong aria-hidden="true">⚠️ Auditoría: </strong>
          Se registraron <strong>{cuadre.data.cantidadAnuladas}</strong> transacciones anuladas por un monto total de
          <strong> RD$ {(cuadre.data.totalAnulado ?? 0).toFixed(2)}</strong>.
        </div>
      )}

      <DataTable
        columnas={columnas}
        filas={transacciones.data ?? []}
        claveFila={(t) => t.id}
        cargando={transacciones.isLoading}
        tituloTabla={`Transacciones de caja del día ${fecha}`}
        nombreArchivo={`transacciones-caja-${fecha}`}
        vacio={`No hay transacciones registradas el ${fecha}.`}
      />
    </>
  );
}
