import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { useAuth } from "../context/AuthContext";
import { ApiError } from "../api/client";
import {
  crearProducto,
  editarProducto,
  getInventarioPaginado,
  getMovimientosProducto,
  registrarAjusteInventario,
  registrarEntradaInventario,
  registrarSalidaInventario,
  type FiltrosInventario,
} from "../api/endpoints";
import type { ProductoArticulo } from "../api/types";

const TAMANO_PAGINA = 10;
type TipoMovimiento = "entrada" | "salida" | "ajuste";

export function InventarioPage() {
  const qc = useQueryClient();
  const { tienePermiso } = useAuth();
  const puedeEditar = tienePermiso("inventario.editar");
  const puedeCrear = tienePermiso("inventario.crear");
  const puedeMover = tienePermiso("inventario.movimientos");

  const [categoria, setCategoria] = useState("");
  const [estadoFiltro, setEstadoFiltro] = useState<"todos" | "activos" | "inactivos">("activos");
  const [soloStockBajo, setSoloStockBajo] = useState(false);
  const [pagina, setPagina] = useState(1);

  const filtros: FiltrosInventario = {
    categoria: categoria || undefined,
    activo: estadoFiltro === "todos" ? undefined : estadoFiltro === "activos",
    stockBajo: soloStockBajo || undefined,
    pagina,
    tamanoPagina: TAMANO_PAGINA,
  };

  const inventario = useQuery({ queryKey: ["inventario-paginado", filtros], queryFn: () => getInventarioPaginado(filtros) });

  const [mostrarFormProducto, setMostrarFormProducto] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [nuevoProducto, setNuevoProducto] = useState({ codigo: "", nombre: "", categoria: "", precioVenta: 0, stockInicial: 0, stockMinimo: 5 });

  const [productoMovimiento, setProductoMovimiento] = useState<ProductoArticulo | null>(null);
  const [tipoMovimiento, setTipoMovimiento] = useState<TipoMovimiento>("entrada");
  const [cantidadMovimiento, setCantidadMovimiento] = useState(1);
  const [motivoMovimiento, setMotivoMovimiento] = useState("");

  const [productoBitacora, setProductoBitacora] = useState<ProductoArticulo | null>(null);
  const movimientos = useQuery({
    queryKey: ["inventario-movimientos", productoBitacora?.id],
    queryFn: () => getMovimientosProducto(productoBitacora!.id),
    enabled: !!productoBitacora,
  });

  const invalidar = () => qc.invalidateQueries({ queryKey: ["inventario-paginado"] });

  const crear = useMutation({
    mutationFn: () => crearProducto(nuevoProducto),
    onSuccess: () => {
      invalidar();
      setMostrarFormProducto(false);
      setNuevoProducto({ codigo: "", nombre: "", categoria: "", precioVenta: 0, stockInicial: 0, stockMinimo: 5 });
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo crear el producto."),
  });

  const toggleActivo = useMutation({
    mutationFn: (p: ProductoArticulo) =>
      editarProducto(p.id, { nombre: p.nombre, categoria: p.categoria, precioVenta: p.precioVenta, stockMinimo: p.stockMinimo, activo: !p.activo }),
    onSuccess: invalidar,
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo actualizar el producto."),
  });

  const movimiento = useMutation({
    mutationFn: () => {
      if (!productoMovimiento) return Promise.reject(new Error("Selecciona un producto."));
      if (tipoMovimiento === "entrada") return registrarEntradaInventario(productoMovimiento.id, cantidadMovimiento, motivoMovimiento || undefined);
      if (tipoMovimiento === "salida") return registrarSalidaInventario(productoMovimiento.id, cantidadMovimiento, motivoMovimiento || undefined);
      return registrarAjusteInventario(productoMovimiento.id, cantidadMovimiento, motivoMovimiento || undefined);
    },
    onSuccess: () => {
      invalidar();
      setProductoMovimiento(null);
      setCantidadMovimiento(1);
      setMotivoMovimiento("");
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo registrar el movimiento."),
  });

  const columnas: Columna<ProductoArticulo>[] = [
    { encabezado: "Código", render: (p) => p.codigo },
    { encabezado: "Nombre", render: (p) => p.nombre },
    { encabezado: "Categoría", render: (p) => p.categoria },
    { encabezado: "Precio", render: (p) => `RD$ ${p.precioVenta.toFixed(2)}` },
    {
      encabezado: "Stock",
      render: (p) => (
        <span className={`tag ${p.stockBajo ? "tag-rojo" : "tag-verde"}`} title={`Stock mínimo: ${p.stockMinimo}`}>
          {p.stockDisponible} {p.stockBajo ? "⚠️ bajo" : ""}
        </span>
      ),
    },
    { encabezado: "Estado", render: (p) => <span className={`tag ${p.activo ? "tag-verde" : "tag-gris"}`}>{p.activo ? "Activo" : "Descontinuado"}</span> },
    {
      encabezado: "Acciones",
      render: (p) => (
        <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
          {puedeMover && (
            <button className="btn btn-secundario" onClick={() => { setProductoMovimiento(p); setTipoMovimiento("entrada"); setCantidadMovimiento(1); setMotivoMovimiento(""); }}>
              Movimiento
            </button>
          )}
          <button className="btn btn-secundario" onClick={() => setProductoBitacora(p)}>Bitácora</button>
          {puedeEditar && (
            <button className="btn btn-secundario" onClick={() => toggleActivo.mutate(p)} disabled={toggleActivo.isPending}>
              {p.activo ? "Descontinuar" : "Reactivar"}
            </button>
          )}
        </div>
      ),
    },
  ];

  const total = inventario.data?.total ?? 0;
  const totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA));

  return (
    <>
      <PageHeader
        titulo="Inventario"
        subtitulo="Catálogo, entradas/salidas/ajustes con bitácora de trazabilidad y alertas de stock bajo."
        accion={puedeCrear ? <button className="btn btn-primario" onClick={() => setMostrarFormProducto((v) => !v)}>{mostrarFormProducto ? "Cancelar" : "+ Nuevo producto"}</button> : undefined}
      />

      {error && <div className="alerta alerta-error">{error}</div>}

      <div className="panel" style={{ marginBottom: 16 }}>
        <div className="form-grid">
          <div className="campo">
            <label>Categoría</label>
            <input value={categoria} onChange={(e) => { setCategoria(e.target.value); setPagina(1); }} placeholder="Ej: Limpieza" />
          </div>
          <div className="campo">
            <label>Estado</label>
            <select value={estadoFiltro} onChange={(e) => { setEstadoFiltro(e.target.value as typeof estadoFiltro); setPagina(1); }}>
              <option value="todos">Todos</option>
              <option value="activos">Activos</option>
              <option value="inactivos">Descontinuados</option>
            </select>
          </div>
          <div className="campo" style={{ justifyContent: "flex-end", flexDirection: "row", alignItems: "center", gap: 8 }}>
            <input type="checkbox" checked={soloStockBajo} onChange={(e) => { setSoloStockBajo(e.target.checked); setPagina(1); }} id="stockBajo" />
            <label htmlFor="stockBajo" style={{ margin: 0 }}>Solo stock bajo</label>
          </div>
        </div>
      </div>

      {mostrarFormProducto && puedeCrear && (
        <div className="panel">
          <h2>Nuevo producto</h2>
          <form onSubmit={(e) => { e.preventDefault(); crear.mutate(); }}>
            <div className="form-grid">
              <div className="campo"><label>Código</label><input required value={nuevoProducto.codigo} onChange={(e) => setNuevoProducto({ ...nuevoProducto, codigo: e.target.value })} /></div>
              <div className="campo"><label>Nombre</label><input required value={nuevoProducto.nombre} onChange={(e) => setNuevoProducto({ ...nuevoProducto, nombre: e.target.value })} /></div>
              <div className="campo"><label>Categoría</label><input required value={nuevoProducto.categoria} onChange={(e) => setNuevoProducto({ ...nuevoProducto, categoria: e.target.value })} /></div>
              <div className="campo"><label>Precio de venta</label><input type="number" min={0} step="0.01" value={nuevoProducto.precioVenta} onChange={(e) => setNuevoProducto({ ...nuevoProducto, precioVenta: Number(e.target.value) })} /></div>
              <div className="campo"><label>Stock inicial</label><input type="number" min={0} value={nuevoProducto.stockInicial} onChange={(e) => setNuevoProducto({ ...nuevoProducto, stockInicial: Number(e.target.value) })} /></div>
              <div className="campo"><label>Stock mínimo (alerta)</label><input type="number" min={0} value={nuevoProducto.stockMinimo} onChange={(e) => setNuevoProducto({ ...nuevoProducto, stockMinimo: Number(e.target.value) })} /></div>
            </div>
            <button className="btn btn-exito" style={{ marginTop: 14 }} type="submit" disabled={crear.isPending}>
              {crear.isPending ? "Guardando..." : "Crear producto"}
            </button>
          </form>
        </div>
      )}

      {productoMovimiento && (
        <div className="panel">
          <h2>Movimiento de stock — {productoMovimiento.nombre}</h2>
          <form onSubmit={(e) => { e.preventDefault(); movimiento.mutate(); }}>
            <div className="form-grid">
              <div className="campo">
                <label>Tipo</label>
                <select value={tipoMovimiento} onChange={(e) => setTipoMovimiento(e.target.value as TipoMovimiento)}>
                  <option value="entrada">Entrada</option>
                  <option value="salida">Salida</option>
                  <option value="ajuste">Ajuste (corregir al conteo físico)</option>
                </select>
              </div>
              <div className="campo">
                <label>{tipoMovimiento === "ajuste" ? "Stock real (conteo físico)" : "Cantidad"}</label>
                <input type="number" min={0} value={cantidadMovimiento} onChange={(e) => setCantidadMovimiento(Number(e.target.value))} />
              </div>
              <div className="campo">
                <label>Motivo (opcional)</label>
                <input value={motivoMovimiento} onChange={(e) => setMotivoMovimiento(e.target.value)} placeholder="Ej: compra, merma, donación..." />
              </div>
            </div>
            <div style={{ display: "flex", gap: 10, marginTop: 14 }}>
              <button className="btn btn-exito" type="submit" disabled={movimiento.isPending}>{movimiento.isPending ? "Guardando..." : "Registrar"}</button>
              <button className="btn btn-secundario" type="button" onClick={() => setProductoMovimiento(null)}>Cancelar</button>
            </div>
          </form>
        </div>
      )}

      {productoBitacora && (
        <div className="panel">
          <h2>Bitácora de movimientos — {productoBitacora.nombre}</h2>
          <DataTable
            columnas={[
              { encabezado: "Fecha", render: (m) => new Date(m.fecha).toLocaleString() },
              { encabezado: "Tipo", render: (m) => m.tipo },
              { encabezado: "Cantidad", render: (m) => m.cantidad },
              { encabezado: "Stock resultante", render: (m) => m.stockResultante },
              { encabezado: "Motivo", render: (m) => m.motivo ?? "—" },
              { encabezado: "Realizado por", render: (m) => m.coreUserNombre },
            ]}
            filas={movimientos.data ?? []}
            claveFila={(m) => m.id}
            cargando={movimientos.isLoading}
            vacio="Sin movimientos registrados todavía."
          />
          <button className="btn btn-secundario" style={{ marginTop: 10 }} onClick={() => setProductoBitacora(null)}>Cerrar bitácora</button>
        </div>
      )}

      <DataTable columnas={columnas} filas={inventario.data?.items ?? []} claveFila={(p) => p.id} cargando={inventario.isLoading} />

      <div style={{ display: "flex", justifyContent: "center", gap: 10, alignItems: "center", marginTop: 14 }}>
        <button className="btn btn-secundario" disabled={pagina <= 1} onClick={() => setPagina((p) => p - 1)}>← Anterior</button>
        <span>Página {pagina} de {totalPaginas} ({total} productos)</span>
        <button className="btn btn-secundario" disabled={pagina >= totalPaginas} onClick={() => setPagina((p) => p + 1)}>Siguiente →</button>
      </div>
    </>
  );
}
