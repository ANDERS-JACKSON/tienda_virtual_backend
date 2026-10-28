using TiendaVirtual.Dominio.Servicios.VentaXqm;

namespace TiendaVirtual.Api.Workers
{
    /// <summary>
    /// Libera stock de pedidos que quedaron en PendientePago (pago abandonado).
    /// Antes de cancelar consulta a la pasarela por si el cobro sí se acreditó.
    /// </summary>
    public sealed class OrdenesPendientePagoWorker : BackgroundService
    {
        private static readonly TimeSpan RetrasoInicial = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrdenesPendientePagoWorker> _logger;

        public OrdenesPendientePagoWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<OrdenesPendientePagoWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(RetrasoInicial, stoppingToken);

                using var timer = new PeriodicTimer(Intervalo);
                do
                {
                    await EjecutarCicloAsync();
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task EjecutarCicloAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var pagos = scope.ServiceProvider.GetRequiredService<IOrdenPagoServicio>();
                var cerradas = await pagos.ReconciliarPagosPendientesAsync();
                if (cerradas > 0)
                {
                    _logger.LogInformation(
                        "Reconciliación de pedidos PendientePago: {Cerradas} cerrados.",
                        cerradas);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de pedidos PendientePago.");
            }
        }
    }
}
