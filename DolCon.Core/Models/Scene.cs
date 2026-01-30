namespace DolCon.Core.Models;

using Combat;
using Enums;

public class Scene
{
    public MoveStatus MoveStatus { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = true;
    public SceneType Type { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Dictionary<int, ShopSelection> Selections { get; set; } = new();
    public ServiceType? SelectedService { get; set; }
    public int Selection { get; set; }
    public Location? Location { get; set; }

    // Combat-specific properties
    public CombatState? CombatState { get; set; }
    public double EncounterCR { get; set; }

    // Pending exploration (only commit on victory, discard on defeat/flee)
    public double PendingExplorationProgress { get; set; }
    public bool HasPendingExploration { get; set; }
    public bool IsLocationExploration { get; set; }  // true = location, false = cell

    public void Reset()
    {
        Title = null;
        Description = null;
        Selections = new Dictionary<int, ShopSelection>();
        SelectedService = null;
        Selection = 0;
        Location = null;
        CombatState = null;
        EncounterCR = 0;
        PendingExplorationProgress = 0;
        HasPendingExploration = false;
        IsLocationExploration = false;
    }
}