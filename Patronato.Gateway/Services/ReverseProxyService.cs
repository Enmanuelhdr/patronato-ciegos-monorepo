using System.Net.Http.Headers;

namespace Patronato.Gateway.Services;

// Reenvía transparentemente cualquier /api/* que no matcheó ninguna ruta del
// Gateway (ver Program.cs -> MapFallback) hacia el Core. Así la web le habla
// solo al Gateway y nunca directo al Core, igual que la Caja por socket.
public class ReverseProxyService
{
    private readonly HttpClient _httpClient;

    public ReverseProxyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task ProxyAsync(HttpContext context)
    {
        var targetUri = new Uri(_httpClient.BaseAddress!, context.Request.Path + context.Request.QueryString);
        var proxyRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            proxyRequest.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrEmpty(context.Request.ContentType))
                proxyRequest.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
        }

        proxyRequest.Headers.Accept.Clear();
        proxyRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Reenvía el JWT de sesión del usuario de la Web (el Core lo valida con [Authorize]
        // en los endpoints exclusivos de Recepción/Admin). La Caja nunca manda este header.
        if (context.Request.Headers.TryGetValue("Authorization", out var autorizacion) && !string.IsNullOrWhiteSpace(autorizacion))
            proxyRequest.Headers.TryAddWithoutValidation("Authorization", autorizacion.ToString());

        // La Caja también usa este reenvío genérico para sus consultas de solo lectura
        // (login, catálogo, listados); distingue el origen real en vez de asumir "Web".
        var origen = context.Request.Headers.TryGetValue("X-Origen", out var origenHeader) && !string.IsNullOrWhiteSpace(origenHeader)
            ? origenHeader.ToString()
            : "Web";
        proxyRequest.Headers.Add("X-Origen", origen);

        try
        {
            using var coreResponse = await _httpClient.SendAsync(
                proxyRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            context.Response.StatusCode = (int)coreResponse.StatusCode;
            context.Response.ContentType = coreResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

            await coreResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente (navegador o frontend) cerró la pestaña o abortó la petición HTTP
        }
        catch (OperationCanceledException ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "Tiempo de espera agotado comunicándose con el Core",
                    detalle = ex.Message
                }));
            }
        }
        catch (HttpRequestException ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "El servicio Patronato.Core aún no está disponible en http://localhost:57141",
                    detalle = ex.Message
                }));
            }
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "Error al comunicarse con el Core a través del Gateway",
                    detalle = ex.Message
                }));
            }
        }
    }
}
