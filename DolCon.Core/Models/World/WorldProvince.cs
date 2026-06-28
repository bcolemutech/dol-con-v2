namespace DolCon.Core.Models.World;

/// <summary>
/// A province. <see cref="FullName"/> is the field the navigation UI displays; the rest of Azgaar's
/// province data (heraldry, color, centers) is dropped.
/// </summary>
public class WorldProvince
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string FullName { get; set; } = null!;
}
