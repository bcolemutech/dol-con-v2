namespace DolCon.Core.Models.World;

using System.Text.Json.Serialization;
using DolCon.Core.Models;
using Enums;

/// <summary>
/// A baked location placed on a cell or burg. Stores the static identity — the full
/// <c>LocationType</c> template is rehydrated from the <c>LocationTypes.Types</c> catalog via
/// <see cref="TypeKey"/>. Per-playthrough exploration/discovery is runtime/save state: it's omitted
/// from a freshly baked world.dol and only written into save files once the player makes progress.
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

    /// <summary>Whether the player has discovered this location. Runtime/save state.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Discovered { get; set; }

    /// <summary>Per-playthrough exploration progress (0–1). Runtime/save state.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double ExploredPercent { get; set; }

    /// <summary>When the location was last explored. Runtime/save state.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime LastExplored { get; set; }

    /// <summary>The full location template, rehydrated from the static catalog by <see cref="TypeKey"/>.</summary>
    [JsonIgnore]
    public LocationType Type => LocationTypes.Get(TypeKey);
}
