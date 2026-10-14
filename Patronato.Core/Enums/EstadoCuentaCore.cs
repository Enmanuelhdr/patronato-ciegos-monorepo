namespace Patronato.Core.Enums
{
    // Estado de la cuenta de un Usuario del Core (empleado). Reemplaza el bool Activo
    // plano para poder distinguir "suspendida temporalmente" de "revocada".
    public enum EstadoCuentaCore
    {
        Activo = 1,
        Suspendido = 2,
        Revocado = 3
    }
}
