using DolCon.Core.Data;
using DolCon.Core.Services;
using DolCon.Services;
using DolCon.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GameService = DolCon.Views.GameService;

// Initialize enemy index at startup
EnemyIndex.Initialize();

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Core services
        services.AddSingleton<ISaveGameService, SaveGameService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<IEventService, EventService>();
        services.AddSingleton<IShopService, ShopService>();
        services.AddSingleton<IServicesService, ServicesService>();
        services.AddSingleton<IItemsService, ItemsService>();
        services.AddSingleton<ICombatService, CombatService>();

        // Console-specific services
        services.AddSingleton<IMainMenuService, MainMenuService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IGameService, GameService>();

        // Bridge services (adapt IImageService to IPositionUpdateHandler)
        services.AddSingleton<IPositionUpdateHandler>(sp =>
            new ImageServicePositionHandler(sp.GetRequiredService<IImageService>()));
        services.AddSingleton<IMapService, MapService>();
        services.AddSingleton<IMoveService, MoveService>();

        services.AddHostedService<HostedService>();
    })
    .Build();

await host.RunAsync();

// Adapter to bridge IImageService to IPositionUpdateHandler
internal class ImageServicePositionHandler : IPositionUpdateHandler
{
    private readonly IImageService _imageService;
    public ImageServicePositionHandler(IImageService imageService) => _imageService = imageService;
    public void OnPositionUpdated() => _imageService.ProcessSvg();
}