using DolCon.Services;
using DolCon.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GameService = DolCon.Views.GameService;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<ISaveGameService, SaveGameService>();
        services.AddSingleton<IMainMenuService, MainMenuService>();
        services.AddSingleton<IMapService, MapService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<IGameService, GameService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IMoveService, MoveService>();
        services.AddSingleton<IEventService, EventService>();
        services.AddHostedService<HostedService>();
    })
    .Build();
    
await host.RunAsync();