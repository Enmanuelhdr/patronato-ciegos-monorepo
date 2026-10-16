using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Enums;
using Patronato.Core.Servicios;

namespace Patronato.Core.Controllers
{
    public record ProductoDto(int Id, string Codigo, string Nombre, string Categoria, decimal PrecioVenta, int StockDisponible, int StockMinimo, bool Activo, bool StockBajo);
    public record CrearProductoRequest(string Codigo, string Nombre, string Categoria, decimal PrecioVenta, int StockInicial, int StockMinimo);
    public record EditarProductoRequest(string Nombre, string Categoria, decimal PrecioVenta, int StockMinimo, bool Activo);
    public record MovimientoRequest(int Cantidad, string? Motivo);
    public record AjusteRequest(int StockReal, string? Motivo);
    public record MovimientoDto(int Id, int ProductoId, string Tipo, int Cantidad, int StockResultante, string? Motivo, string CoreUserNombre, DateTime Fecha);
    public record PaginaResultado<T>(int Total, int Pagina, int TamanoPagina, List<T> Items);

    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static ProductoDto AProductoDto(ProductoArticulo p) =>
            new(p.Id, p.Codigo, p.Nombre, p.Categoria, p.PrecioVenta, p.StockDisponible, p.StockMinimo, p.Activo, p.StockDisponible <= p.StockMinimo);

        private int CoreUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // 1. GET: api/inventario (listado con filtros rápidos + paginación)
        [HttpGet]
        [RequierePermiso(PermisosCore.InventarioVer)]
        public async Task<ActionResult<PaginaResultado<ProductoDto>>> GetProductos(
            [FromQuery] string? categoria, [FromQuery] bool? activo, [FromQuery] bool? stockBajo,
            [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 1, 500);

            var query = _context.ProductosArticulos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(categoria)) query = query.Where(p => p.Categoria == categoria);
            if (activo.HasValue) query = query.Where(p => p.Activo == activo.Value);
            if (stockBajo == true) query = query.Where(p => p.StockDisponible <= p.StockMinimo);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Categoria).ThenBy(p => p.Nombre)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            return Ok(new PaginaResultado<ProductoDto>(total, pagina, tamanoPagina, items.Select(AProductoDto).ToList()));
        }

        // 2. GET: api/inventario/alertas (productos en o por debajo de su stock mínimo)
        [HttpGet("alertas")]
        [RequierePermiso(PermisosCore.InventarioVer)]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetAlertas()
        {
            var productos = await _context.ProductosArticulos
                .Where(p => p.Activo && p.StockDisponible <= p.StockMinimo)
                .OrderBy(p => p.StockDisponible)
                .ToListAsync();

            return Ok(productos.Select(AProductoDto));
        }

        // 3. GET: api/inventario/5 (Consultar un producto específico)
        [HttpGet("{id}")]
        [RequierePermiso(PermisosCore.InventarioVer)]
        public async Task<ActionResult<ProductoDto>> GetProducto(int id)
        {
            var producto = await _context.ProductosArticulos.FindAsync(id);
            if (producto == null)
                return NotFound("Producto no encontrado.");

            return AProductoDto(producto);
        }

        // 4. GET: api/inventario/5/movimientos (Bitácora de entradas/salidas/ajustes)
        [HttpGet("{id}/movimientos")]
        [RequierePermiso(PermisosCore.InventarioVer)]
        public async Task<ActionResult<IEnumerable<MovimientoDto>>> GetMovimientos(int id)
        {
            var movimientos = await _context.InventarioMovimientos
                .Where(m => m.ProductoId == id)
                .Include(m => m.CoreUser)
                .OrderByDescending(m => m.Fecha)
                .Select(m => new MovimientoDto(m.Id, m.ProductoId, m.Tipo.ToString(), m.Cantidad, m.StockResultante, m.Motivo, m.CoreUser!.NombreCompleto, m.Fecha))
                .ToListAsync();

            return Ok(movimientos);
        }

        // 5. POST: api/inventario (Dar de alta un nuevo producto en catálogo)
        [HttpPost]
        [RequierePermiso(PermisosCore.InventarioCrear)]
        public async Task<ActionResult<ProductoDto>> PostProducto(CrearProductoRequest request)
        {
            if (await _context.ProductosArticulos.AnyAsync(p => p.Codigo == request.Codigo))
                return BadRequest("Ya existe un producto con ese código.");

            var producto = new ProductoArticulo
            {
                Codigo = request.Codigo,
                Nombre = request.Nombre,
                Categoria = request.Categoria,
                PrecioVenta = request.PrecioVenta,
                StockDisponible = request.StockInicial,
                StockMinimo = request.StockMinimo,
                Activo = true,
            };
            _context.ProductosArticulos.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, AProductoDto(producto));
        }

        // 6. PUT: api/inventario/5 (Editar catálogo: nombre, categoría, precio, stock mínimo, estado)
        [HttpPut("{id}")]
        [RequierePermiso(PermisosCore.InventarioEditar)]
        public async Task<IActionResult> PutProducto(int id, EditarProductoRequest request)
        {
            var producto = await _context.ProductosArticulos.FindAsync(id);
            if (producto == null)
                return NotFound("Producto no encontrado.");

            producto.Nombre = request.Nombre;
            producto.Categoria = request.Categoria;
            producto.PrecioVenta = request.PrecioVenta;
            producto.StockMinimo = request.StockMinimo;
            producto.Activo = request.Activo;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Catálogo actualizado para {producto.Nombre}." });
        }

        // 7. PUT: api/inventario/5/entrada (Entrada de producción o compra)
        [HttpPut("{id}/entrada")]
        [RequierePermiso(PermisosCore.InventarioMovimientos)]
        public async Task<IActionResult> Entrada(int id, MovimientoRequest request) =>
            await RegistrarMovimiento(id, TipoMovimientoInventario.Entrada, request.Cantidad, request.Motivo);

        // 8. PUT: api/inventario/5/salida (Salida manual: merma, donación, uso interno, etc.)
        [HttpPut("{id}/salida")]
        [RequierePermiso(PermisosCore.InventarioMovimientos)]
        public async Task<IActionResult> Salida(int id, MovimientoRequest request) =>
            await RegistrarMovimiento(id, TipoMovimientoInventario.Salida, -request.Cantidad, request.Motivo);

        // 9. PUT: api/inventario/5/ajuste (Corrige el stock al conteo físico real)
        [HttpPut("{id}/ajuste")]
        [RequierePermiso(PermisosCore.InventarioMovimientos)]
        public async Task<IActionResult> Ajuste(int id, AjusteRequest request)
        {
            var producto = await _context.ProductosArticulos.FindAsync(id);
            if (producto == null)
                return NotFound("Producto no encontrado.");

            if (request.StockReal < 0)
                return BadRequest("El stock real no puede ser negativo.");

            var delta = request.StockReal - producto.StockDisponible;
            producto.StockDisponible = request.StockReal;

            _context.InventarioMovimientos.Add(new InventarioMovimiento
            {
                ProductoId = id,
                Tipo = TipoMovimientoInventario.Ajuste,
                Cantidad = delta,
                StockResultante = producto.StockDisponible,
                Motivo = request.Motivo,
                CoreUserId = CoreUserId,
            });

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Stock ajustado a {producto.StockDisponible} unidades para {producto.Nombre}.", nuevoStock = producto.StockDisponible });
        }

        private async Task<IActionResult> RegistrarMovimiento(int id, TipoMovimientoInventario tipo, int cantidadConSigno, string? motivo)
        {
            var producto = await _context.ProductosArticulos.FindAsync(id);
            if (producto == null)
                return NotFound("Producto no encontrado.");

            try
            {
                if (cantidadConSigno >= 0)
                    producto.AgregarStock(cantidadConSigno);
                else
                    producto.DescontarStock(-cantidadConSigno);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            _context.InventarioMovimientos.Add(new InventarioMovimiento
            {
                ProductoId = id,
                Tipo = tipo,
                Cantidad = cantidadConSigno,
                StockResultante = producto.StockDisponible,
                Motivo = motivo,
                CoreUserId = CoreUserId,
            });

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Stock actualizado exitosamente para {producto.Nombre}.", nuevoStock = producto.StockDisponible });
        }
    }
}
