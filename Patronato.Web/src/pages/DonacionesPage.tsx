import { useQuery } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { getDonaciones } from "../api/endpoints";
import type { Donacion } from "../api/types";

export function DonacionesPage() {
  const { data, isLoading } = useQuery({ queryKey: ["donaciones"], queryFn: getDonaciones });

  const columnas: Columna<Donacion>[] = [
    { encabezado: "Fecha", render: (d) => new Date(d.fecha).toLocaleDateString() },
    { encabezado: "Donante", render: (d) => d.donante },
    { encabezado: "Cédula/RNC", render: (d) => d.rncCedula },
    { encabezado: "Monto", render: (d) => `RD$ ${d.monto.toFixed(2)}` },
    { encabezado: "Método de pago", render: (d) => d.metodoPago },
    { encabezado: "NCF", render: (d) => <span className={`tag ${d.requiereComprobanteFiscal ? "tag-azul" : "tag-gris"}`}>{d.requiereComprobanteFiscal ? "Requiere" : "No requiere"}</span> },
  ];

  return (
    <>
      <PageHeader
        titulo="Donaciones"
        subtitulo="Reporte de aportes institucionales. El registro y recibo oficial se emite en la ventanilla de Caja."
      />
      <DataTable columnas={columnas} filas={data ?? []} claveFila={(d) => d.id} cargando={isLoading} />
    </>
  );
}
