namespace Patronato.Core.Servicios
{
    // Catálogo único de códigos de permiso: lo usa DbInitializer para sembrar
    // CorePermission/CoreRolePermission y los controladores vía [RequierePermiso(...)]
    // para no repetir strings sueltos por todo el código.
    public static class PermisosCore
    {
        public const string CoreUsersVer = "core-users.ver";
        public const string CoreUsersCrear = "core-users.crear";
        public const string CoreUsersEditar = "core-users.editar";
        public const string CoreUsersEstado = "core-users.estado";
        public const string CoreUsersCrearSuperAdmin = "core-users.crear-superadmin";

        public const string CoreRolesVer = "core-roles.ver";

        public const string InventarioVer = "inventario.ver";
        public const string InventarioCrear = "inventario.crear";
        public const string InventarioEditar = "inventario.editar";
        public const string InventarioMovimientos = "inventario.movimientos";

        public const string WebUsuariosVer = "web-usuarios.ver";
        public const string WebUsuariosGestionar = "web-usuarios.gestionar";

        // Panel de soporte /monitor y /monitor/operaciones: acceso directo al Core, fuera
        // del Gateway. Ya no se guarda con una clave compartida (X-Admin-Key) sino con
        // login real (mismo Usuario/CoreRole) + estos dos permisos: Ver = auditar/solo
        // lectura, Gestionar = además crear/editar/eliminar/ajustar.
        public const string MonitorVer = "monitor.ver";
        public const string MonitorGestionar = "monitor.gestionar";

        public static readonly (string Codigo, string Modulo, string Descripcion)[] Catalogo = new[]
        {
            (CoreUsersVer, "Usuarios del Core", "Ver el listado de empleados del Core"),
            (CoreUsersCrear, "Usuarios del Core", "Registrar nuevos empleados"),
            (CoreUsersEditar, "Usuarios del Core", "Editar datos, rol y estado de un empleado"),
            (CoreUsersEstado, "Usuarios del Core", "Activar, suspender o revocar el acceso de un empleado"),
            (CoreUsersCrearSuperAdmin, "Usuarios del Core", "Crear o ascender empleados al rol Administrador (SuperAdmin/IT)"),

            (CoreRolesVer, "Roles", "Ver el catálogo de roles y sus permisos"),

            (InventarioVer, "Inventario", "Ver el catálogo, filtros y bitácora de inventario"),
            (InventarioCrear, "Inventario", "Dar de alta nuevos productos"),
            (InventarioEditar, "Inventario", "Editar catálogo (nombre, categoría, precio, stock mínimo, estado)"),
            (InventarioMovimientos, "Inventario", "Registrar entradas, salidas y ajustes de stock"),

            (WebUsuariosVer, "Accesos Web", "Ver el listado e historial de usuarios de la plataforma web"),
            (WebUsuariosGestionar, "Accesos Web", "Habilitar, deshabilitar, resetear credenciales o forzar cierre de sesión"),

            (MonitorVer, "Panel de Soporte", "Entrar a /monitor y /monitor/operaciones y auditar su contenido (solo lectura)"),
            (MonitorGestionar, "Panel de Soporte", "Crear, editar, eliminar o ajustar cualquier cosa desde el panel de soporte"),
        };

        // Roles nuevos (los clínicos/operativos ya existentes -Cajero, MedicoOftalmologo,
        // InstructorRehabilitacion, Recepcionista- se siembran sin permisos nuevos: su
        // acceso sigue viniendo de los [Authorize(Roles=)] ya existentes en cada controlador).
        public const string RolAdministrador = "Administrador"; // = SuperAdmin / IT (nombre sin cambiar, ver Caja/Gateway)
        public const string RolAdminOperativo = "AdminOperativo";
        public const string RolAuditor = "Auditor";
        public const string RolOperadorLogistico = "OperadorLogistico";

        public static readonly string[] PermisosAdministrador = new[]
        {
            CoreUsersVer, CoreUsersCrear, CoreUsersEditar, CoreUsersEstado, CoreUsersCrearSuperAdmin,
            CoreRolesVer,
            InventarioVer, InventarioCrear, InventarioEditar, InventarioMovimientos,
            WebUsuariosVer, WebUsuariosGestionar,
            MonitorVer, MonitorGestionar,
        };

        public static readonly string[] PermisosAdminOperativo = new[]
        {
            InventarioVer, InventarioCrear, InventarioEditar, InventarioMovimientos,
            WebUsuariosVer, WebUsuariosGestionar,
        };

        // Solo lectura: todos los .ver, ningún permiso de escritura/gestión.
        public static readonly string[] PermisosAuditor = new[]
        {
            CoreUsersVer, CoreRolesVer, InventarioVer, WebUsuariosVer, MonitorVer,
        };

        public static readonly string[] PermisosOperadorLogistico = new[]
        {
            InventarioVer, InventarioMovimientos,
        };

        public static readonly string[] PermisosRecepcionista = new[]
        {
            InventarioVer,
        };
    }
}
