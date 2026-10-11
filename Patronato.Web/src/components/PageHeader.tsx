export function PageHeader({
  titulo,
  subtitulo,
  accion,
}: {
  titulo: string;
  subtitulo?: string;
  accion?: React.ReactNode;
}) {
  return (
    <div className="page-header">
      <div>
        <h1>{titulo}</h1>
        {subtitulo && <p>{subtitulo}</p>}
      </div>
      {accion}
    </div>
  );
}

export function StatCard({ etiqueta, valor, tono }: { etiqueta: string; valor: string; tono?: "azul" | "verde" | "ambar" | "morado" }) {
  return (
    <div className={`stat-card stat-${tono ?? "azul"}`}>
      <span className="stat-etiqueta">{etiqueta}</span>
      <span className="stat-valor">{valor}</span>
    </div>
  );
}
