using System.Text;
using System.Text.Json;
using Patronato.Gateway.Protocol;

namespace Patronato.Gateway.Services;

// Punto único donde convergen la petición REST (web) y la petición por socket (Caja):
// ambas terminan aquí como un TransaccionRequest y salen como una sola llamada al Core.
public class CoreGatewayService
{
    private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(CoreGatewayService));

    private readonly HttpClient _httpClient;

    public CoreGatewayService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TransaccionResponse> ProcesarAsync(TransaccionRequest request, string origen)
    {
        log4net.LogicalThreadContext.Properties["Origen"] = origen;
        Logger.Info($"Transacción {request.CodigoTransaccion} STAN={request.Stan} recibida desde {origen}");

        try
        {
            var respuesta = request.CodigoTransaccion switch
            {
                "MASAJE_COBRAR" => await CobrarMasajeAsync(request, origen),
                "PRESTAMO_ABONAR" => await AbonarPrestamoAsync(request, origen),
                "PRODUCTO_VENDER" => await VenderProductoAsync(request, origen),
                "VENTA_CARRITO" => await VenderCarritoAsync(request, origen),
                "DONACION_REGISTRAR" => await RegistrarDonacionAsync(request, origen),
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
        var response = await EnviarAsync(HttpMethod.Post, $"api/caja/cobrar-masaje/{id}?{query}", null, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    private async Task<TransaccionResponse> AbonarPrestamoAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var prestamoId = datos.GetProperty("prestamoId").GetInt32();
        var monto = datos.GetProperty("monto").GetDecimal();
        var query = ConstruirQueryCobro(datos);
        var response = await EnviarAsync(HttpMethod.Post,
            $"api/caja/abonar-prestamo?prestamoId={prestamoId}&monto={monto}&{query}", null, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    private async Task<TransaccionResponse> VenderProductoAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var productoId = datos.GetProperty("productoId").GetInt32();
        var cantidad = datos.GetProperty("cantidad").GetInt32();
        var query = ConstruirQueryCobro(datos);
        if (datos.TryGetProperty("cliente", out var clienteEl) && clienteEl.ValueKind == JsonValueKind.String)
            query += $"&cliente={Uri.EscapeDataString(clienteEl.GetString() ?? string.Empty)}";

        var response = await EnviarAsync(HttpMethod.Post,
            $"api/caja/vender-producto?productoId={productoId}&cantidad={cantidad}&{query}", null, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    private async Task<TransaccionResponse> VenderCarritoAsync(TransaccionRequest request, string origen)
    {
        var content = new StringContent(request.Datos.GetRawText(), Encoding.UTF8, "application/json");
        var response = await EnviarAsync(HttpMethod.Post, "api/caja/vender-carrito", content, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    private async Task<TransaccionResponse> RegistrarDonacionAsync(TransaccionRequest request, string origen)
    {
        var datos = request.Datos;
        var cajero = ObtenerTexto(datos, "cajero", "Caja Principal");
        var sucursal = ObtenerTexto(datos, "sucursal", "Sede Central");
        var content = new StringContent(datos.GetRawText(), Encoding.UTF8, "application/json");
        var query = $"cajero={Uri.EscapeDataString(cajero)}&sucursal={Uri.EscapeDataString(sucursal)}";
        var response = await EnviarAsync(HttpMethod.Post, $"api/caja/donacion?{query}", content, origen);
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
}
