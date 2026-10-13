namespace Patronato.Core.Monitoreo
{
    // Compuerta compartida del panel de soporte (/monitor y /monitor/operaciones):
    // acceso directo al Core, fuera de /api, protegido con la misma X-Admin-Key
    // que ya usa el Gateway (ningún secreto nuevo).
    public static class AdminAuth
    {
        public static bool EsValida(HttpContext ctx, string? claveAdmin) =>
            !string.IsNullOrEmpty(claveAdmin) &&
            ctx.Request.Headers.TryGetValue("X-Admin-Key", out var clave) &&
            clave == claveAdmin;
    }
}
