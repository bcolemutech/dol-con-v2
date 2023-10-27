using DolCon.Enums;
using DolCon.Models;

namespace DolCon.Services;

public interface IShopService
{
    Scene ProcessShop(Scene scene);
}

public class ShopService : IShopService
{
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
            scene.Selections = new Dictionary<int, string>();
            var i = 1;
            var typeServices = scene.Location?.Type.Services;

            foreach (var s in typeServices)
            {
                scene.Selections.Add(i, s.ToString());
                i++;
            }
            
            scene.Selections.Add(i, "Leave");
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
        
        if (service == null)
        {
            scene.SelectedService = Enum.Parse<Service>(scene.Selections[selection]);
            scene.Title = $"[bold black on white]{scene.SelectedService}[/]";
            scene.Description = "Select purchase.";
            scene.Selections = new Dictionary<int, string>();
            var i = 1;
            // TODO: Add items to selection
            scene.Selections.Add(i, "Leave|0");
            scene.Selection = 0;
            scene.Message = "Select an option from the menu.";
            return scene;
        }
        
        var item = scene.Selections[selection].Split('|');
        
        var price = int.Parse(item[1]);
        var playerMoney = SaveGameService.Party.Players[0].coin;

        if (playerMoney < price)
        {
            scene.Message = "[red]You don't have enough money.[/]";
            return scene;
        }

        SaveGameService.Party.Players[0].coin -= price;
        // TODO: Add item to inventory
        // Calculate money break down
        var copper = price % 10;
        var silver = (price / 10) % 10;
        var gold = (price / 100) % 10;
        scene.Message = $"You bought {item[0]} for {gold} gold, {silver} silver, and {copper} copper.";

        return scene;
    }
}