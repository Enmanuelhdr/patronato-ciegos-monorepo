using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Patronato.Caja.Models;
using Patronato.Caja.Services;

namespace Patronato.Caja.Views
{
    public partial class CierreCajaWindow : Window
    {
        private readonly CuadreCajaDto _cuadre;
        private TurnoCajaDto? _turno;
        private readonly ApiService _apiService = new();
        private readonly ObservableCollection<DenominacionItem> _denominaciones = new();

        public bool TurnoCerrado { get; private set; }

        public CierreCajaWindow(CuadreCajaDto cuadre, TurnoCajaDto? turno = null)
        {
            _cuadre = cuadre ?? new CuadreCajaDto();
            _turno = turno;

            InitializeComponent();

            InicializarDenominaciones();
            CargarDatos();
            RecalcularConteo();
        }

        private void InicializarDenominaciones()
        {
            // Denominaciones oficiales de curso legal en la República Dominicana (DOP)
            var items = new[]
            {
                new DenominacionItem { Valor = 2000, Descripcion = "Billete RD$ 2,000", EsBillete = true },
                new DenominacionItem { Valor = 1000, Descripcion = "Billete RD$ 1,000", EsBillete = true },
                new DenominacionItem { Valor = 500,  Descripcion = "Billete RD$ 500",   EsBillete = true },
                new DenominacionItem { Valor = 200,  Descripcion = "Billete RD$ 200",   EsBillete = true },
                new DenominacionItem { Valor = 100,  Descripcion = "Billete RD$ 100",   EsBillete = true },
                new DenominacionItem { Valor = 50,   Descripcion = "Billete RD$ 50",    EsBillete = true },
                new DenominacionItem { Valor = 25,   Descripcion = "Moneda RD$ 25",     EsBillete = false },
                new DenominacionItem { Valor = 10,   Descripcion = "Moneda RD$ 10",     EsBillete = false },
                new DenominacionItem { Valor = 5,    Descripcion = "Moneda RD$ 5",      EsBillete = false },
                new DenominacionItem { Valor = 1,    Descripcion = "Moneda RD$ 1",      EsBillete = false },
            };

            foreach (var item in items)
            {
                item.PropertyChanged += Item_PropertyChanged;
                _denominaciones.Add(item);
            }

            GridDenominaciones.ItemsSource = _denominaciones;
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RecalcularConteo();
        }

        private void GridDenominaciones_CellEditEnding(object? sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(RecalcularConteo), System.Windows.Threading.DispatcherPriority.Background);
        }

        private decimal _montoBoveda = 0;
        private decimal _montoRemanente = 0;
        private string _destinoFondos = "RetiroTotalBoveda";
        private bool _actualizandoManual = false;

        private decimal TotalEfectivoContado => _denominaciones.Sum(d => d.Total);

        private void AutoCompletarDesglose(decimal montoEsperado)
        {
            if (montoEsperado < 0) montoEsperado = 0;
            decimal restante = montoEsperado;

            foreach (var d in _denominaciones.OrderByDescending(x => x.Valor))
            {
                int cant = (int)(restante / d.Valor);
                d.Cantidad = cant;
                restante -= cant * d.Valor;
            }

            RecalcularConteo();
        }

        private void BtnAutoCompletarConteo_Click(object sender, RoutedEventArgs e)
        {
            decimal fondo = _turno?.FondoInicial ?? _cuadre.FondoInicial;
            decimal ventasEfectivo = _cuadre.PorMetodo?.Efectivo ?? 0m;
            decimal esperado = _cuadre.EfectivoEsperadoEnCaja > 0 
                ? _cuadre.EfectivoEsperadoEnCaja 
                : (fondo + ventasEfectivo);

            AutoCompletarDesglose(esperado);
        }

        private void BtnLimpiarConteo_Click(object sender, RoutedEventArgs e)
        {
            foreach (var d in _denominaciones)
            {
                d.Cantidad = 0;
            }
            RecalcularConteo();
        }

        private void RecalcularConteo()
        {
            if (_cuadre == null || TxtEfectivoContadoTotal == null || TxtEfectivoEsperado == null || TxtDiferenciaValor == null) return;

            decimal contado = TotalEfectivoContado;
            decimal fondo = _turno?.FondoInicial ?? _cuadre.FondoInicial;
            decimal ventasEfectivo = _cuadre.PorMetodo?.Efectivo ?? 0m;
            decimal esperado = _cuadre.EfectivoEsperadoEnCaja > 0 
                ? _cuadre.EfectivoEsperadoEnCaja 
                : (fondo + ventasEfectivo);

            decimal diferencia = contado - esperado;

            TxtEfectivoContadoTotal.Text = $"RD$ {contado:N2}";
            TxtEfectivoEsperado.Text = $"RD$ {esperado:N2}";
            TxtDiferenciaValor.Text = $"RD$ {Math.Abs(diferencia):N2}";

            if (BannerDiferencia != null)
            {
                if (contado == 0 && esperado > 0)
                {
                    BannerDiferencia.Background = new SolidColorBrush(Color.FromRgb(254, 249, 195));
                    BannerDiferencia.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 224, 71));
                    if (TxtDiferenciaTitulo != null)
                    {
                        TxtDiferenciaTitulo.Text = "⏳ CONTEO PENDIENTE";
                        TxtDiferenciaTitulo.Foreground = new SolidColorBrush(Color.FromRgb(161, 98, 7));
                    }
                    if (TxtDiferenciaSubtitulo != null)
                    {
                        TxtDiferenciaSubtitulo.Text = "Ingrese las cantidades por denominación o pulse '⚡ Cuadre Exacto'";
                        TxtDiferenciaSubtitulo.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                    }
                    if (TxtDiferenciaValor != null)
                    {
                        TxtDiferenciaValor.Foreground = new SolidColorBrush(Color.FromRgb(161, 98, 7));
                    }
                    if (TxtBottomEstadoCuadre != null)
                    {
                        TxtBottomEstadoCuadre.Text = "⏳ Conteo pendiente";
                        TxtBottomEstadoCuadre.Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14));
                    }
                }
                else if (diferencia == 0)
                {
                    BannerDiferencia.Background = new SolidColorBrush(Color.FromRgb(236, 253, 245));
                    BannerDiferencia.BorderBrush = new SolidColorBrush(Color.FromRgb(167, 243, 208));
                    if (TxtDiferenciaTitulo != null)
                    {
                        TxtDiferenciaTitulo.Text = "CUADRE EXACTO";
                        TxtDiferenciaTitulo.Foreground = new SolidColorBrush(Color.FromRgb(6, 95, 70));
                    }
                    if (TxtDiferenciaSubtitulo != null)
                    {
                        TxtDiferenciaSubtitulo.Text = "Sin discrepancias en el arqueo físico de caja";
                        TxtDiferenciaSubtitulo.Foreground = new SolidColorBrush(Color.FromRgb(4, 120, 87));
                    }
                    if (TxtDiferenciaValor != null)
                    {
                        TxtDiferenciaValor.Foreground = new SolidColorBrush(Color.FromRgb(6, 95, 70));
                    }
                    if (TxtBottomEstadoCuadre != null)
                    {
                        TxtBottomEstadoCuadre.Text = "✅ Cuadre Exacto";
                        TxtBottomEstadoCuadre.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                    }
                }
                else if (diferencia > 0)
                {
                    BannerDiferencia.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));
                    BannerDiferencia.BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254));
                    if (TxtDiferenciaTitulo != null)
                    {
                        TxtDiferenciaTitulo.Text = "SOBRANTE EN CAJA (+)";
                        TxtDiferenciaTitulo.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                    }
                    if (TxtDiferenciaSubtitulo != null)
                    {
                        TxtDiferenciaSubtitulo.Text = "El conteo físico supera el monto esperado en caja";
                        TxtDiferenciaSubtitulo.Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                    }
                    if (TxtDiferenciaValor != null)
                    {
                        TxtDiferenciaValor.Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138));
                    }
                    if (TxtBottomEstadoCuadre != null)
                    {
                        TxtBottomEstadoCuadre.Text = $"⬆️ Sobrante: +RD$ {diferencia:N2}";
                        TxtBottomEstadoCuadre.Foreground = new SolidColorBrush(Color.FromRgb(29, 78, 216));
                    }
                }
                else
                {
                    BannerDiferencia.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                    BannerDiferencia.BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202));
                    if (TxtDiferenciaTitulo != null)
                    {
                        TxtDiferenciaTitulo.Text = "FALTANTE EN CAJA (-)";
                        TxtDiferenciaTitulo.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));
                    }
                    if (TxtDiferenciaSubtitulo != null)
                    {
                        TxtDiferenciaSubtitulo.Text = "El efectivo contado es inferior al esperado en gaveta";
                        TxtDiferenciaSubtitulo.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                    }
                    if (TxtDiferenciaValor != null)
                    {
                        TxtDiferenciaValor.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));
                    }
                    if (TxtBottomEstadoCuadre != null)
                    {
                        TxtBottomEstadoCuadre.Text = $"⚠️ Faltante: -RD$ {Math.Abs(diferencia):N2}";
                        TxtBottomEstadoCuadre.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                    }
                }
            }

            if (TxtBottomEfectivoContado != null)
            {
                TxtBottomEfectivoContado.Text = $"Contado: RD$ {contado:N2} / Esperado: RD$ {esperado:N2}";
            }

            RecalcularDistribucion();
        }

        private void RecalcularDistribucion()
        {
            if (_cuadre == null) return;
            decimal contado = TotalEfectivoContado;
            decimal fondo = _turno?.FondoInicial ?? (_cuadre?.FondoInicial ?? 0m);

            if (RbRetiroTotal != null && RbRetiroTotal.IsChecked == true)
            {
                _destinoFondos = "RetiroTotalBoveda";
                _montoBoveda = contado;
                _montoRemanente = 0;
                if (PanelDistribucionManual != null) PanelDistribucionManual.Visibility = Visibility.Collapsed;
            }
            else if (RbDejarFondo != null && RbDejarFondo.IsChecked == true)
            {
                _destinoFondos = "DejarFondoEnCaja";
                _montoRemanente = Math.Min(fondo, contado);
                _montoBoveda = Math.Max(0, contado - _montoRemanente);
                if (PanelDistribucionManual != null) PanelDistribucionManual.Visibility = Visibility.Collapsed;
            }
            else if (RbPersonalizado != null && RbPersonalizado.IsChecked == true)
            {
                _destinoFondos = "Personalizado";
                if (PanelDistribucionManual != null) PanelDistribucionManual.Visibility = Visibility.Visible;
                if (!_actualizandoManual)
                {
                    _montoBoveda = contado;
                    _montoRemanente = 0;
                    if (TxtMontoBovedaManual != null) TxtMontoBovedaManual.Text = _montoBoveda.ToString("F2");
                    if (TxtMontoRemanenteManual != null) TxtMontoRemanenteManual.Text = _montoRemanente.ToString("F2");
                }
            }

            ActualizarVistasDistribucion(contado);
        }

        private void ActualizarVistasDistribucion(decimal contado)
        {
            if (TxtResumenEntregaBoveda != null) TxtResumenEntregaBoveda.Text = $"RD$ {_montoBoveda:N2}";
            if (TxtResumenQuedaGaveta != null) TxtResumenQuedaGaveta.Text = $"RD$ {_montoRemanente:N2}";

            if (TxtReciboEfectivoContado != null) TxtReciboEfectivoContado.Text = $"RD$ {contado:N2}";
            if (TxtReciboFondoRemanente != null) TxtReciboFondoRemanente.Text = $"RD$ {_montoRemanente:N2}";
            if (TxtReciboEntregadoBoveda != null) TxtReciboEntregadoBoveda.Text = $"RD$ {_montoBoveda:N2}";
            if (TxtReciboDestinoTexto != null)
            {
                TxtReciboDestinoTexto.Text = _destinoFondos switch
                {
                    "RetiroTotalBoveda" => "Modalidad: Retiro total de valores hacia bóveda (la gaveta queda en RD$ 0.00).",
                    "DejarFondoEnCaja" => $"Modalidad: Se deja fondo base de RD$ {_montoRemanente:N2} en gaveta para el próximo turno y se entrega la recaudación a bóveda.",
                    _ => $"Modalidad: Distribución personalizada (Entregado a Bóveda: RD$ {_montoBoveda:N2}, Remanente en Gaveta: RD$ {_montoRemanente:N2})."
                };
            }
        }

        private void RbDestino_Checked(object sender, RoutedEventArgs e)
        {
            RecalcularDistribucion();
        }

        private void TxtMontoBovedaManual_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_actualizandoManual || RbPersonalizado?.IsChecked != true) return;
            _actualizandoManual = true;
            try
            {
                if (decimal.TryParse(TxtMontoBovedaManual.Text, out var boveda))
                {
                    decimal contado = TotalEfectivoContado;
                    _montoBoveda = Math.Min(boveda, contado);
                    _montoRemanente = Math.Max(0, contado - _montoBoveda);
                    if (TxtMontoRemanenteManual != null) TxtMontoRemanenteManual.Text = _montoRemanente.ToString("F2");
                    ActualizarVistasDistribucion(contado);
                }
            }
            finally
            {
                _actualizandoManual = false;
            }
        }

        private void TxtMontoRemanenteManual_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_actualizandoManual || RbPersonalizado?.IsChecked != true) return;
            _actualizandoManual = true;
            try
            {
                if (decimal.TryParse(TxtMontoRemanenteManual.Text, out var rem))
                {
                    decimal contado = TotalEfectivoContado;
                    _montoRemanente = Math.Min(rem, contado);
                    _montoBoveda = Math.Max(0, contado - _montoRemanente);
                    if (TxtMontoBovedaManual != null) TxtMontoBovedaManual.Text = _montoBoveda.ToString("F2");
                    ActualizarVistasDistribucion(contado);
                }
            }
            finally
            {
                _actualizandoManual = false;
            }
        }

        private void CargarDatos()
        {
            if (_turno == null && _cuadre.TieneTurnoActivo && _cuadre.TurnoId.HasValue)
            {
                _turno = new TurnoCajaDto
                {
                    Id = _cuadre.TurnoId.Value,
                    FondoInicial = _cuadre.FondoInicial,
                    Cajero = _cuadre.Cajero,
                    Estado = "Abierto"
                };
            }

            if (_turno != null)
            {
                string idStr = _turno.Id < 0 ? $"Contingencia #{Math.Abs(_turno.Id):0000}" : $"#{_turno.Id}";
                TxtBadgeTurno.Text = $"{idStr} ({_turno.Estado})";
                TxtNumeroCierre.Text = $"TURNO-{Math.Abs(_turno.Id):0000}-{DateTime.Now:yyyyMMdd}";
            }
            else
            {
                TxtBadgeTurno.Text = "General";
                TxtNumeroCierre.Text = $"CIERRE-{DateTime.Now:yyyyMMdd-HHmm}";
            }

            TxtFecha.Text = DateTime.TryParse(_cuadre.Fecha, out var fecha) ? fecha.ToString("dd/MM/yyyy") : _cuadre.Fecha;
            TxtSucursal.Text = string.IsNullOrWhiteSpace(SessionManager.CurrentSession?.SucursalNombre)
                ? "Sede Central Santo Domingo"
                : SessionManager.CurrentSession!.SucursalNombre;
            TxtCajero.Text = string.IsNullOrWhiteSpace(_cuadre.Cajero)
                ? (SessionManager.CurrentSession?.Nombre ?? "Caja Principal")
                : _cuadre.Cajero;

            decimal fondo = _turno?.FondoInicial ?? _cuadre.FondoInicial;
            decimal ventasEfectivo = _cuadre.PorMetodo?.Efectivo ?? 0m;
            decimal esperado = _cuadre.EfectivoEsperadoEnCaja > 0 
                ? _cuadre.EfectivoEsperadoEnCaja 
                : (fondo + ventasEfectivo);

            TxtFondoInicialResumen.Text = $"RD$ {fondo:N2}";
            TxtVentasEfectivoResumen.Text = $"RD$ {ventasEfectivo:N2}";
            TxtAnulacionesResumen.Text = $"RD$ {_cuadre.TotalAnulado:N2}";
            TxtEfectivoEsperado.Text = $"RD$ {esperado:N2}";

            TxtReporteFondoInicial.Text = $"RD$ {fondo:N2}";
            TxtReporteEfectivoEsperado.Text = $"RD$ {esperado:N2}";

            TxtTotalEfectivo.Text = $"RD$ {_cuadre.PorMetodo?.Efectivo ?? 0m:N2}";
            TxtTotalTarjeta.Text = $"RD$ {_cuadre.PorMetodo?.Tarjeta ?? 0m:N2}";
            TxtTotalTransferencia.Text = $"RD$ {_cuadre.PorMetodo?.Transferencia ?? 0m:N2}";

            TxtTotalVentas.Text = $"RD$ {_cuadre.PorConcepto?.VentasProductos ?? 0m:N2}";
            TxtTotalMasajes.Text = $"RD$ {_cuadre.PorConcepto?.MasajesTacto ?? 0m:N2}";
            TxtTotalPrestamos.Text = $"RD$ {_cuadre.PorConcepto?.AbonosPrestamos ?? 0m:N2}";
            TxtTotalConsultas.Text = $"RD$ {_cuadre.PorConcepto?.Consultas ?? 0m:N2}";
            TxtTotalDonaciones.Text = $"RD$ {_cuadre.PorConcepto?.Donaciones ?? 0m:N2}";

            TxtTotalGeneral.Text = $"RD$ {_cuadre.TotalGeneral:N2}";
            TxtCantidadTx.Text = $"{_cuadre.CantidadTransacciones} transacciones auditadas en este corte";

            GridMovimientos.ItemsSource = _cuadre.Movimientos;
            TxtGeneradoEn.Text = $"Generado el {DateTime.Now:dd/MM/yyyy hh:mm tt}";
        }

        private async void BtnCerrarTurnoFinal_Click(object sender, RoutedEventArgs e)
        {
            if (_turno == null)
            {
                try
                {
                    var resTurno = await _apiService.GetTurnoActivoAsync();
                    if (resTurno != null && resTurno.TieneTurnoActivo && resTurno.Turno != null)
                    {
                        _turno = resTurno.Turno;
                    }
                }
                catch { }
            }

            int turnoId = _turno?.Id ?? (_cuadre.TurnoId ?? 0);
            if (turnoId == 0 && _turno == null)
            {
                MessageBox.Show("No se encontró un turno activo registrado en el sistema para cerrar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal contado = TotalEfectivoContado;
            decimal fondo = _turno?.FondoInicial ?? _cuadre.FondoInicial;
            decimal ventasEfectivo = _cuadre.PorMetodo?.Efectivo ?? 0m;
            decimal esperado = _cuadre.EfectivoEsperadoEnCaja > 0 ? _cuadre.EfectivoEsperadoEnCaja : (fondo + ventasEfectivo);

            if (contado == 0 && esperado > 0)
            {
                var autoRes = MessageBox.Show(
                    $"El efectivo físico contado está en RD$ 0.00, pero el monto esperado en gaveta es RD$ {esperado:N2}.\n\n" +
                    "¿Desea auto-completar el arqueo con el monto esperado (Cuadre Exacto) antes de cerrar el turno?\n\n" +
                    "• Seleccione 'Sí' para auto-completar el desglose exacto y proceder con el cierre.\n" +
                    "• Seleccione 'No' para proceder cerrando formalmente con FALTANTE de RD$ " + esperado.ToString("N2") + ".\n" +
                    "• Seleccione 'Cancelar' para revisar las cantidades manualmente.",
                    "Arqueo Físico en Cero - Verificación de Caja",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (autoRes == MessageBoxResult.Cancel) return;

                if (autoRes == MessageBoxResult.Yes)
                {
                    AutoCompletarDesglose(esperado);
                    contado = TotalEfectivoContado;
                }
            }

            decimal dif = contado - esperado;

            string resumenDif = dif == 0 
                ? "Cuadre EXACTO (RD$ 0.00)" 
                : (dif > 0 ? $"SOBRANTE de RD$ {dif:N2}" : $"FALTANTE de RD$ {Math.Abs(dif):N2}");

            string idTexto = (_turno != null && _turno.Id < 0) ? $"Contingencia #{Math.Abs(_turno.Id):0000}" : $"#{turnoId}";

            var confirm = MessageBox.Show(
                $"¿CONFIRMA EL CIERRE DEL TURNO {idTexto} Y ARQUEO DE CAJA?\n\n" +
                $"• Fondo Inicial de Apertura: RD$ {fondo:N2}\n" +
                $"• Efectivo Esperado en Gaveta: RD$ {esperado:N2}\n" +
                $"• Efectivo Físico Contado: RD$ {contado:N2}\n" +
                $"• Resultado del Arqueo: {resumenDif}\n\n" +
                $"💼 DESTINO DEL EFECTIVO:\n" +
                $"• Retiro a Bóveda / Remesa Bancaria: RD$ {_montoBoveda:N2}\n" +
                $"• Fondo Remanente dejado en Gaveta: RD$ {_montoRemanente:N2}\n" +
                $"• Modalidad: {_destinoFondos}\n\n" +
                "Al confirmar, el turno quedará cerrado y se generará el Comprobante Oficial de Entrega de Valores.",
                "Confirmación de Cierre de Turno y Remesa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            BtnCerrarTurnoFinal.IsEnabled = false;
            if (BtnCerrarTurnoPrincipal != null) BtnCerrarTurnoPrincipal.IsEnabled = false;

            try
            {
                var desgloseDict = _denominaciones.ToDictionary(d => d.Descripcion, d => d.Cantidad);
                string desgloseJson = JsonSerializer.Serialize(desgloseDict);

                var (success, message, _) = await _apiService.CerrarTurnoAsync(
                    turnoId,
                    contado,
                    desgloseJson,
                    TxtObservacionesCierre.Text.Trim(),
                    _montoBoveda,
                    _montoRemanente,
                    _destinoFondos);

                if (!success)
                {
                    MessageBox.Show(message, "Error al Cerrar Turno", MessageBoxButton.OK, MessageBoxImage.Error);
                    BtnCerrarTurnoFinal.IsEnabled = true;
                    if (BtnCerrarTurnoPrincipal != null) BtnCerrarTurnoPrincipal.IsEnabled = true;
                    return;
                }

                TurnoCerrado = true;
                MessageBox.Show(
                    $"El Turno {idTexto} ha sido cerrado exitosamente.\n\n" +
                    $"• Entregado a Bóveda: RD$ {_montoBoveda:N2}\n" +
                    $"• Fondo Remanente en Gaveta: RD$ {_montoRemanente:N2}\n\n" +
                    "A continuación puede revisar o imprimir el Comprobante Formal de Cierre y Remesa.",
                    "Turno Cerrado Exitosamente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Cambiar a la pestaña de reporte formal para que puedan imprimirlo
                TabCierre.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cerrar el turno: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnCerrarTurnoFinal.IsEnabled = true;
                if (BtnCerrarTurnoPrincipal != null) BtnCerrarTurnoPrincipal.IsEnabled = true;
            }
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            bool exito = TicketPrintService.ImprimirVisual(ReporteVisualBorder, $"Cierre de Caja {TxtNumeroCierre.Text}");
            if (exito)
            {
                MessageBox.Show("El reporte de cierre de caja ha sido enviado a la cola de impresión.", "Impresión Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
