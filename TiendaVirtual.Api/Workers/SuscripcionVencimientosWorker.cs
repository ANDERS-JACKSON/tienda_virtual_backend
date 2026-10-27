using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;

namespace TiendaVirtual.Api.Workers
{
    /// <summary>
    /// Mantiene al día los estados de suscripción y envía los avisos de vencimiento.
    /// Ambas operaciones son idempotentes, así que repetir un ciclo no duplica efectos.
    /// </summary>
    public sealed class SuscripcionVencimientosWorker : BackgroundService
    {
        private static readonly TimeSpan RetrasoInicial = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Intervalo = TimeSpan.FromHours(6);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SuscripcionVencimientosWorker> _logger;

        public SuscripcionVencimientosWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SuscripcionVencimientosWorker> logger)
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
                // Apagado normal del host.
            }
        }

        private async Task EjecutarCicloAsync()
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var servicio = scope.ServiceProvider.GetRequiredService<ISuscripcionServicio>();

                var vencimientos = await servicio.ProcesarVencimientosAsync();
                var avisos = await servicio.EnviarAvisosVencimientoAsync();

                _logger.LogInformation(
                    "Ciclo de suscripciones: {Vencimientos} cambios de estado, {Avisos} avisos de vencimiento.",
                    vencimientos.Datos, avisos.Datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de vencimientos de suscripciones.");
            }
        }
    }
}
