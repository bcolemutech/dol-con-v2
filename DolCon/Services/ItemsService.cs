using System.Reflection;
using System.Text.Json;
using DolCon.Enums;
using DolCon.Models;

namespace DolCon.Services;

public interface IItemsService
{
    IEnumerable<Item> GenerateItems(Rarity rarity, TagType type);
}

public class ItemsService : IItemsService
{
    private readonly List<Item> _items;

    public ItemsService()
    {
        const string itemsResourceName = "DolCon.Resources.Items.json";
        var executingAssembly = Assembly.GetExecutingAssembly();
        var jsonStream = executingAssembly.GetManifestResourceStream(itemsResourceName);
        using var reader = new StreamReader(jsonStream ?? throw new InvalidOperationException());
        var json = reader.ReadToEnd();
        _items = JsonSerializer.Deserialize<List<Item>>(json) ?? new List<Item>();
    }

    public IEnumerable<Item> GenerateItems(Rarity rarity, TagType type)
    {
        return _items.Where(i => i.Rarity == rarity && i.Tags.Any(x => x.type == type));
    }
}
