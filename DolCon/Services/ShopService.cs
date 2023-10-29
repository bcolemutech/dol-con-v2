using DolCon.Enums;
using DolCon.Models;

namespace DolCon.Services;

public interface IShopService
{
    Scene ProcessShop(Scene scene);
}

public class ShopService : IShopService
{
    private readonly IServicesService _servicesService;

    public ShopService(IServicesService servicesService)
    {
        _servicesService = servicesService;
    }

    public Scene ProcessShop(Scene scene)
    {
        if (scene.Type != SceneType.Shop)
        {
            return scene;
        }

        var selection = scene.Selection;
        var service = scene.SelectedService;

        if (scene.Selections.Count == 0)
        {
            scene.IsCompleted = false;
            scene.Title = $"[bold black on white]Welcome to {scene.Location?.Name}[/]";
            scene.Description = "Select a service.";
            scene.Selections = new Dictionary<int, ShopSelection>();
            var i = 1;
            var typeServices = new List<ServiceType>();

            if (scene.Location?.Type.Goods is { Length: > 0 })
            {
                typeServices.Add(ServiceType.Buy);
                typeServices.Add(ServiceType.Sell);
            }

            if (scene.Location is { Type.Services.Length: > 0 })
                typeServices.AddRange(scene.Location?.Type.Services);

            foreach (var s in typeServices)
            {
                scene.Selections.Add(i, new ShopSelection{Name = s.ToString()});
                i++;
            }

            scene.Selections.Add(i, new ShopSelection{Name = "Leave"});
            scene.Message = "Select an option from the menu.";
            return scene;
        }

        if (selection == scene.Selections.Count)
        {
            scene.IsCompleted = true;
            scene.Message = "You left the shop.";
            scene.Reset();
            return scene;
        }

        var sceneSelection = scene.Selections[selection];
        if (service == null)
        {
            scene.SelectedService = Enum.Parse<ServiceType>(sceneSelection.Name);
            scene.Title = $"[bold black on white]{scene.SelectedService}[/]";
            scene.Description = "Select purchase.";
            scene.Selections = GetServiceSelections(scene);
                var nextId = scene.Selections.Count + 1;
            scene.Selections.Add(nextId, new ShopSelection{Name = "Back"});
            scene.Selection = 0;
            scene.Message = "Select an option from the menu.";
            return scene;
        }

        var price = sceneSelection.Price;
        
        scene.Message = ProcessPurchase(scene, price, sceneSelection);

        return scene;
    }

    private static string ProcessPurchase(Scene scene, int price, ShopSelection selection)
    {
        var playerMoney = SaveGameService.Party.Players[0].coin;
        if (playerMoney < price)
        {
            return "[red]You don't have enough money.[/]";
        }
        
        SaveGameService.Party.Players[0].coin -= price;
        // Calculate money break down
        var copper = price % 10;
        var silver = price / 10 % 100;
        var gold = price / 1000;
        
        // TODO: Evaluate effect of purchase
        
        return $"You bought a {selection.Name} for {gold} gold, {silver} silver, and {copper} copper.";
    }

    private Dictionary<int,ShopSelection> GetServiceSelections(Scene scene)
    {
        var playersCoin = SaveGameService.Party.Players[0].coin;
        var locationRarity = (scene.Location?.Rarity ?? Rarity.Common);
        var selections = new Dictionary<int, ShopSelection>();
        var i = 1;

        if (scene.SelectedService == ServiceType.Lodging)
        {
            var services = _servicesService.GetServices(ServiceType.Lodging, locationRarity);
            foreach (var service in services)
            {
                selections.Add(i, new ShopSelection{Name = service.Name, Price = service.Price, Afford = service.Price <= playersCoin});
                i++;
            }
        }

        return selections;
    }
}