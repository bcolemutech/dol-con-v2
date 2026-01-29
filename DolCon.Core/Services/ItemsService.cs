using System.Reflection;
using System.Text.Json;
using DolCon.Core.Enums;
using DolCon.Core.Models;

namespace DolCon.Core.Services;

public interface IItemsService
{
    IEnumerable<Item> GenerateItems(Rarity rarity, string type);
    IEnumerable<Item> GetItems(string[]? goods, Rarity rarity);
}

public class ItemsService : IItemsService
{
    private readonly List<Item> _items;

    public ItemsService()
    {
        const string itemsResourceName = "DolCon.Core.Resources.Items.json";
        var executingAssembly = Assembly.GetExecutingAssembly();
        var jsonStream = executingAssembly.GetManifestResourceStream(itemsResourceName);
        using var reader = new StreamReader(jsonStream ?? throw new InvalidOperationException());
        var json = reader.ReadToEnd();
        _items = JsonSerializer.Deserialize<List<Item>>(json) ?? new List<Item>();
    }

    public IEnumerable<Item> GenerateItems(Rarity rarity, string type) =>
        _items.Where(i => i.Rarity == rarity && i.Tags.Any(x => x.Name == type && x.type == TagType.Good));

    public IEnumerable<Item> GetItems(string[]? goods, Rarity rarity) =>
        goods is null
            ? new List<Item>()
            : _items.Where(i =>
                i.Rarity <= rarity && i.Tags.Any(x => x.type == TagType.Good && goods.Contains(x.Name)));
}
