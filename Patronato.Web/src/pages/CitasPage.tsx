import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { cancelarCita, confirmarCita, eliminarCita, getCitas } from "../api/endpoints";
import { useAuth } from "../context/AuthContext";
import { EstadoCitaTexto, SedeTexto, TipoServicioCitaTexto, type SolicitudCita } from "../api/types";

// Para los tipos de servicio que exigen que el solicitante esté registrado como
// Beneficiario antes de poder atenderlo (Consulta/Rehabilitación), un link directo
// a la pantalla siguiente evita que Recepción tenga que adivinar el camino.
const SIGUIENTE_PASO: Partial<Record<number, { ruta: string; etiqueta: string }>> = {
  1: { ruta: "/panel/consultas", etiqueta: "Ir a Consultas →" },
  2: { ruta: "/panel/rehabilitacion", etiqueta: "Ir a Rehabilitación →" },
};

export function CitasPage() {
  const qc = useQueryClient();
  const { sesion } = useAuth();
  const [nota, setNota] = useState<string | null>(null);
  const { data, isLoading } = useQuery({
    queryKey: ["citas"],
    queryFn: getCitas,
    refetchInterval: 20000,
    refetchOnWindowFocus: true,
  });

  const invalidar = () => {
    qc.invalidateQueries({ queryKey: ["citas"] });
    // Confirmar una cita de Masaje TACTO crea una sesión pendiente de cobro en Caja;
    // si Recepción tiene esa pantalla abierta en otra pestaña, que también se refresque.
    qc.invalidateQueries({ queryKey: ["masajes-pendientes"] });
  };
  const confirmar = useMutation({
    mutationFn: confirmarCita,
    onSuccess: (respuesta) => {
      invalidar();
      setNota(respuesta.mensaje);
    },
  });
  const cancelar = useMutation({ mutationFn: cancelarCita, onSuccess: invalidar });
  const eliminar = useMutation({ mutationFn: eliminarCita, onSuccess: invalidar });

  const tagEstado = (estado: number, tipoServicio: number) => {
    const clase = estado === 2 ? "tag-verde" : estado === 3 ? "tag-gris" : "tag-ambar";
    return (
      <span className={`tag ${clase}`}>
        {EstadoCitaTexto[estado as 1 | 2 | 3]}
        {estado === 2 && tipoServicio === 3 ? " · enviada a Caja" : ""}
      </span>
    );
  };

  const columnas: Columna<SolicitudCita>[] = [
    { encabezado: "Solicitante", render: (c) => c.nombreSolicitante },
    { encabezado: "Teléfono", render: (c) => c.telefono },
    { encabezado: "Servicio", render: (c) => TipoServicioCitaTexto[c.tipoServicio] },
    { encabezado: "Sede preferida", render: (c) => SedeTexto[c.sedePreferida] },
    { encabezado: "Fecha preferida", render: (c) => new Date(c.fechaPreferida).toLocaleDateString() },
    { encabezado: "Estado", render: (c) => tagEstado(c.estado, c.tipoServicio) },
    {
      encabezado: "Acciones",
      render: (c) => (
        <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
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
          {c.estado === 2 && SIGUIENTE_PASO[c.tipoServicio] && (
            <Link className="btn btn-secundario" to={SIGUIENTE_PASO[c.tipoServicio]!.ruta}>
              {SIGUIENTE_PASO[c.tipoServicio]!.etiqueta}
            </Link>
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
      {nota && (
        <div className="alerta alerta-exito" style={{ marginBottom: 14 }}>
          {nota}
          {nota.includes("Beneficiario") && (
            <>
              {" "}
              <Link to="/panel/beneficiarios">Ir a Beneficiarios →</Link>
            </>
          )}
        </div>
      )}
      <DataTable columnas={columnas} filas={data ?? []} claveFila={(c) => c.id} cargando={isLoading} />
    </>
  );
}
