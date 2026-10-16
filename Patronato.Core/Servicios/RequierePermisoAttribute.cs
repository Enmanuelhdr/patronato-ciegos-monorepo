using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Patronato.Core.Servicios
{
    // Autorización granular por permiso: exige un usuario autenticado cuyo token sea
    // del dominio del Core (claim "ctx"="core" — nunca un futuro token de WebUser) y que
    // tenga al menos uno de los códigos indicados entre sus claims "permiso". Convive con
    // los [Authorize(Roles=...)] ya existentes en el resto de controladores (no los reemplaza).
    public class RequierePermisoAttribute : TypeFilterAttribute
    {
        public RequierePermisoAttribute(params string[] codigos) : base(typeof(RequierePermisoFilter))
        {
            Arguments = new object[] { codigos };
        }
    }

    public class RequierePermisoFilter : IAuthorizationFilter
    {
        private readonly string[] _codigos;

        public RequierePermisoFilter(string[] codigos)
        {
            _codigos = codigos;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var usuario = context.HttpContext.User;

            if (usuario?.Identity == null || !usuario.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult("Debe iniciar sesión.");
                return;
            }

            // Llamadas internas legítimas del Gateway o Caja firmadas con clave del sistema
            if (usuario.Identity.AuthenticationType == "GatewayCajaInternal" || usuario.FindFirst("origen")?.Value == "Caja")
            {
                return;
            }

            if (usuario.FindFirst("ctx")?.Value != "core")
            {
                context.Result = new ForbidResult();
                return;
            }

            var permisosDelToken = usuario.FindAll("permiso").Select(c => c.Value).ToHashSet();
            if (_codigos.Length > 0 && !_codigos.Any(permisosDelToken.Contains))
            {
                context.Result = new ObjectResult(new { mensaje = "No tiene permiso para realizar esta acción." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
