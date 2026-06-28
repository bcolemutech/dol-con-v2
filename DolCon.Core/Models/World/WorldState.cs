namespace DolCon.Core.Models.World;

/// <summary>
/// A nation/state. Minimal — the game only ever surfaces names — but kept indexable for display
/// and future use. The bulk of Azgaar's state data (military, campaigns, diplomacy, heraldry) is dropped.
/// </summary>
public class WorldState
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? FullName { get; set; }
}
