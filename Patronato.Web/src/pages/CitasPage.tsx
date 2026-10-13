import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { cancelarCita, confirmarCita, eliminarCita, getCitas } from "../api/endpoints";
import { useAuth } from "../context/AuthContext";
import { EstadoCitaTexto, SedeTexto, TipoServicioCitaTexto, type SolicitudCita } from "../api/types";

export function CitasPage() {
  const qc = useQueryClient();
  const { sesion } = useAuth();
  const { data, isLoading } = useQuery({ queryKey: ["citas"], queryFn: getCitas });

  const invalidar = () => qc.invalidateQueries({ queryKey: ["citas"] });
  const confirmar = useMutation({ mutationFn: confirmarCita, onSuccess: invalidar });
  const cancelar = useMutation({ mutationFn: cancelarCita, onSuccess: invalidar });
  const eliminar = useMutation({ mutationFn: eliminarCita, onSuccess: invalidar });

  const tagEstado = (estado: number) => {
    const clase = estado === 2 ? "tag-verde" : estado === 3 ? "tag-gris" : "tag-ambar";
    return <span className={`tag ${clase}`}>{EstadoCitaTexto[estado as 1 | 2 | 3]}</span>;
  };

  const columnas: Columna<SolicitudCita>[] = [
    { encabezado: "Solicitante", render: (c) => c.nombreSolicitante },
    { encabezado: "Teléfono", render: (c) => c.telefono },
    { encabezado: "Servicio", render: (c) => TipoServicioCitaTexto[c.tipoServicio] },
    { encabezado: "Sede preferida", render: (c) => SedeTexto[c.sedePreferida] },
    { encabezado: "Fecha preferida", render: (c) => new Date(c.fechaPreferida).toLocaleDateString() },
    { encabezado: "Estado", render: (c) => tagEstado(c.estado) },
    {
      encabezado: "Acciones",
      render: (c) => (
        <div style={{ display: "flex", gap: 6 }}>
          {c.estado === 1 && (
            <>
              <button className="btn btn-exito" disabled={confirmar.isPending} onClick={() => confirmar.mutate(c.id)}>
                Confirmar
              </button>
              <button className="btn btn-secundario" disabled={cancelar.isPending} onClick={() => cancelar.mutate(c.id)}>
                Cancelar
              </button>
            </>
          )}
          {sesion?.rol === "Administrador" && (
            <button
              className="btn btn-peligro"
              disabled={eliminar.isPending}
              onClick={() => {
                if (confirm(`¿Eliminar la solicitud de ${c.nombreSolicitante}?`)) eliminar.mutate(c.id);
              }}
            >
              Eliminar
            </button>
          )}
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        titulo="Solicitudes de Cita"
        subtitulo="Pedidos de agendamiento enviados desde el sitio público. Confirma o cancela según disponibilidad."
      />
      <DataTable columnas={columnas} filas={data ?? []} claveFila={(c) => c.id} cargando={isLoading} />
    </>
  );
}
