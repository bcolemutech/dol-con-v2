namespace DolCon.Models;

public class Item
{
    public string description { get; set; }
    public List<Tag> tags { get; set; }
    public int price { get; set; }
}
