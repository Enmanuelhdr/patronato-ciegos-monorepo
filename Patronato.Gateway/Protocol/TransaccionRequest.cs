using System.Text.Json;

namespace Patronato.Gateway.Protocol;

// Formato inspirado en ISO 8583 (protocolo de cajeros automáticos): MTI + STAN
// para casar petición/respuesta, y un código de transacción con sus datos.
public class TransaccionRequest
{
    public string Mti { get; set; } = "0200";
    public string Stan { get; set; } = "";
    public string CodigoTransaccion { get; set; } = "";
    public JsonElement Datos { get; set; }
}
