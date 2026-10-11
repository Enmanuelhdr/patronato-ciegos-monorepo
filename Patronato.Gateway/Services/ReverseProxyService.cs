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
        proxyRequest.Headers.Add("X-Origen", "Web");

        using var coreResponse = await _httpClient.SendAsync(
            proxyRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

        context.Response.StatusCode = (int)coreResponse.StatusCode;
        context.Response.ContentType = coreResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

        await coreResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
