namespace Patronato.Core.Entities
{
    // Tabla puente Rol <-> Permiso (muchos a muchos).
    public class CoreRolePermission
    {
        public int Id { get; set; }

        public int RoleId { get; set; }
        public CoreRole? Role { get; set; }

        public int PermissionId { get; set; }
        public CorePermission? Permission { get; set; }
    }
}
