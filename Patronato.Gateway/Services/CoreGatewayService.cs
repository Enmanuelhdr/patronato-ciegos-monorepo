using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Patronato.Gateway.Protocol;

namespace Patronato.Gateway.Services;

// Punto único donde convergen la petición REST (web) y la petición por socket (Caja):
// ambas terminan aquí como un TransaccionRequest y salen como una sola llamada al Core.
// Si el Core está caído, las transacciones de cobro no se pierden: quedan encoladas en
// PendingQueueStore y CoreSyncWorker las reenvía cuando el Core vuelve a responder.
public class CoreGatewayService
{
    private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(CoreGatewayService));

    private readonly HttpClient _httpClient;
    private readonly PendingQueueStore _cola;

    public CoreGatewayService(HttpClient httpClient, PendingQueueStore cola)
    {
        _httpClient = httpClient;
        _cola = cola;
    }

    public async Task<TransaccionResponse> ProcesarAsync(TransaccionRequest request, string origen)
    {
        log4net.LogicalThreadContext.Properties["Origen"] = origen;
        Logger.Info($"Transacción {request.CodigoTransaccion} STAN={request.Stan} recibida desde {origen}");

        try
        {
            var respuesta = request.CodigoTransaccion switch
            {
                // Línea viva (como el "keep-alive" de un cajero automático): no toca el Core.
                "PING" => Aprobar(request, "Gateway operativo."),

                // Transaccionales: mueven dinero real, siempre contra el Core.
                "MASAJE_COBRAR" => await CobrarMasajeAsync(request, origen),
                "CONSULTA_COBRAR" => await CobrarConsultaAsync(request, origen),
                "PRESTAMO_ABONAR" => await AbonarPrestamoAsync(request, origen),
                "PRODUCTO_VENDER" => await VenderProductoAsync(request, origen),
                "VENTA_CARRITO" => await VenderCarritoAsync(request, origen),
                "DONACION_REGISTRAR" => await RegistrarDonacionAsync(request, origen),

                // Consultas: la Caja ya no usa HTTP en absoluto, todo entra por el mismo socket.
                "LOGIN" => await LoginAsync(request, origen),
                "SUCURSALES_CONSULTAR" => await ConsultarAsync(request, origen, "api/Sucursales"),
                "INVENTARIO_CONSULTAR" => await ConsultarAsync(request, origen, "api/inventario"),
                "MASAJES_PENDIENTES_CONSULTAR" => await ConsultarAsync(request, origen, "api/caja/masajes-pendientes"),
                "CONSULTAS_PENDIENTES_CONSULTAR" => await ConsultarAsync(request, origen, "api/caja/consultas-pendientes"),
                "CLIENTES_FRECUENTES_CONSULTAR" => await ConsultarAsync(request, origen, "api/caja/clientes-frecuentes"),
                "ORDENES_PENDIENTES_CONSULTAR" => await ConsultarAsync(request, origen, "api/caja/ordenes-pendientes"),
                "PRESTAMOS_ACTIVOS_CONSULTAR" => await PrestamosActivosAsync(request, origen),
                "CUADRE_CAJA_CONSULTAR" => await CuadreCajaAsync(request, origen),
                "TRANSACCIONES_CONSULTAR" => await TransaccionesAsync(request, origen),

                _ => Rechazar(request, "12", $"Código de transacción no soportado: {request.CodigoTransaccion}")
            };

            Logger.Info($"Transacción {request.CodigoTransaccion} STAN={request.Stan} -> {respuesta.CodigoRespuesta}");
            return respuesta;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error procesando transacción {request.CodigoTransaccion} STAN={request.Stan} desde {origen}", ex);
            return Rechazar(request, "96", $"Error del gateway: {ex.Message}");
        }
    }

    private async Task<TransaccionResponse> CobrarMasajeAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var id = datos.GetProperty("id").GetInt32();
        var query = ConstruirQueryCobro(datos);
        var ruta = $"api/caja/cobrar-masaje/{id}?{query}";
        return await EnviarCobroAsync(request, origen, HttpMethod.Post, ruta, null, null);
    }

    private async Task<TransaccionResponse> CobrarConsultaAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var id = datos.GetProperty("id").GetInt32();
        var query = ConstruirQueryCobro(datos);
        var ruta = $"api/caja/cobrar-consulta/{id}?{query}";
        return await EnviarCobroAsync(request, origen, HttpMethod.Post, ruta, null, null);
    }

    private async Task<TransaccionResponse> AbonarPrestamoAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var prestamoId = datos.GetProperty("prestamoId").GetInt32();
        var monto = datos.GetProperty("monto").GetDecimal();
        var query = ConstruirQueryCobro(datos);
        var ruta = $"api/caja/abonar-prestamo?prestamoId={prestamoId}&monto={monto}&{query}";
        return await EnviarCobroAsync(request, origen, HttpMethod.Post, ruta, null, null);
    }

    private async Task<TransaccionResponse> VenderProductoAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var productoId = datos.GetProperty("productoId").GetInt32();
        var cantidad = datos.GetProperty("cantidad").GetInt32();
        var query = ConstruirQueryCobro(datos);
        if (datos.TryGetProperty("cliente", out var clienteEl) && clienteEl.ValueKind == JsonValueKind.String)
            query += $"&cliente={Uri.EscapeDataString(clienteEl.GetString() ?? string.Empty)}";

        var ruta = $"api/caja/vender-producto?productoId={productoId}&cantidad={cantidad}&{query}";
        return await EnviarCobroAsync(request, origen, HttpMethod.Post, ruta, null, null);
    }

    private async Task<TransaccionResponse> VenderCarritoAsync(TransaccionRequest request, string origen)
    {
        var json = request.Datos.GetRawText();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await EnviarCobroAsync(request, origen, HttpMethod.Post, "api/caja/vender-carrito", content, json);
    }

    private async Task<TransaccionResponse> RegistrarDonacionAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var cajero = ObtenerTexto(datos, "cajero", "Caja Principal");
        var sucursal = ObtenerTexto(datos, "sucursal", "Sede Central");
        var json = datos.GetRawText();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var query = $"cajero={Uri.EscapeDataString(cajero)}&sucursal={Uri.EscapeDataString(sucursal)}";
        var ruta = $"api/caja/donacion?{query}";
        return await EnviarCobroAsync(request, origen, HttpMethod.Post, ruta, content, json);
    }

    // Envía una transacción de cobro al Core. Si el Core no responde (está caído), no se
    // rechaza: se encola para reenviarla más tarde (ver CoreSyncWorker) y se le avisa a quien
    // llamó (Web o Caja) que quedó aceptada-pendiente ("01"), no rechazada.
    private async Task<TransaccionResponse> EnviarCobroAsync(
        TransaccionRequest request, string origen, HttpMethod metodo, string ruta, HttpContent? content, string? cuerpoJsonParaCola)
    {
        try
        {
            var response = await EnviarAsync(metodo, ruta, content, origen);
            return await TraducirRespuestaAsync(request, response);
        }
        catch (Exception ex) when (EsErrorDeConectividad(ex))
        {
            _cola.Encolar(new SolicitudPendiente
            {
                Metodo = metodo.Method,
                Ruta = ruta,
                CuerpoJson = cuerpoJsonParaCola,
                Origen = origen,
                CodigoTransaccion = request.CodigoTransaccion,
                Stan = request.Stan
            });

            Logger.Warn($"Core no disponible: transacción {request.CodigoTransaccion} STAN={request.Stan} encolada ({_cola.Cantidad} pendiente(s)).", ex);

            return new TransaccionResponse
            {
                Stan = request.Stan,
                CodigoRespuesta = "01",
                Mensaje = "El Core no está disponible en este momento: la operación quedó registrada y se confirmará automáticamente en cuanto vuelva en línea."
            };
        }
    }

    // Reenvía una transacción que había quedado encolada. Lo usa CoreSyncWorker cuando el
    // Core vuelve a responder. Devuelve false solo si sigue sin poder conectar (se reintenta
    // en el siguiente ciclo); un rechazo de negocio del Core (p.ej. stock ya no disponible)
    // se considera resuelto (true) porque reintentarlo no cambiaría el resultado.
    public async Task<bool> ReenviarPendienteAsync(SolicitudPendiente pendiente)
    {
        try
        {
            HttpContent? content = pendiente.CuerpoJson != null
                ? new StringContent(pendiente.CuerpoJson, Encoding.UTF8, "application/json")
                : null;

            var response = await EnviarAsync(new HttpMethod(pendiente.Metodo), pendiente.Ruta, content, pendiente.Origen);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"El Core rechazó la transacción encolada {pendiente.CodigoTransaccion} STAN={pendiente.Stan} ({response.StatusCode}); se descarta.");
            }

            return true;
        }
        catch (Exception ex) when (EsErrorDeConectividad(ex))
        {
            return false;
        }
    }

    private static bool EsErrorDeConectividad(Exception ex) =>
        ex is HttpRequestException or SocketException or TaskCanceledException or OperationCanceledException;

    private async Task<TransaccionResponse> LoginAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var usuario = Uri.EscapeDataString(ObtenerTexto(datos, "nombreUsuario", ""));
        var password = Uri.EscapeDataString(ObtenerTexto(datos, "password", ""));
        var response = await EnviarAsync(HttpMethod.Post, $"api/Usuarios/login?nombreUsuario={usuario}&password={password}", null, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    private async Task<TransaccionResponse> PrestamosActivosAsync(TransaccionRequest request, string origen)
    {
        var filtro = ObtenerTexto(request.Datos, "filtro", "");
        var ruta = "api/caja/prestamos-activos";
        if (!string.IsNullOrWhiteSpace(filtro))
            ruta += $"?filtro={Uri.EscapeDataString(filtro)}";
        return await ConsultarAsync(request, origen, ruta);
    }

    private async Task<TransaccionResponse> CuadreCajaAsync(TransaccionRequest request, string origen)
    {
        var ruta = "api/caja/cuadre-caja" + ConstruirQueryFechaCajero(request.Datos);
        return await ConsultarAsync(request, origen, ruta);
    }

    private async Task<TransaccionResponse> TransaccionesAsync(TransaccionRequest request, string origen)
    {
        var ruta = "api/caja/transacciones" + ConstruirQueryFechaCajero(request.Datos);
        return await ConsultarAsync(request, origen, ruta);
    }

    private static string ConstruirQueryFechaCajero(JsonElement datos)
    {
        var fecha = ObtenerTexto(datos, "fecha", "");
        var cajero = ObtenerTexto(datos, "cajero", "");
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(fecha)) partes.Add($"fecha={Uri.EscapeDataString(fecha)}");
        if (!string.IsNullOrWhiteSpace(cajero)) partes.Add($"cajero={Uri.EscapeDataString(cajero)}");
        return partes.Count > 0 ? "?" + string.Join("&", partes) : "";
    }

    // Consulta genérica de solo lectura: reenvía el GET al Core tal cual (la Caja ya
    // nunca le habla directo), sin armar ningún query string especial.
    private async Task<TransaccionResponse> ConsultarAsync(TransaccionRequest request, string origen, string ruta)
    {
        var response = await EnviarAsync(HttpMethod.Get, ruta, null, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    // Arma el query string común a los endpoints de cobro: así el Core recibe el método de pago,
    // el monto recibido en efectivo y la identidad real del cajero/sucursal que vienen de la Caja,
    // en vez de quedarse con los valores por defecto del Core.
    private static string ConstruirQueryCobro(JsonElement datos)
    {
        var metodoPago = ObtenerTexto(datos, "metodoPago", "Efectivo");
        var cajero = ObtenerTexto(datos, "cajero", "Caja Principal");
        var sucursal = ObtenerTexto(datos, "sucursal", "Sede Central");

        var query = $"metodoPago={Uri.EscapeDataString(metodoPago)}&cajero={Uri.EscapeDataString(cajero)}&sucursal={Uri.EscapeDataString(sucursal)}";

        if (datos.TryGetProperty("montoRecibido", out var montoEl) && montoEl.ValueKind == JsonValueKind.Number)
            query += $"&montoRecibido={montoEl.GetDecimal()}";

        return query;
    }

    private static string ObtenerTexto(JsonElement datos, string propiedad, string valorPorDefecto) =>
        datos.TryGetProperty(propiedad, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? valorPorDefecto
            : valorPorDefecto;

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, HttpContent? content, string origen)
    {
        var mensaje = new HttpRequestMessage(metodo, url) { Content = content };
        mensaje.Headers.Add("X-Origen", origen);
        return await _httpClient.SendAsync(mensaje);
    }

    private static async Task<TransaccionResponse> TraducirRespuestaAsync(TransaccionRequest request, HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        object? datos = null;
        string mensaje = "";

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                datos = doc.RootElement.Clone();
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("mensaje", out var m))
                {
                    mensaje = m.GetString() ?? "";
                }
            }
            catch (JsonException)
            {
                // El Core respondió texto plano (p.ej. NotFound("...")) en vez de JSON.
                mensaje = body;
            }
        }

        return new TransaccionResponse
        {
            Stan = request.Stan,
            CodigoRespuesta = response.IsSuccessStatusCode ? "00" : "05",
            Mensaje = string.IsNullOrEmpty(mensaje) ? body : mensaje,
            Datos = datos
        };
    }

    private static TransaccionResponse Rechazar(TransaccionRequest request, string codigoRespuesta, string mensaje) =>
        new() { Stan = request.Stan, CodigoRespuesta = codigoRespuesta, Mensaje = mensaje };

    private static TransaccionResponse Aprobar(TransaccionRequest request, string mensaje) =>
        new() { Stan = request.Stan, CodigoRespuesta = "00", Mensaje = mensaje };
}
