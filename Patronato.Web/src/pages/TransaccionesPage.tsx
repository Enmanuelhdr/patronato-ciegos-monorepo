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
    { encabezado: "Hora", render: (t) => new Date(t.fechaHora).toLocaleTimeString() },
    { encabezado: "No. Recibo", render: (t) => t.numeroRecibo },
    { encabezado: "Tipo", render: (t) => t.tipoTransaccion },
    { encabezado: "Concepto", render: (t) => t.concepto },
    { encabezado: "Cliente/Beneficiario", render: (t) => t.clienteOBeneficiario ?? "—" },
    { encabezado: "Método", render: (t) => t.metodoPago },
    { encabezado: "Monto", render: (t) => `RD$ ${t.montoTotal.toFixed(2)}` },
    { encabezado: "Cajero", render: (t) => t.cajero },
  ];

  return (
    <>
      <PageHeader
        titulo="Transacciones de Caja"
        subtitulo="Reporte administrativo de todo lo cobrado en la terminal de Caja (vía los códigos del Gateway)."
        accion={
          <div className="campo">
            <label>Fecha</label>
            <input type="date" value={fecha} onChange={(e) => setFecha(e.target.value)} />
          </div>
        }
      />

      {cuadre.data && (
        <div className="stats-grid">
          <StatCard etiqueta="TOTAL DEL DÍA" valor={`RD$ ${cuadre.data.totalGeneral.toFixed(2)}`} tono="azul" />
          <StatCard etiqueta="VENTAS POS" valor={`RD$ ${cuadre.data.porConcepto.ventasProductos.toFixed(2)}`} tono="verde" />
          <StatCard etiqueta="MASAJES TACTO" valor={`RD$ ${cuadre.data.porConcepto.masajesTacto.toFixed(2)}`} tono="morado" />
          <StatCard etiqueta="ABONOS + DONACIONES" valor={`RD$ ${(cuadre.data.porConcepto.abonosPrestamos + cuadre.data.porConcepto.donaciones).toFixed(2)}`} tono="ambar" />
          <StatCard etiqueta="CONSULTAS OFTALMOLÓGICAS" valor={`RD$ ${cuadre.data.porConcepto.consultas.toFixed(2)}`} tono="cyan" />
        </div>
      )}

      <DataTable columnas={columnas} filas={transacciones.data ?? []} claveFila={(t) => t.id} cargando={transacciones.isLoading} />
    </>
  );
}
