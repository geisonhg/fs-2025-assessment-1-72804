using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DublinBikes.Api.Services
{
    public class StationUpdateBackgroundService : BackgroundService
    {
        private readonly IStationService _stationService;
        private readonly ILogger<StationUpdateBackgroundService> _logger;

        public StationUpdateBackgroundService(
            IStationService stationService,
            ILogger<StationUpdateBackgroundService> logger)
        {
            _stationService = stationService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StationUpdateBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _stationService.RandomUpdateAllStations();
                    _logger.LogInformation("Stations updated at: {time}", DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating stations.");
                }

                // Espera 15 segundos antes del siguiente update
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }

            _logger.LogInformation("StationUpdateBackgroundService stopping.");
        }
    }
}
