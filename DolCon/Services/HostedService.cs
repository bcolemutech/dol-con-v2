namespace DolCon.Services;

using DolCon.Core.Services;
using Microsoft.Extensions.DependencyInjection;
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
            var task = Task.Run(async () =>
            {
                _logger.LogInformation("Application started");
                await _mainMenuService.Show(cancellationToken);
            }, cancellationToken);
            task.Wait(cancellationToken);
            if(task is { IsFaulted: true, Exception: not null })
            {
               throw task.Exception;
            }

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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping application");
        
        var saveService = _serviceProvider.GetService<ISaveGameService>();

        if (saveService != null && SaveGameService.CurrentMap.info is not null) await saveService.SaveGame();
    }
}
