using ChanceNET;
using DolCon.Enums;
using DolCon.Models;

namespace DolCon.Services;

public interface IShopService
{
    Scene ProcessShop(Scene scene);
    Item GenerateReward();
}

public class ShopService : IShopService
{
    private readonly IServicesService _servicesService;
    private readonly IMoveService _moveService;
    private readonly IItemsService _itemsService;

    public ShopService(IServicesService servicesService, IMoveService moveService, IItemsService itemsService)
    {
        _servicesService = servicesService;
        _moveService = moveService;
        _itemsService = itemsService;
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
                scene.Selections.Add(i, new ShopSelection { Name = s.ToString() });
                i++;
            }
            scene.Message = "Select an option from the menu.";
            return scene;
        }

        var sceneSelection = scene.Selections[selection];
        if (service == null)
        {
            scene.SelectedService = Enum.Parse<ServiceType>(sceneSelection.Name);
            scene.Title = $"[bold black on white]{scene.SelectedService}[/]";
            scene.Description = "Select purchase.";
            scene.Selections = GetServiceSelections(scene);
            scene.Selection = 0;
            scene.Message = "Select an option from the menu.";
            return scene;
        }

        scene.Message = ProcessPurchase(scene, sceneSelection);

        return scene;
    }

    public Item GenerateReward()
    {
        var random = new Chance();
        var roll = random.Dice(100);
        var rarity = roll switch
        {
            < 50 => Rarity.Common,
            < 75 => Rarity.Uncommon,
            < 90 => Rarity.Rare,
            < 99 => Rarity.Epic,
            _ => Rarity.Legendary
        };
        var items = _itemsService.GenerateItems(rarity, "Gem").ToArray();
        return items[random.Dice(items.Length) - 1];
    }

    private string ProcessPurchase(Scene scene, ShopSelection selection)
    {
        var playerMoney = SaveGameService.Party.Players[0].coin;
        var locationRarity = scene.Location?.Rarity ?? Rarity.Common;
        var copper = selection.Price % 10;
        var silver = selection.Price / 10 % 10;
        var gold = selection.Price / 100;
        var message = $"You bought a {selection.Name} for {gold} gold, {silver} silver, and {copper} copper.";
        if (playerMoney < selection.Price)
        {
            return "[red]You don't have enough money.[/]";
        }

        if (scene.SelectedService == ServiceType.Lodging)
        {
            var services = _servicesService.GetServices(ServiceType.Lodging, locationRarity);
            var service = services.FirstOrDefault(s => s.Name == selection.Name);
            if (service == null) return "Something went wrong.";
            var slept = _moveService.Sleep(service.Rarity);
            if (!slept) return "You are not tired enough to sleep here.";
            message =  $"{message}\nYou slept at {scene.Location?.Name} at {service.Rarity} quality.";
        }
        
        if (scene.SelectedService == ServiceType.Sell)
        {
            var player = SaveGameService.Party.Players[0];
            var inventory = player.Inventory;
            var item = inventory[selection.ItemId];
            inventory.RemoveAt(selection.ItemId);

            // Rebuild selections with updated inventory
            scene.Selections = GetServiceSelections(scene);
            scene.Selection = 0;

            message = $"You sold {item.Name} for [bold gold1]{gold}[/]|[bold silver]{silver}[/]|[bold tan]{copper}[/].";

        }
        
        if (scene.SelectedService == ServiceType.Buy)
        {
            var goods = scene.Location?.Type.Goods;
            var items = _itemsService.GetItems(goods, locationRarity);
            var item = items.FirstOrDefault(i => i.Name == selection.Name);
            if (item == null) return "Something went wrong.";
            SaveGameService.Party.Players[0].Inventory.Add(item);
        }

        SaveGameService.Party.Players[0].coin -= selection.Price;

        return message;
    }

    private Dictionary<int, ShopSelection> GetServiceSelections(Scene scene)
    {
        var playersCoin = SaveGameService.Party.Players[0].coin;
        var locationRarity = (scene.Location?.Rarity ?? Rarity.Common);
        var selections = new Dictionary<int, ShopSelection>();
        var i = 1;

        switch (scene.SelectedService)
        {
            case ServiceType.Lodging:
            {
                var services = _servicesService.GetServices(ServiceType.Lodging, locationRarity);
                foreach (var service in services)
                {
                    selections.Add(i,
                        new ShopSelection
                            { Name = service.Name, Price = service.Price, Afford = service.Price <= playersCoin });
                    i++;
                }

                break;
            }
            case ServiceType.Sell:
            {
                var player = SaveGameService.Party.Players[0];
                var inventory = player.Inventory;
                var itemIndex = 0;
                foreach (var item in inventory)
                {
                    var price = (item.Price / 2) * -1;
                    selections.Add(i, new ShopSelection { Name = item.Name, Price = price, Afford = true, ItemId = itemIndex });
                    i++;
                    itemIndex++;
                }

                break;
            }
            case ServiceType.Buy:
            {
                var goods = scene.Location?.Type.Goods;
                var items = _itemsService.GetItems(goods, locationRarity);
                foreach (var item in items)
                {
                    selections.Add(i,
                        new ShopSelection
                            { Name = item.Name, Price = item.Price, Afford = item.Price <= playersCoin });
                    i++;
                }

                break;
            }
        }

        return selections;
    }
}