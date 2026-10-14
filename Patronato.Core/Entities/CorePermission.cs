namespace Patronato.Core.Entities
{
    // Catálogo de permisos granulares. El código (ej. "inventario.ver") es lo que
    // viaja como claim "permiso" dentro del JWT y lo que revisa [RequierePermiso].
    public class CorePermission
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}
