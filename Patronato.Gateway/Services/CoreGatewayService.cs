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
        var id = request.Datos.GetProperty("id").GetInt32();
        var response = await EnviarAsync(HttpMethod.Post, $"api/caja/cobrar-masaje/{id}", null, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    private async Task<TransaccionResponse> AbonarPrestamoAsync(TransaccionRequest request, string origen)
    {
        var prestamoId = request.Datos.GetProperty("prestamoId").GetInt32();
        var monto = request.Datos.GetProperty("monto").GetDecimal();
        var response = await EnviarAsync(HttpMethod.Post,
            $"api/caja/abonar-prestamo?prestamoId={prestamoId}&monto={monto}", null, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    private async Task<TransaccionResponse> VenderProductoAsync(TransaccionRequest request, string origen)
    {
        var productoId = request.Datos.GetProperty("productoId").GetInt32();
        var cantidad = request.Datos.GetProperty("cantidad").GetInt32();
        var response = await EnviarAsync(HttpMethod.Post,
            $"api/caja/vender-producto?productoId={productoId}&cantidad={cantidad}", null, origen);
        return await TraducirRespuestaAsync(request, response);
    }

    private async Task<TransaccionResponse> RegistrarDonacionAsync(TransaccionRequest request, string origen)
    {
        var content = new StringContent(request.Datos.GetRawText(), Encoding.UTF8, "application/json");
        var response = await EnviarAsync(HttpMethod.Post, "api/caja/donacion", content, origen);
        return await TraducirRespuestaAsync(request, response);
    }

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
