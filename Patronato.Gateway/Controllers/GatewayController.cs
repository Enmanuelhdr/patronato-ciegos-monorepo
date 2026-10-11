using Microsoft.AspNetCore.Mvc;
using Patronato.Gateway.Protocol;
using Patronato.Gateway.Services;

namespace Patronato.Gateway.Controllers;

// Entrada REST usada por la página web. Usa el mismo sobre transaccional
// que la entrada por socket de la Caja (ver Sockets/CajaSocketServer).
[ApiController]
[Route("api/gateway")]
public class GatewayController : ControllerBase
{
    private readonly CoreGatewayService _service;

    public GatewayController(CoreGatewayService service)
    {
        _service = service;
    }

    [HttpPost("transaccion")]
    public async Task<ActionResult<TransaccionResponse>> Procesar(TransaccionRequest request)
    {
        var respuesta = await _service.ProcesarAsync(request, "Web");
        return Ok(respuesta);
    }
}
