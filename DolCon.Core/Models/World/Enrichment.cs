namespace DolCon.Core.Models.World;

/// <summary>
/// Authoring/worklist state of an <see cref="Enrichment"/> block. Driven by the Phase 3 (#76)
/// flag/todo system. <see cref="Empty"/> means nothing has been authored yet.
/// </summary>
public enum EnrichmentStatus
{
    Empty,
    Draft,
    Authored,
    Reviewed
}

/// <summary>
/// Reserved, AI-friendly container for authored content attached to a world object
/// (world / cell / burg / location). Empty by default so a world can be baked with no authored
/// content. Phases 3–5 (#76–#78) populate these. The whole block is optional (nullable) on every
/// object and omitted from JSON when null.
/// </summary>
public class Enrichment
{
    public EnrichmentStatus Status { get; set; } = EnrichmentStatus.Empty;
    public List<Npc> Npcs { get; set; } = new();
    public List<HistoryEvent> History { get; set; } = new();
    public List<PointOfInterest> PointsOfInterest { get; set; } = new();
    public List<AssetRef> Assets { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>Minimal NPC placeholder; expanded in Phase 4 (#77).</summary>
public class Npc
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Role { get; set; }
    public string? Description { get; set; }
}

/// <summary>Minimal history-event placeholder; expanded in Phase 4 (#77).</summary>
public class HistoryEvent
{
    public string Title { get; set; } = null!;
    public string? Era { get; set; }
    public string? Description { get; set; }
}

/// <summary>Minimal point-of-interest placeholder; expanded in Phase 4 (#77).</summary>
public class PointOfInterest
{
    public string Name { get; set; } = null!;
    public string? Kind { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Reference to an external visual asset. The AI authors the <see cref="Spec"/> (prompt/spec);
/// the actual art is procured externally and its <see cref="Path"/> filled in later (Phase 5, #78).
/// </summary>
public class AssetRef
{
    public string Key { get; set; } = null!;
    public string? Kind { get; set; }
    public string? Spec { get; set; }
    public string? Path { get; set; }
    public EnrichmentStatus Status { get; set; } = EnrichmentStatus.Empty;
}
