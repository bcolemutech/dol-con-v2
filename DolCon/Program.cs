using DolCon.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<ISaveGameService, SaveGameService>();
        services.AddSingleton<IMainMenuService, MainMenuService>();
        services.AddSingleton<IMapService, MapService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddHostedService<HostedService>();
    })
    .Build();
    
await host.RunAsync();
