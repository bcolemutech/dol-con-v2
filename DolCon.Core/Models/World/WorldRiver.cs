namespace DolCon.Core.Models.World;

/// <summary>
/// A river. Not read by current game logic, but retained (slimmed) per the issue scope for future
/// map rendering. The Azgaar discharge/basin/widthFactor fields are dropped.
/// </summary>
public class WorldRiver
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>Cell ids the river flows through.</summary>
    public List<int> Cells { get; set; } = new();

    public double Width { get; set; }
    public double Length { get; set; }
    public int Source { get; set; }
    public int Mouth { get; set; }
}
