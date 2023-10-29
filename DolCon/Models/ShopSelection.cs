namespace DolCon.Models;

public record ShopSelection
{
    public int ItemId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsPurchase { get; set; }
    public int Price { get; set; }
    public bool Afford { get; set; }
}