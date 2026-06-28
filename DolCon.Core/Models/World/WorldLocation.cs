namespace DolCon.Core.Models.World;

using Enums;

/// <summary>
/// A baked location definition placed on a cell or burg. Stores only the static identity — the
/// full <c>LocationType</c> template is rehydrated at load time from the <c>LocationTypes.Types</c>
/// catalog via <see cref="TypeKey"/>. Per-playthrough exploration/discovery state is NOT stored
/// here; it lives in the save game.
/// </summary>
public class WorldLocation
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>
    /// Key into the static <c>LocationTypes.Types</c> catalog (the unique <c>LocationType.Type</c>,
    /// e.g. <c>"tavern"</c>). Keeps the world file slim instead of embedding the whole template.
    /// </summary>
    public string TypeKey { get; set; } = null!;

    /// <summary>The rolled instance rarity (within the type's rarity range), so it must be persisted.</summary>
    public Rarity Rarity { get; set; }

    /// <summary>Reserved enrichment for this location. Optional; omitted when null.</summary>
    public Enrichment? Enrichment { get; set; }
}
