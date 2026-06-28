namespace DolCon.Core.Models.World;

using DolCon.Core.Models;

/// <summary>
/// A persisted save game: the canonical <see cref="DolWorld"/> plus the per-playthrough state laid
/// over it (party, active player, and the exploration/discovery progress carried on the world's
/// cells and locations). This is what gets written to a save file — distinct from the shipped,
/// progress-free <c>world.dol</c>.
/// </summary>
public class SaveGame
{
    public DolWorld World { get; set; } = new();
    public Party Party { get; set; } = new();
    public Guid CurrentPlayerId { get; set; }
}
