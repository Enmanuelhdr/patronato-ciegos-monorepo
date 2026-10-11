using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Patronato.Caja.Models;
using Patronato.Caja.Services;
using Patronato.Caja.Views;

namespace Patronato.Caja
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService = new();
        private readonly DispatcherTimer _clockTimer = new();

        // Colecciones en memoria
        private List<ProductoItem> _catalogoProductos = new();
        private readonly ObservableCollection<CarritoItem> _carrito = new();
        private List<ClienteFrecuenteDto> _clientesFrecuentes = new();
        private List<OrdenPedidoCajaDto> _ordenesPendientes = new();
        private int? _ordenSeleccionadaId = null;
        private List<SesionMasajeItem> _masajesPendientes = new();
        private List<PrestamoItem> _prestamosActivos = new();
        private List<TransaccionCajaDto> _recibosHistorial = new();

        public MainWindow()
        {
            InitializeComponent();
            GridCarrito.ItemsSource = _carrito;

            Loaded += MainWindow_Loaded;
            ConfigurarReloj();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CargarInfoSesion();
            await CargarDatosInicialesAsync();
        }

        private void ConfigurarReloj()
        {
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) =>
            {
                TxtInfoHora.Text = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
            };
            _clockTimer.Start();
        }

        private void CargarInfoSesion()
        {
            var sesion = SessionManager.CurrentSession;
            string rol = sesion?.Rol ?? "Cajero";
            TxtInfoCajero.Text = sesion?.Nombre ?? "Cajero General";
            TxtInfoSucursal.Text = sesion?.SucursalNombre ?? "Santo Domingo";
            TxtInfoRol.Text = rol;

            bool esAdmin = rol == "Administrador";
            bool esRecepcion = rol == "Recepcionista";

            // Color del Badge de Rol
            if (esRecepcion)
            {
                BadgeRol.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(124, 58, 237)); // Púrpura
            }
            else if (esAdmin)
            {
                BadgeRol.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)); // Slate Oscuro
            }
            else
            {
                BadgeRol.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 132, 199)); // Azul Cyan
            }

            // Control de pestañas por Rol
            TabGestionCajeros.Visibility = esAdmin ? Visibility.Visible : Visibility.Collapsed;
            TabArqueoCaja.Visibility = esRecepcion ? Visibility.Collapsed : Visibility.Visible;
            TabDonaciones.Visibility = esRecepcion ? Visibility.Collapsed : Visibility.Visible;

            // En Préstamos: Banner informativo para Recepción (Caja solo cobra cuotas de préstamos aprobados vía Web)
            PanelInfoPrestamosRecepcion.Visibility = esRecepcion ? Visibility.Visible : Visibility.Collapsed;

            // Adaptación de Interfaz POS según el Rol (Recepción crea órdenes, Cajero y Admin cobran)
            if (esRecepcion)
            {
                PanelCobroEfectivo.Visibility = Visibility.Collapsed;
                PanelInfoRecepcion.Visibility = Visibility.Visible;
                BtnCobrarVenta.Visibility = Visibility.Collapsed;
                BtnGenerarOrdenVenta.Visibility = Visibility.Visible;
                PanelOrdenesPendientes.Visibility = Visibility.Collapsed;
                LblTotalTitulo.Text = "TOTAL DE LA ORDEN:";
                TxtTituloCarrito.Text = "Detalle de Orden / Cotización";

                // En Masajes: Recepción agenda citas pero no cobra dinero en ventanilla
                BtnCobrarMasaje.IsEnabled = false;
                BtnCobrarMasaje.ToolTip = "Solo el Cajero puede cobrar el servicio en ventanilla.";

                // En Préstamos: Recepción consulta saldos pero no cobra abonos
                BtnAplicarAbono.IsEnabled = false;
                BtnAplicarAbono.ToolTip = "Solo el Cajero puede cobrar abonos en ventanilla.";

                // En Donaciones: Recepción no maneja cobro directo
                BtnRegistrarDonacion.IsEnabled = false;
                BtnRegistrarDonacion.ToolTip = "El cobro y recibo oficial de donaciones se emite en Caja.";
            }
            else
            {
                PanelCobroEfectivo.Visibility = Visibility.Visible;
                PanelInfoRecepcion.Visibility = Visibility.Collapsed;
                BtnCobrarVenta.Visibility = Visibility.Visible;
                BtnGenerarOrdenVenta.Visibility = Visibility.Collapsed;
                PanelOrdenesPendientes.Visibility = Visibility.Visible;
                LblTotalTitulo.Text = "TOTAL A COBRAR:";
                TxtTituloCarrito.Text = "Detalle del Carrito / Cobro";

                BtnCobrarMasaje.IsEnabled = true;
                BtnCobrarMasaje.ToolTip = null;

                BtnAplicarAbono.IsEnabled = true;
                BtnAplicarAbono.ToolTip = null;

                BtnRegistrarDonacion.IsEnabled = true;
                BtnRegistrarDonacion.ToolTip = null;
            }
        }

        private async Task CargarDatosInicialesAsync()
        {
            SetEstado("Cargando catálogo e información de caja...");
            await Task.WhenAll(
                CargarProductosAsync(),
                CargarClientesFrecuentesAsync(),
                CargarOrdenesPendientesAsync(),
                CargarMasajesAsync(),
                CargarPrestamosAsync(),
                CargarArqueoAsync()
            );
            SetEstado("Sistema listo para operar.");
        }

        private async Task CargarOrdenesPendientesAsync()
        {
            try
            {
                _ordenesPendientes = await _apiService.GetOrdenesPendientesAsync();
                CmbOrdenesPendientes.ItemsSource = null;
                CmbOrdenesPendientes.ItemsSource = _ordenesPendientes;
                if (_ordenesPendientes.Count > 0)
                {
                    CmbOrdenesPendientes.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar órdenes pendientes: {ex.Message}");
            }
        }

        private async Task CargarClientesFrecuentesAsync()
        {
            try
            {
                _clientesFrecuentes = await _apiService.GetClientesFrecuentesAsync();
                var nombres = _clientesFrecuentes.Select(c => c.Nombre).Distinct().ToList();

                // Poblar Combo de Ventas POS
                CmbClientesFrecuentes.ItemsSource = nombres;
                if (string.IsNullOrWhiteSpace(CmbClientesFrecuentes.Text))
                {
                    CmbClientesFrecuentes.Text = "Consumidor Final";
                }

                // Poblar Combo de Donantes
                CmbDonantesFrecuentes.ItemsSource = nombres;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar clientes frecuentes: {ex.Message}");
            }
        }

        private void SetEstado(string mensaje)
        {
            TxtEstadoGlobal.Text = mensaje;
        }

        private async void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl) return;

            var selectedTab = MainTabControl.SelectedItem as TabItem;
            if (selectedTab == null) return;

            string header = selectedTab.Header.ToString() ?? "";
            if (header.Contains("Arqueo"))
            {
                await CargarArqueoAsync();
            }
            else if (header.Contains("Recibos"))
            {
                await CargarHistorialRecibosAsync();
            }
            else if (header.Contains("Masajes"))
            {
                await CargarMasajesAsync();
            }
            else if (header.Contains("Préstamos"))
            {
                await CargarPrestamosAsync();
            }
            else if (header.Contains("Gestión") || header.Contains("Admin"))
            {
                await CargarUsuariosAdminAsync();
            }
        }

        // =========================================================
        // 1. MÓDULO DE VENTA DE INSUMOS (POS)
        // =========================================================
        private async Task CargarProductosAsync()
        {
            _catalogoProductos = await _apiService.GetProductosInventarioAsync();
            FiltrarProductos();
        }

        private void FiltrarProductos()
        {
            string filtro = TxtBuscarProducto.Text?.Trim().ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(filtro))
            {
                GridProductos.ItemsSource = _catalogoProductos;
            }
            else
            {
                GridProductos.ItemsSource = _catalogoProductos.Where(p =>
                    p.Nombre.ToLower().Contains(filtro) ||
                    p.Codigo.ToLower().Contains(filtro) ||
                    p.Categoria.ToLower().Contains(filtro)).ToList();
            }
        }

        private void TxtBuscarProducto_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarProductos();
        }

        private async void BtnActualizarProductos_Click(object sender, RoutedEventArgs e)
        {
            await CargarProductosAsync();
            SetEstado("Catálogo de productos actualizado.");
        }

        private void BtnAgregarAlCarrito_Click(object sender, RoutedEventArgs e)
        {
            if (GridProductos.SelectedItem is not ProductoItem producto)
            {
                MessageBox.Show("Por favor seleccione un producto del catálogo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(TxtCantidadAgregar.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("La cantidad a agregar debe ser un número entero mayor a cero.", "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existente = _carrito.FirstOrDefault(c => c.ProductoId == producto.Id);
            int cantidadTotal = (existente?.Cantidad ?? 0) + cantidad;

            if (cantidadTotal > producto.StockDisponible)
            {
                MessageBox.Show($"Stock insuficiente. Solo quedan {producto.StockDisponible} unidades disponibles de '{producto.Nombre}'.", "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (existente != null)
            {
                existente.Cantidad = cantidadTotal;
            }
            else
            {
                _carrito.Add(new CarritoItem
                {
                    ProductoId = producto.Id,
                    Codigo = producto.Codigo,
                    Nombre = producto.Nombre,
                    PrecioUnitario = producto.PrecioVenta,
                    MaxStock = producto.StockDisponible,
                    Cantidad = cantidad
                });
            }

            TxtCantidadAgregar.Text = "1";
            ActualizarTotalCarrito();
        }

        private void BtnQuitarItemCarrito_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CarritoItem item)
            {
                _carrito.Remove(item);
                ActualizarTotalCarrito();
            }
        }

        private void BtnVaciarCarrito_Click(object sender, RoutedEventArgs e)
        {
            if (_carrito.Any())
            {
                _carrito.Clear();
                ActualizarTotalCarrito();
            }
        }

        private void ActualizarTotalCarrito()
        {
            decimal total = _carrito.Sum(c => c.Subtotal);
            TxtTotalCarrito.Text = $"RD$ {total:N2}";

            string metodo = ((ComboBoxItem)CmbMetodoPagoVenta.SelectedItem)?.Content?.ToString() ?? "Efectivo";
            if (metodo != "Efectivo")
            {
                TxtMontoRecibidoVenta.Text = total.ToString("F2");
            }

            CalcularDevueltaVenta();
        }

        private void CmbMetodoPagoVenta_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtMontoRecibidoVenta == null || PanelMontoRecibidoVenta == null || PanelDevueltaVenta == null) return;
            string metodo = ((ComboBoxItem)CmbMetodoPagoVenta.SelectedItem)?.Content?.ToString() ?? "Efectivo";
            decimal total = _carrito.Sum(c => c.Subtotal);

            bool esEfectivo = metodo == "Efectivo";
            if (esEfectivo)
            {
                PanelMontoRecibidoVenta.Visibility = Visibility.Visible;
                PanelDevueltaVenta.Visibility = Visibility.Visible;
                TxtMontoRecibidoVenta.IsEnabled = true;
                TxtMontoRecibidoVenta.Text = total > 0 ? total.ToString("F2") : "0.00";
            }
            else
            {
                // En pagos electrónicos con tarjeta o transferencia no aplica tender de efectivo ni cambio
                PanelMontoRecibidoVenta.Visibility = Visibility.Collapsed;
                PanelDevueltaVenta.Visibility = Visibility.Collapsed;
                TxtMontoRecibidoVenta.IsEnabled = false;
                TxtMontoRecibidoVenta.Text = total.ToString("F2");
                TxtDevueltaVenta.Text = "RD$ 0.00";
            }
            CalcularDevueltaVenta();
        }

        private void TxtMontoRecibidoVenta_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularDevueltaVenta();
        }

        private void CalcularDevueltaVenta()
        {
            if (TxtDevueltaVenta == null) return;
            string metodo = ((ComboBoxItem)CmbMetodoPagoVenta.SelectedItem)?.Content?.ToString() ?? "Efectivo";
            if (metodo != "Efectivo")
            {
                TxtDevueltaVenta.Text = "RD$ 0.00";
                return;
            }

            decimal total = _carrito.Sum(c => c.Subtotal);
            string rawTexto = TxtMontoRecibidoVenta?.Text?.Trim().Replace(",", "") ?? "";
            decimal.TryParse(rawTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal recibido);
            decimal devuelta = Math.Max(0, recibido - total);
            TxtDevueltaVenta.Text = $"RD$ {devuelta:N2}";
        }

        private void BtnClienteExpress_Click(object sender, RoutedEventArgs e)
        {
            CmbClientesFrecuentes.Text = "Consumidor Final";
            _ordenSeleccionadaId = null;
        }

        private void BtnCargarOrden_Click(object sender, RoutedEventArgs e)
        {
            if (CmbOrdenesPendientes.SelectedItem is not OrdenPedidoCajaDto orden)
            {
                MessageBox.Show("Por favor seleccione una orden pendiente de la lista.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<OrdenItemDto>>(orden.ItemsJson);
                if (items == null || !items.Any())
                {
                    MessageBox.Show("La orden seleccionada no contiene artículos legibles.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _carrito.Clear();
                foreach (var it in items)
                {
                    _carrito.Add(new CarritoItem
                    {
                        ProductoId = it.ProductoId,
                        Codigo = it.Codigo,
                        Nombre = it.Nombre,
                        PrecioUnitario = it.PrecioUnitario,
                        Cantidad = it.Cantidad,
                        MaxStock = 999
                    });
                }

                _ordenSeleccionadaId = orden.Id;
                CmbClientesFrecuentes.Text = orden.ClienteNombre;
                ActualizarTotalCarrito();

                SetEstado($"Orden {orden.NumeroOrden} de '{orden.ClienteNombre}' cargada al carrito.");
                MessageBox.Show($"¡Orden '{orden.NumeroOrden}' cargada correctamente!\n\nCliente: {orden.ClienteNombre}\nCreada por: {orden.CreadoPor}\nTotal a Cobrar: RD$ {orden.Total:N2}\n\nPuede proceder con el método de pago y emitir el recibo.", "Orden Cargada de Recepción", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los artículos de la orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGenerarOrdenVenta_Click(object sender, RoutedEventArgs e)
        {
            if (!_carrito.Any())
            {
                MessageBox.Show("El carrito de compras está vacío. Agregue productos antes de generar la orden.", "Carrito Vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string cliente = string.IsNullOrWhiteSpace(CmbClientesFrecuentes.Text) ? "Consumidor Final" : CmbClientesFrecuentes.Text.Trim();
            decimal total = _carrito.Sum(c => c.Subtotal);

            BtnGenerarOrdenVenta.IsEnabled = false;
            SetEstado("Generando orden de pedido para Caja...");

            try
            {
                var dto = new CrearOrdenDto
                {
                    ClienteNombre = cliente,
                    CreadoPor = $"{SessionManager.CurrentSession?.Nombre ?? "Laura Sánchez"} (Recepción)",
                    Notas = $"Orden de insumos generada en recepción ({_carrito.Count} artículos)",
                    Items = _carrito.Select(c => new OrdenItemDto
                    {
                        ProductoId = c.ProductoId,
                        Codigo = c.Codigo,
                        Nombre = c.Nombre,
                        Cantidad = c.Cantidad,
                        PrecioUnitario = c.PrecioUnitario
                    }).ToList()
                };

                var (success, message, orden) = await _apiService.CrearOrdenAsync(dto);
                if (!success || orden == null)
                {
                    MessageBox.Show(message, "Error al Generar Orden", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show($"¡Orden generada con éxito!\n\nNo. Orden: {orden.NumeroOrden}\nCliente: {cliente}\nTotal a Pagar en Caja: RD$ {total:N2}\n\nIndique al cliente que pase a la ventanilla de Caja con este número de orden para pagar.", "Orden Registrada en Recepción", MessageBoxButton.OK, MessageBoxImage.Information);

                _carrito.Clear();
                ActualizarTotalCarrito();
                CmbClientesFrecuentes.Text = "Consumidor Final";

                await CargarOrdenesPendientesAsync();
                await CargarClientesFrecuentesAsync();
                SetEstado($"Orden {orden.NumeroOrden} lista para ser cobrada en Caja.");
            }
            finally
            {
                BtnGenerarOrdenVenta.IsEnabled = true;
            }
        }

        private async void BtnCobrarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (!_carrito.Any())
            {
                MessageBox.Show("El carrito de compras está vacío. Agregue productos antes de cobrar.", "Carrito Vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal total = _carrito.Sum(c => c.Subtotal);
            string metodo = ((ComboBoxItem)CmbMetodoPagoVenta.SelectedItem)?.Content?.ToString() ?? "Efectivo";
            decimal recibido = total;

            if (metodo == "Efectivo")
            {
                string rawTexto = TxtMontoRecibidoVenta.Text?.Trim().Replace(",", "") ?? "";
                if (!decimal.TryParse(rawTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out recibido) || recibido < total)
                {
                    MessageBox.Show($"El monto recibido en efectivo (RD$ {recibido:N2}) es menor al total de la venta (RD$ {total:N2}).", "Monto Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                // En tarjeta o transferencia el monto recibido siempre es exactamente el total a cobrar
                recibido = total;
            }

            BtnCobrarVenta.IsEnabled = false;
            SetEstado("Procesando cobro en Caja...");

            try
            {
                string cliente = string.IsNullOrWhiteSpace(CmbClientesFrecuentes.Text) ? "Consumidor Final" : CmbClientesFrecuentes.Text.Trim();
                var (success, message, recibo) = await _apiService.VenderCarritoAsync(_carrito.ToList(), metodo, recibido, cliente, _ordenSeleccionadaId);

                if (!success)
                {
                    MessageBox.Show(message, "Error al Procesar Venta", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _carrito.Clear();
                ActualizarTotalCarrito();
                CmbClientesFrecuentes.Text = "Consumidor Final";
                TxtMontoRecibidoVenta.Text = "0.00";
                _ordenSeleccionadaId = null;

                await CargarProductosAsync();
                await CargarArqueoAsync();
                await CargarOrdenesPendientesAsync();
                await CargarClientesFrecuentesAsync();

                if (recibo != null)
                {
                    var preview = new ReceiptPreviewWindow(recibo) { Owner = this };
                    preview.ShowDialog();
                }
                else
                {
                    MessageBox.Show(message, "Venta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                SetEstado("Venta completada exitosamente.");
            }
            finally
            {
                BtnCobrarVenta.IsEnabled = true;
            }
        }

        // =========================================================
        // 2. MÓDULO DE MASAJES TACTO
        // =========================================================
        private async Task CargarMasajesAsync()
        {
            _masajesPendientes = await _apiService.GetMasajesPendientesAsync();
            GridMasajes.ItemsSource = null;
            GridMasajes.ItemsSource = _masajesPendientes;
            DesactivarCobroMasaje();
        }

        private async void BtnActualizarMasajes_Click(object sender, RoutedEventArgs e)
        {
            await CargarMasajesAsync();
            SetEstado("Lista de masajes TACTO actualizada.");
        }

        private async void BtnAbrirAgendarMasaje_Click(object sender, RoutedEventArgs e)
        {
            var modal = new AgendarMasajeWindow { Owner = this };
            if (modal.ShowDialog() == true)
            {
                await CargarMasajesAsync();
                await CargarClientesFrecuentesAsync();
                SetEstado("Nueva cita de Masaje TACTO agendada exitosamente.");
            }
        }

        private void GridMasajes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridMasajes.SelectedItem is SesionMasajeItem sesion)
            {
                TxtDetalleMasajeSeleccionado.Text = $"Cita #{sesion.Id}: {sesion.NombreCliente} | Masajista: {sesion.NombreMasajista} ({sesion.DuracionMinutos} min)";
                TxtTarifaMasaje.Text = $"RD$ {sesion.PrecioTarifa:N2}";
                TxtMontoRecibidoMasaje.Text = sesion.PrecioTarifa.ToString("F2");
                bool esRecepcion = SessionManager.CurrentSession?.Rol == "Recepcionista";
                BtnCobrarMasaje.IsEnabled = !esRecepcion;
            }
            else
            {
                DesactivarCobroMasaje();
            }
        }

        private void DesactivarCobroMasaje()
        {
            TxtDetalleMasajeSeleccionado.Text = "Selecciona una cita de la lista superior para cobrar";
            TxtTarifaMasaje.Text = "RD$ 0.00";
            TxtMontoRecibidoMasaje.Text = "0.00";
            BtnCobrarMasaje.IsEnabled = false;
        }

        private void CmbMetodoPagoMasaje_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelMontoRecibidoMasaje == null || TxtMontoRecibidoMasaje == null) return;
            string metodo = ((ComboBoxItem)CmbMetodoPagoMasaje.SelectedItem)?.Content?.ToString() ?? "Efectivo";
            bool esEfectivo = metodo == "Efectivo";
            PanelMontoRecibidoMasaje.Visibility = esEfectivo ? Visibility.Visible : Visibility.Collapsed;
            TxtMontoRecibidoMasaje.IsEnabled = esEfectivo;
        }

        private void TxtMontoRecibidoMasaje_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Opcional cálculo si se requiere mostrar devuelta
        }

        private async void BtnCobrarMasaje_Click(object sender, RoutedEventArgs e)
        {
            if (GridMasajes.SelectedItem is not SesionMasajeItem sesion)
                return;

            string metodo = ((ComboBoxItem)CmbMetodoPagoMasaje.SelectedItem)?.Content?.ToString() ?? "Efectivo";
            decimal recibido = sesion.PrecioTarifa;

            if (metodo == "Efectivo")
            {
                string rawTexto = TxtMontoRecibidoMasaje.Text?.Trim().Replace(",", "") ?? "";
                if (!decimal.TryParse(rawTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out recibido) || recibido < sesion.PrecioTarifa)
                {
                    MessageBox.Show($"El monto recibido es menor a la tarifa del masaje (RD$ {sesion.PrecioTarifa:N2}).", "Monto Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                recibido = sesion.PrecioTarifa;
            }

            BtnCobrarMasaje.IsEnabled = false;
            SetEstado($"Cobrando sesión #{sesion.Id} de masajes TACTO...");

            try
            {
                var (success, message, recibo) = await _apiService.CobrarMasajeAsync(sesion.Id, metodo, recibido);
                if (!success)
                {
                    MessageBox.Show(message, "Error al Cobrar Masaje", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await CargarMasajesAsync();
                await CargarArqueoAsync();

                if (recibo != null)
                {
                    var preview = new ReceiptPreviewWindow(recibo) { Owner = this };
                    preview.ShowDialog();
                }
                else
                {
                    MessageBox.Show(message, "Masaje Cobrado", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                SetEstado("Sesión de masaje cobrada exitosamente.");
            }
            finally
            {
                BtnCobrarMasaje.IsEnabled = true;
            }
        }

        // =========================================================
        // 3. MÓDULO DE ABONOS A PRÉSTAMOS
        // =========================================================
        private async Task CargarPrestamosAsync()
        {
            string filtro = TxtBuscarPrestamo.Text.Trim();
            _prestamosActivos = await _apiService.GetPrestamosActivosAsync(filtro);
            GridPrestamos.ItemsSource = null;
            GridPrestamos.ItemsSource = _prestamosActivos;
            DesactivarCobroPrestamo();
        }

        private async void BtnBuscarPrestamo_Click(object sender, RoutedEventArgs e)
        {
            await CargarPrestamosAsync();
        }

        private async void TxtBuscarPrestamo_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Búsqueda en tiempo real
            await CargarPrestamosAsync();
        }

        private void GridPrestamos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridPrestamos.SelectedItem is PrestamoItem prestamo)
            {
                string benef = prestamo.Beneficiario?.NombreCompleto ?? "Beneficiario #" + prestamo.BeneficiarioId;
                TxtDetallePrestamo.Text = $"Préstamo #{prestamo.Id} - {benef} ({prestamo.RubroTexto})";
                TxtSaldoActual.Text = $"RD$ {prestamo.SaldoPendiente:N2}";
                TxtMontoAbono.Text = Math.Min(1000, prestamo.SaldoPendiente).ToString("F2");
                CalcularNuevoSaldoPrestamo(prestamo);
                
                bool esRecepcion = SessionManager.CurrentSession?.Rol == "Recepcionista";
                BtnAplicarAbono.IsEnabled = !esRecepcion;
            }
            else
            {
                DesactivarCobroPrestamo();
            }
        }

        private void DesactivarCobroPrestamo()
        {
            TxtDetallePrestamo.Text = "Selecciona un préstamo activo de la lista";
            TxtSaldoActual.Text = "RD$ 0.00";
            TxtNuevoSaldoEstimado.Text = "RD$ 0.00";
            TxtMontoAbono.Text = "0.00";
            BtnAplicarAbono.IsEnabled = false;
        }

        private void TxtMontoAbono_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (GridPrestamos.SelectedItem is PrestamoItem prestamo)
            {
                CalcularNuevoSaldoPrestamo(prestamo);
            }
        }

        private void CalcularNuevoSaldoPrestamo(PrestamoItem prestamo)
        {
            decimal.TryParse(TxtMontoAbono.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal abono);
            decimal nuevoSaldo = Math.Max(0, prestamo.SaldoPendiente - abono);
            TxtNuevoSaldoEstimado.Text = nuevoSaldo == 0 ? "RD$ 0.00 (¡PAGADO COMPLETAMENTE!)" : $"RD$ {nuevoSaldo:N2}";
        }

        private async void BtnAplicarAbono_Click(object sender, RoutedEventArgs e)
        {
            if (GridPrestamos.SelectedItem is not PrestamoItem prestamo)
                return;

            if (!decimal.TryParse(TxtMontoAbono.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal montoAbono) || montoAbono <= 0)
            {
                MessageBox.Show("El monto a abonar debe ser un valor positivo mayor a cero.", "Monto Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (montoAbono > prestamo.SaldoPendiente)
            {
                MessageBox.Show($"El monto a abonar (RD$ {montoAbono:N2}) no puede ser mayor que el saldo pendiente (RD$ {prestamo.SaldoPendiente:N2}).", "Monto Excesivo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string metodo = ((ComboBoxItem)CmbMetodoPagoPrestamo.SelectedItem)?.Content?.ToString() ?? "Efectivo";

            BtnAplicarAbono.IsEnabled = false;
            SetEstado($"Registrando abono a préstamo #{prestamo.Id}...");

            try
            {
                var (success, message, nuevoSaldo, recibo) = await _apiService.AbonarPrestamoAsync(prestamo.Id, montoAbono, metodo, montoAbono);
                if (!success)
                {
                    MessageBox.Show(message, "Error al Abonar", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await CargarPrestamosAsync();
                await CargarArqueoAsync();

                if (recibo != null)
                {
                    var preview = new ReceiptPreviewWindow(recibo) { Owner = this };
                    preview.ShowDialog();
                }
                else
                {
                    MessageBox.Show(message, "Abono Registrado", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                SetEstado("Abono a préstamo registrado exitosamente.");
            }
            finally
            {
                BtnAplicarAbono.IsEnabled = true;
            }
        }

        // =========================================================
        // 4. MÓDULO DE DONACIONES
        // =========================================================
        private void BtnDonanteAnonimo_Click(object sender, RoutedEventArgs e)
        {
            CmbDonantesFrecuentes.Text = "Donante Anónimo";
            TxtDonanteRnc.Text = "";
        }

        private void CmbDonantesFrecuentes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDonantesFrecuentes.SelectedItem is string nombreSeleccionado)
            {
                var cli = _clientesFrecuentes.FirstOrDefault(c => c.Nombre == nombreSeleccionado);
                if (cli != null && !string.IsNullOrWhiteSpace(cli.Documento))
                {
                    TxtDonanteRnc.Text = cli.Documento;
                }
            }
        }

        private async void BtnRegistrarDonacion_Click(object sender, RoutedEventArgs e)
        {
            string donante = string.IsNullOrWhiteSpace(CmbDonantesFrecuentes.Text) ? "Donante Anónimo" : CmbDonantesFrecuentes.Text.Trim();
            string rncCedula = TxtDonanteRnc.Text.Trim();

            if (string.IsNullOrWhiteSpace(donante))
            {
                MessageBox.Show("Por favor ingrese el nombre del donante o entidad benefactora.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtMontoDonacion.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Por favor ingrese un monto de donación válido mayor a cero.", "Monto Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string metodo = ((ComboBoxItem)CmbMetodoPagoDonacion.SelectedItem)?.Content?.ToString() ?? "Efectivo";
            bool requiereNcf = ChkRequiereNcf.IsChecked == true;

            BtnRegistrarDonacion.IsEnabled = false;
            SetEstado("Registrando donación en Caja...");

            try
            {
                var donacionReq = new DonacionRequest
                {
                    Donante = donante,
                    RncCedula = rncCedula,
                    Monto = monto,
                    MetodoPago = metodo,
                    RequiereComprobanteFiscal = requiereNcf
                };

                var (success, message, recibo) = await _apiService.RegistrarDonacionAsync(donacionReq);
                if (!success)
                {
                    MessageBox.Show(message, "Error al Registrar Donación", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CmbDonantesFrecuentes.Text = "";
                TxtDonanteRnc.Text = "";
                TxtMontoDonacion.Text = "1000.00";
                ChkRequiereNcf.IsChecked = false;

                await CargarArqueoAsync();
                await CargarClientesFrecuentesAsync();

                if (recibo != null)
                {
                    var preview = new ReceiptPreviewWindow(recibo) { Owner = this };
                    preview.ShowDialog();
                }
                else
                {
                    MessageBox.Show(message, "Donación Registrada", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                SetEstado("Donación institucional recibida y registrada.");
            }
            finally
            {
                BtnRegistrarDonacion.IsEnabled = true;
            }
        }

        // =========================================================
        // 5. MÓDULO DE ARQUEO Y CIERRE DE CAJA
        // =========================================================
        private async Task CargarArqueoAsync()
        {
            var cuadre = await _apiService.GetCuadreCajaAsync();
            if (cuadre == null) return;

            TxtKpiTotalGeneral.Text = $"RD$ {cuadre.TotalGeneral:N2}";
            TxtKpiCantidadTx.Text = $"{cuadre.CantidadTransacciones} transacciones registradas";

            TxtKpiEfectivo.Text = $"RD$ {cuadre.PorMetodo.Efectivo:N2}";
            TxtKpiTarjeta.Text = $"RD$ {cuadre.PorMetodo.Tarjeta:N2}";
            TxtKpiTransferencia.Text = $"RD$ {cuadre.PorMetodo.Transferencia:N2}";

            TxtKpiVentas.Text = $"RD$ {cuadre.PorConcepto.VentasProductos:N2}";
            TxtKpiMasajes.Text = $"RD$ {cuadre.PorConcepto.MasajesTacto:N2}";
            TxtKpiPrestamos.Text = $"RD$ {cuadre.PorConcepto.AbonosPrestamos:N2}";
            TxtKpiDonaciones.Text = $"RD$ {cuadre.PorConcepto.Donaciones:N2}";

            GridMovimientosTurno.ItemsSource = cuadre.Movimientos;
        }

        private async void BtnActualizarArqueo_Click(object sender, RoutedEventArgs e)
        {
            await CargarArqueoAsync();
            SetEstado("Arqueo de caja actualizado con éxito.");
        }

        private async void BtnImprimirCierre_Click(object sender, RoutedEventArgs e)
        {
            var cuadre = await _apiService.GetCuadreCajaAsync();
            if (cuadre == null)
            {
                MessageBox.Show("No se pudo obtener el cuadre de caja actual.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var transaccionCierre = new TransaccionCajaDto
            {
                NumeroRecibo = $"CIERRE-{DateTime.Now:yyyyMMdd-HHmm}",
                FechaHora = DateTime.Now,
                TipoTransaccion = "CIERRE DE CAJA (ARQUEO)",
                Concepto = $"Reporte Consolidado del Turno - {cuadre.CantidadTransacciones} operaciones",
                MontoTotal = cuadre.TotalGeneral,
                MetodoPago = "CONSOLIDADO",
                MontoRecibido = cuadre.TotalGeneral,
                Devuelta = 0,
                Cajero = SessionManager.CurrentSession?.Nombre ?? "Caja Principal",
                Sucursal = SessionManager.CurrentSession?.SucursalNombre ?? "Sede Central",
                ClienteOBeneficiario = "ADMINISTRACIÓN GENERAL",
                DetalleLineas = $"• Ventas Productos POS: RD$ {cuadre.PorConcepto.VentasProductos:N2}\n" +
                                $"• Cobro Masajes TACTO: RD$ {cuadre.PorConcepto.MasajesTacto:N2}\n" +
                                $"• Abonos Préstamos: RD$ {cuadre.PorConcepto.AbonosPrestamos:N2}\n" +
                                $"• Donaciones en Caja: RD$ {cuadre.PorConcepto.Donaciones:N2}\n" +
                                $"--------------------------------------\n" +
                                $"• Total Efectivo: RD$ {cuadre.PorMetodo.Efectivo:N2}\n" +
                                $"• Total Tarjeta: RD$ {cuadre.PorMetodo.Tarjeta:N2}\n" +
                                $"• Total Transferencia: RD$ {cuadre.PorMetodo.Transferencia:N2}"
            };

            var preview = new ReceiptPreviewWindow(transaccionCierre) { Owner = this };
            preview.ShowDialog();
        }

        // =========================================================
        // 6. HISTORIAL DE RECIBOS
        // =========================================================
        private async Task CargarHistorialRecibosAsync()
        {
            _recibosHistorial = await _apiService.GetTransaccionesAsync();
            FiltrarHistorialRecibos();
        }

        private void FiltrarHistorialRecibos()
        {
            string filtro = TxtBuscarRecibo.Text?.Trim().ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(filtro))
            {
                GridRecibos.ItemsSource = _recibosHistorial;
            }
            else
            {
                GridRecibos.ItemsSource = _recibosHistorial.Where(r =>
                    r.NumeroRecibo.ToLower().Contains(filtro) ||
                    (r.ClienteOBeneficiario != null && r.ClienteOBeneficiario.ToLower().Contains(filtro)) ||
                    r.Concepto.ToLower().Contains(filtro)).ToList();
            }
        }

        private void TxtBuscarRecibo_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarHistorialRecibos();
        }

        private void BtnReimprimirRecibo_Click(object sender, RoutedEventArgs e)
        {
            AbrirReciboSeleccionado();
        }

        private void GridRecibos_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AbrirReciboSeleccionado();
        }

        private void AbrirReciboSeleccionado()
        {
            if (GridRecibos.SelectedItem is TransaccionCajaDto recibo)
            {
                var preview = new ReceiptPreviewWindow(recibo) { Owner = this };
                preview.ShowDialog();
            }
            else
            {
                MessageBox.Show("Por favor seleccione un recibo de la lista.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =========================================================
        // 7. ADMINISTRACIÓN DE CAJEROS (SOLO ADMIN)
        // =========================================================
        private async Task CargarUsuariosAdminAsync()
        {
            var usuarios = await _apiService.GetUsuariosAsync();
            GridUsuariosAdmin.ItemsSource = null;
            GridUsuariosAdmin.ItemsSource = usuarios;
        }

        private async void BtnActualizarUsuariosAdmin_Click(object sender, RoutedEventArgs e)
        {
            await CargarUsuariosAdminAsync();
            SetEstado("Lista de empleados actualizada.");
        }

        private async void BtnRegistrarCajero_Click(object sender, RoutedEventArgs e)
        {
            string nombre = TxtNuevoNombreCompleto.Text.Trim();
            string usuario = TxtNuevoUsuario.Text.Trim();
            string correo = TxtNuevoCorreo.Text.Trim();
            string password = TxtNuevoPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Por favor complete el nombre, usuario y contraseña.", "Campos Requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int sucursalId = 1;
            if (CmbNuevaSucursal.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                int.TryParse(item.Tag.ToString(), out sucursalId);
            }

            int rol = 2;
            if (CmbNuevoRol.SelectedItem is ComboBoxItem rolItem && rolItem.Tag != null)
            {
                int.TryParse(rolItem.Tag.ToString(), out rol);
            }
            string rolTexto = rol == 5 ? "Recepcionista" : "Cajero";

            BtnRegistrarCajero.IsEnabled = false;
            SetEstado($"Registrando nuevo {rolTexto.ToLower()} en el sistema...");

            try
            {
                var (success, message, nuevoUsuario) = await _apiService.CrearUsuarioAsync(nombre, usuario, correo, password, rol, sucursalId);

                if (!success)
                {
                    MessageBox.Show(message, "Error al Crear Empleado", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show($"¡{rolTexto} '{nombre}' creado exitosamente!\n\nUsuario: {usuario}\nContraseña: {password}\nRol: {rolTexto}\n\nEl empleado ya puede iniciar sesión en el sistema con sus credenciales.", "Empleado Registrado", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtNuevoNombreCompleto.Text = "";
                TxtNuevoUsuario.Text = "";
                TxtNuevoCorreo.Text = "";
                TxtNuevoPassword.Text = "cajero123";

                await CargarUsuariosAdminAsync();
                SetEstado($"Nuevo {rolTexto.ToLower()} registrado satisfactoriamente.");
            }
            finally
            {
                BtnRegistrarCajero.IsEnabled = true;
            }
        }

        // =========================================================
        // CERRAR SESIÓN
        // =========================================================
        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("¿Está seguro de que desea cerrar la sesión actual del turno de caja?", "Cerrar Turno", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _clockTimer.Stop();
                SessionManager.Logout();
                var login = new LoginWindow();
                login.Show();
                Close();
            }
        }
    }
}