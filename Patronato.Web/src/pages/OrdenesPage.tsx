import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "../components/PageHeader";
import { DataTable, type Columna } from "../components/DataTable";
import { crearOrden, getInventario, getOrdenesPendientes, type CrearOrdenItemPayload } from "../api/endpoints";
import { ApiError } from "../api/client";
import { useAuth } from "../context/AuthContext";
import type { OrdenPedidoCaja } from "../api/types";

export function OrdenesPage() {
  const qc = useQueryClient();
  const { sesion } = useAuth();
  const ordenes = useQuery({ queryKey: ["ordenes-pendientes"], queryFn: getOrdenesPendientes });
  const inventario = useQuery({ queryKey: ["inventario"], queryFn: getInventario });

  const [mostrarForm, setMostrarForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [clienteNombre, setClienteNombre] = useState("");
  const [clienteDocumento, setClienteDocumento] = useState("");
  const [notas, setNotas] = useState("");
  const [productoId, setProductoId] = useState(0);
  const [cantidad, setCantidad] = useState(1);
  const [items, setItems] = useState<CrearOrdenItemPayload[]>([]);

  const total = items.reduce((acc, it) => acc + it.cantidad * it.precioUnitario, 0);

  function agregarItem() {
    const producto = inventario.data?.find((p) => p.id === productoId);
    if (!producto) return;
    setItems((prev) => {
      const existente = prev.find((it) => it.productoId === producto.id);
      if (existente) {
        return prev.map((it) => (it.productoId === producto.id ? { ...it, cantidad: it.cantidad + cantidad } : it));
      }
      return [...prev, { productoId: producto.id, codigo: producto.codigo, nombre: producto.nombre, cantidad, precioUnitario: producto.precioVenta }];
    });
    setCantidad(1);
  }

  const crear = useMutation({
    mutationFn: () =>
      crearOrden({
        clienteNombre,
        clienteDocumento: clienteDocumento || undefined,
        creadoPor: `${sesion?.nombre ?? "Recepción"} (${sesion?.rol ?? "Recepción"})`,
        notas: notas || undefined,
        items,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["ordenes-pendientes"] });
      setMostrarForm(false);
      setItems([]);
      setClienteNombre("");
      setClienteDocumento("");
      setNotas("");
      setError(null);
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "No se pudo generar la orden."),
  });

  const columnas: Columna<OrdenPedidoCaja>[] = [
    { encabezado: "No. Orden", render: (o) => o.numeroOrden },
    { encabezado: "Cliente", render: (o) => o.clienteNombre },
    { encabezado: "Generada por", render: (o) => o.creadoPor },
    { encabezado: "Total", render: (o) => `RD$ ${o.total.toFixed(2)}` },
    { encabezado: "Estado", render: (o) => <span className={`tag ${o.estado === "Pendiente" ? "tag-ambar" : "tag-verde"}`}>{o.estado}</span> },
  ];

  return (
    <>
      <PageHeader
        titulo="Órdenes de Pedido"
        subtitulo="Genera una cotización de insumos para que el cliente la pague en la ventanilla de Caja."
        accion={<button className="btn btn-primario" onClick={() => setMostrarForm((v) => !v)}>{mostrarForm ? "Cancelar" : "+ Generar orden"}</button>}
      />

      {mostrarForm && (
        <div className="panel">
          <h2>Nueva orden de pedido</h2>
          {error && <div className="alerta alerta-error">{error}</div>}

          <div className="form-grid" style={{ marginBottom: 14 }}>
            <div className="campo">
              <label>Cliente</label>
              <input required value={clienteNombre} onChange={(e) => setClienteNombre(e.target.value)} />
            </div>
            <div className="campo">
              <label>Documento (opcional)</label>
              <input value={clienteDocumento} onChange={(e) => setClienteDocumento(e.target.value)} />
            </div>
            <div className="campo">
              <label>Notas (opcional)</label>
              <input value={notas} onChange={(e) => setNotas(e.target.value)} />
            </div>
          </div>

          <div className="panel" style={{ background: "#f8fafc" }}>
            <h2 style={{ fontSize: 13 }}>Artículos de la orden</h2>
            <div className="form-grid" style={{ marginBottom: 10 }}>
              <div className="campo">
                <label>Producto</label>
                <select value={productoId} onChange={(e) => setProductoId(Number(e.target.value))}>
                  <option value={0} disabled>Selecciona un producto...</option>
                  {inventario.data?.map((p) => (
                    <option key={p.id} value={p.id}>{p.nombre} — RD$ {p.precioVenta.toFixed(2)} (stock: {p.stockDisponible})</option>
                  ))}
                </select>
              </div>
              <div className="campo">
                <label>Cantidad</label>
                <input type="number" min={1} value={cantidad} onChange={(e) => setCantidad(Number(e.target.value))} />
              </div>
              <div className="campo" style={{ justifyContent: "flex-end" }}>
                <button type="button" className="btn btn-secundario" onClick={agregarItem} disabled={productoId === 0}>
                  + Agregar a la orden
                </button>
              </div>
            </div>

            <DataTable
              columnas={[
                { encabezado: "Artículo", render: (it: CrearOrdenItemPayload) => it.nombre },
                { encabezado: "Cantidad", render: (it: CrearOrdenItemPayload) => it.cantidad },
                { encabezado: "P. Unitario", render: (it: CrearOrdenItemPayload) => `RD$ ${it.precioUnitario.toFixed(2)}` },
                { encabezado: "Subtotal", render: (it: CrearOrdenItemPayload) => `RD$ ${(it.cantidad * it.precioUnitario).toFixed(2)}` },
                {
                  encabezado: "",
                  render: (it: CrearOrdenItemPayload) => (
                    <button className="btn btn-secundario" onClick={() => setItems((prev) => prev.filter((x) => x.productoId !== it.productoId))}>
                      Quitar
                    </button>
                  ),
                },
              ]}
              filas={items}
              claveFila={(it) => it.productoId}
              vacio="Agrega al menos un artículo."
            />

            <div style={{ textAlign: "right", marginTop: 10, fontWeight: 700 }}>Total: RD$ {total.toFixed(2)}</div>
          </div>

          <button
            className="btn btn-exito"
            style={{ marginTop: 14 }}
            onClick={() => crear.mutate()}
            disabled={crear.isPending || items.length === 0 || !clienteNombre}
          >
            {crear.isPending ? "Generando..." : "Generar orden"}
          </button>
        </div>
      )}

      <DataTable columnas={columnas} filas={ordenes.data ?? []} claveFila={(o) => o.id} cargando={ordenes.isLoading} />
    </>
  );
}
