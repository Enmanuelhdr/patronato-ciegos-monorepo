using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Servicios;

namespace Patronato.Core.Controllers
{
    public record CoreRoleDto(int Id, string Nombre, string Descripcion, bool EsSistema);

    // Solo lectura: alimenta el selector de rol de la pantalla de Usuarios del Core.
    [ApiController]
    [Route("api/[controller]")]
    public class CoreRolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CoreRolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [RequierePermiso(PermisosCore.CoreRolesVer)]
        public async Task<ActionResult<IEnumerable<CoreRoleDto>>> GetRoles()
        {
            var roles = await _context.CoreRoles
                .OrderBy(r => r.Nombre)
                .Select(r => new CoreRoleDto(r.Id, r.Nombre, r.Descripcion, r.EsSistema))
                .ToListAsync();

            return Ok(roles);
        }
    }
}
