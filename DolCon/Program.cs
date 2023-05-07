using DolCon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<ISaveGameService, SaveGameService>();
        services.AddHostedService<MainMenu>();
    })
    .Build();
    
await host.RunAsync();
