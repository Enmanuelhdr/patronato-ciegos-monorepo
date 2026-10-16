import { useState, useMemo } from "react";

export interface Columna<T> {
  encabezado: string;
  render: (fila: T) => React.ReactNode;
  ancho?: string;
  // Valor textual opcional para búsqueda y exportación a Excel/CSV
  valorTexto?: (fila: T) => string;
}

export function DataTable<T>({
  columnas,
  filas,
  claveFila,
  cargando,
  vacio = "No hay registros.",
  habilitarBusqueda = true,
  habilitarExportacion = true,
  nombreArchivo = "reporte-patronato",
  tituloTabla,
}: {
  columnas: Columna<T>[];
  filas: T[];
  claveFila: (fila: T) => React.Key;
  cargando?: boolean;
  vacio?: string;
  habilitarBusqueda?: boolean;
  habilitarExportacion?: boolean;
  nombreArchivo?: string;
  tituloTabla?: string;
}) {
  const [busqueda, setBusqueda] = useState("");

  // Búsqueda global en tiempo real sobre las filas
  const filasFiltradas = useMemo(() => {
    if (!busqueda.trim()) return filas;
    const q = busqueda.toLowerCase().trim();
    return filas.filter((fila) => {
      // Si las columnas definen valorTexto, lo usamos; de lo contrario, convertimos valores del objeto a string
      return columnas.some((col) => {
        if (col.valorTexto) {
          return col.valorTexto(fila).toLowerCase().includes(q);
        }
        return false;
      }) || Object.values(fila as Record<string, unknown>).some((val) => {
        if (val === null || val === undefined) return false;
        if (typeof val === "object") {
          return Object.values(val as Record<string, unknown>).some(
            (subVal) => subVal !== null && subVal !== undefined && String(subVal).toLowerCase().includes(q)
          );
        }
        return String(val).toLowerCase().includes(q);
      });
    });
  }, [filas, busqueda, columnas]);

  // Exportar a Excel (CSV con BOM UTF-8)
  const exportarCSV = () => {
    if (!filasFiltradas.length) return;

    const encabezados = columnas.map((c) => `"${c.encabezado.replace(/"/g, '""')}"`).join(",");
    const lineas = filasFiltradas.map((fila) => {
      return columnas
        .map((col) => {
          let texto = "";
          if (col.valorTexto) {
            texto = col.valorTexto(fila);
          } else {
            // Extracción genérica
            const node = col.render(fila);
            if (typeof node === "string" || typeof node === "number") {
              texto = String(node);
            } else {
              texto = "";
            }
          }
          return `"${String(texto).replace(/"/g, '""')}"`;
        })
        .join(",");
    });

    const csvContent = "\uFEFF" + [encabezados, ...lineas].join("\r\n");
    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `${nombreArchivo}-${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  return (
    <div>
      {(habilitarBusqueda || habilitarExportacion) && (
        <div className="tabla-busqueda-toolbar">
          {habilitarBusqueda && (
            <div style={{ display: "flex", alignItems: "center", gap: 8, flex: 1 }}>
              <label htmlFor="tabla-input-busqueda" style={{ fontSize: 13, fontWeight: 600, color: "var(--slate-700)" }}>
                Buscar:
              </label>
              <input
                id="tabla-input-busqueda"
                type="search"
                className="input-busqueda-global"
                placeholder="Filtrar registros..."
                value={busqueda}
                onChange={(e) => setBusqueda(e.target.value)}
                aria-label="Buscar en esta tabla"
              />
              {busqueda && (
                <button
                  type="button"
                  className="btn btn-secundario"
                  style={{ padding: "4px 8px", fontSize: 12 }}
                  onClick={() => setBusqueda("")}
                  aria-label="Limpiar búsqueda"
                >
                  ✕
                </button>
              )}
              <span style={{ fontSize: 12, color: "var(--slate-500)" }} role="status" aria-live="polite">
                {filasFiltradas.length} de {filas.length} registros
              </span>
            </div>
          )}

          {habilitarExportacion && (
            <button
              type="button"
              className="btn btn-secundario"
              onClick={exportarCSV}
              disabled={cargando || filasFiltradas.length === 0}
              aria-label="Exportar datos visibles a archivo Excel CSV"
              style={{ display: "flex", alignItems: "center", gap: 6 }}
            >
              <span aria-hidden="true">📥</span>
              <span>Exportar Excel</span>
            </button>
          )}
        </div>
      )}

      <div className="tabla-contenedor" tabIndex={0} role="region" aria-label={tituloTabla ?? "Tabla de datos"}>
        <table className="tabla" aria-busy={cargando}>
          {tituloTabla && <caption className="sr-only" style={{ display: "none" }}>{tituloTabla}</caption>}
          <thead>
            <tr>
              {columnas.map((c) => (
                <th key={c.encabezado} scope="col" style={{ width: c.ancho }}>
                  {c.encabezado}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {cargando ? (
              <tr>
                <td colSpan={columnas.length} className="tabla-estado" role="status" aria-live="polite">
                  Cargando información...
                </td>
              </tr>
            ) : filasFiltradas.length === 0 ? (
              <tr>
                <td colSpan={columnas.length} className="tabla-estado" role="status" aria-live="polite">
                  {busqueda ? `No se encontraron coincidencias para "${busqueda}".` : vacio}
                </td>
              </tr>
            ) : (
              filasFiltradas.map((fila) => (
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
    </div>
  );
}
