namespace Patronato.Gateway.Services;

// Cuando el Core vuelve a responder tras una caída, reenvía en orden todo lo que quedó
// encolado mientras estuvo caído (ver PendingQueueStore y CoreGatewayService.EnviarCobroAsync).
// Simétrico al ciclo de sincronización que corre dentro de la Caja para su propia cola
// hacia el Gateway.
public class CoreSyncWorker : BackgroundService
{
    private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(CoreSyncWorker));
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PendingQueueStore _cola;

    public CoreSyncWorker(IServiceScopeFactory scopeFactory, PendingQueueStore cola)
    {
        _scopeFactory = scopeFactory;
        _cola = cola;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                await IntentarVaciarColaAsync();
            }
            catch (OperationCanceledException)
            {
                // Apagado normal del host.
            }
        }
    }

    private async Task IntentarVaciarColaAsync()
    {
        if (_cola.Cantidad == 0) return;

        while (_cola.Cantidad > 0)
        {
            var pendiente = _cola.ObtenerTodos()[0];

            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<CoreGatewayService>();

            bool exito = await service.ReenviarPendienteAsync(pendiente);
            if (!exito)
            {
                // El Core sigue sin responder: se deja en cola y se reintenta en el próximo ciclo.
                break;
            }

            _cola.QuitarPrimero();
            Logger.Info($"Sincronizada con el Core la transacción {pendiente.CodigoTransaccion} STAN={pendiente.Stan} (quedan {_cola.Cantidad} pendiente(s)).");
        }
    }
}
