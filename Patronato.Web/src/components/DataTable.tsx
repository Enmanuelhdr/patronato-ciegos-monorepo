export interface Columna<T> {
  encabezado: string;
  render: (fila: T) => React.ReactNode;
  ancho?: string;
}

export function DataTable<T>({
  columnas,
  filas,
  claveFila,
  cargando,
  vacio = "No hay registros.",
}: {
  columnas: Columna<T>[];
  filas: T[];
  claveFila: (fila: T) => React.Key;
  cargando?: boolean;
  vacio?: string;
}) {
  return (
    <div className="tabla-contenedor">
      <table className="tabla">
        <thead>
          <tr>
            {columnas.map((c) => (
              <th key={c.encabezado} style={{ width: c.ancho }}>
                {c.encabezado}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {cargando ? (
            <tr>
              <td colSpan={columnas.length} className="tabla-estado">
                Cargando...
              </td>
            </tr>
          ) : filas.length === 0 ? (
            <tr>
              <td colSpan={columnas.length} className="tabla-estado">
                {vacio}
              </td>
            </tr>
          ) : (
            filas.map((fila) => (
              <tr key={claveFila(fila)}>
                {columnas.map((c) => (
                  <td key={c.encabezado}>{c.render(fila)}</td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
