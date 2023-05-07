﻿namespace DolCon;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class MainMenu : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainMenu> _logger;

    public MainMenu(IHostApplicationLifetime appLifetime, IServiceProvider serviceProvider, ILogger<MainMenu> logger)
    {
        _appLifetime = appLifetime;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting application");

        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                await Task.Delay(1000, cancellationToken);
                _logger.LogInformation("Application started");
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
