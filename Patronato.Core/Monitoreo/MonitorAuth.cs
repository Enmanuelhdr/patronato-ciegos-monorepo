namespace Patronato.Core.Monitoreo
{
    // Compuerta compartida del panel de soporte (/monitor y /monitor/operaciones): ya no
    // es una clave compartida (X-Admin-Key) sino el mismo JWT por-usuario que emite
    // POST /monitor/login (mismo formato que api/Usuarios/login, ver JwtService). Dos
    // permisos controlan el acceso: MonitorVer (entrar y auditar) y MonitorGestionar
    // (además crear/editar/eliminar/ajustar). UseAuthentication() ya corre para toda
    // request (ver Program.cs) y deja el ClaimsPrincipal listo en ctx.User aunque estos
    // endpoints Minimal API no tengan [Authorize]/RequireAuthorization().
    public static class MonitorAuth
    {
        public static bool TienePermiso(HttpContext ctx, string permiso) =>
            ctx.User?.Identity?.IsAuthenticated == true &&
            ctx.User.FindFirst("ctx")?.Value == "core" &&
            ctx.User.FindAll("permiso").Any(c => c.Value == permiso);
    }
}
