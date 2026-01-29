namespace DolCon.Core.Models;

public record ShopSelection
{
    public int ItemId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool IsPurchase { get; set; }
    public int Price { get; set; }
    public bool Afford { get; set; } = true;
}