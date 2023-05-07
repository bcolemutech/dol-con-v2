namespace DolCon.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class HostedService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HostedService> _logger;
    private readonly IMainMenuService _mainMenuService;

    public HostedService(IHostApplicationLifetime appLifetime, IServiceProvider serviceProvider, ILogger<HostedService> logger, IMainMenuService mainMenuService)
    {
        _appLifetime = appLifetime;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mainMenuService = mainMenuService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting application");

        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(() =>
            {
                _logger.LogInformation("Application started");
                _mainMenuService.Show(cancellationToken);
            }, cancellationToken);
        });

        _appLifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation("Application stopping");
        });

        _appLifetime.ApplicationStopped.Register(() =>
        {
            _logger.LogInformation("Application stopped");
        });
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping application");

        return Task.CompletedTask;
    }
}
