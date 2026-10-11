namespace Patronato.Gateway.Protocol;

public class TransaccionResponse
{
    public string Mti { get; set; } = "0210";
    public string Stan { get; set; } = "";

    // "00" = aprobada, "05" = rechazada por el Core, "12" = transacción no soportada,
    // "96" = error del sistema/gateway. Mismos códigos que usa ISO 8583.
    public string CodigoRespuesta { get; set; } = "96";
    public string Mensaje { get; set; } = "";
    public object? Datos { get; set; }
}
