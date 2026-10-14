using System.Collections.Generic;

namespace Patronato.Core.Entities
{
    // Rol de un empleado del Core. Reemplaza al viejo enum RolUsuario como fuente de
    // verdad: los permisos de cada rol viven en CoreRolePermission, no en código.
    // Los nombres de los roles ya existentes (Administrador, Cajero, MedicoOftalmologo,
    // InstructorRehabilitacion, Recepcionista) NO se renombran: Patronato.Gateway y
    // Patronato.Caja comparan el string "rol" de la respuesta de login contra
    // "Administrador"/"Cajero" literalmente (ver ApiService.LoginAsync en la Caja).
    public class CoreRole
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        // Rol sembrado por el sistema: no se puede eliminar desde la UI.
        public bool EsSistema { get; set; } = true;

        public List<CoreRolePermission> Permisos { get; set; } = new();
    }
}
