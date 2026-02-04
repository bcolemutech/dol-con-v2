using ChanceNET;
using DolCon.Core.Enums;
using DolCon.Core.Models;

namespace DolCon.Core.Services;

public interface IEventService
{
    Scene ProcessEvent(Event thisEvent);
}

public class EventService : IEventService
{
    private readonly IShopService _shopService;

    public EventService(IShopService shopService)
    {
        _shopService = shopService;
    }

    public Scene ProcessEvent(Event thisEvent)
    {
        var scene = new Scene();
        switch (thisEvent.Location)
        {
            case { Type.Size: LocationSize.unexplorable }:
                return ProcessServices(thisEvent, scene);
            case { ExploredPercent: < 1 }:
            case null when thisEvent.Area is { ExploredPercent: < 1 }:
            {
                // Calculate pending exploration (don't apply yet)
                scene.MoveStatus = CalculatePendingExploration(scene);
                if (scene.MoveStatus == MoveStatus.Success)
                {
                    // Get challenge rating from current cell (default to 1.0 if CR is 0)
                    var baseChallengeRating = SaveGameService.CurrentCell.ChallengeRating > 0
                        ? SaveGameService.CurrentCell.ChallengeRating
                        : 1.0;

                    // Roll for combat encounter
                    var (encounterOccurred, _, adjustedCR) = RollForCombatEncounter(baseChallengeRating);

                    if (encounterOccurred)
                    {
                        // Combat encounter - exploration is pending until victory
                        scene.Type = SceneType.Battle;
                        scene.EncounterCR = adjustedCR;
                        scene.IsCompleted = false;
                        scene.Message = $"Combat encounter! (CR: {adjustedCR:F2})";
                        return scene;
                    }
                    else
                    {
                        // No encounter - commit exploration immediately
                        CommitPendingExploration(scene);
                        scene.Message = "The party explored the area but encountered nothing of interest.";
                    }
                }
                else
                {
                    scene.Message = "You do not have enough stamina to explore the area.";
                }

                return scene;
            }
        }

        scene.Message = "You have already explored this area.";
        scene.MoveStatus = MoveStatus.Hold;

        return scene;
    }

    private Scene ProcessServices(Event thisEvent, Scene scene)
    {
        scene.Type = SceneType.Shop;
        scene.Location = thisEvent.Location;
        scene = _shopService.ProcessShop(scene);
        return scene;
    }

    private static (bool encounterOccurred, int roll, double adjustedCR) RollForCombatEncounter(double baseChallengeRating)
    {
        var chance = new Chance();
        var roll = chance.Dice(20);

        bool encounterOccurred = roll > 5;
        double adjustedCR = baseChallengeRating;

        if (encounterOccurred)
        {
            adjustedCR = roll switch
            {
                20 => baseChallengeRating * 1.20,      // +20%
                >= 16 => baseChallengeRating * 1.15,   // +15%
                >= 11 => baseChallengeRating * 1.10,   // +10%
                _ => baseChallengeRating
            };
        }

        return (encounterOccurred, roll, adjustedCR);
    }

    /// <summary>
    /// Calculate exploration progress but store it as pending (don't apply yet).
    /// This allows us to only commit the exploration if the player wins combat.
    /// </summary>
    private static MoveStatus CalculatePendingExploration(Scene scene)
    {
        var party = SaveGameService.Party;
        const int defaultExploration = 100;
        var currentLocation = SaveGameService.CurrentLocation;
        int explorationSize;
        var currentBurg = SaveGameService.CurrentBurg;

        if (currentLocation is not null)
        {
            if (!party.TryMove(.005)) return MoveStatus.Failure;

            explorationSize = (int)currentLocation.Type.Size * 100;
            var currentExplored = currentLocation.ExploredPercent * explorationSize;
            var newExplored = currentExplored + defaultExploration;
            var newPercent = newExplored / explorationSize;

            // Store as pending - don't apply yet
            scene.PendingExplorationProgress = newPercent - currentLocation.ExploredPercent;
            scene.HasPendingExploration = true;
            scene.IsLocationExploration = true;

            return MoveStatus.Success;
        }

        if (currentBurg is not null) return MoveStatus.None;

        if (!party.TryMove(.05)) return MoveStatus.Failure;

        var currentCell = SaveGameService.CurrentCell;
        explorationSize = currentCell.CellSize == CellSize.small ? 300 : 500;

        var cellCurrentExplored = currentCell.ExploredPercent * explorationSize;
        var cellNewExplored = cellCurrentExplored + defaultExploration;
        var cellNewPercent = cellNewExplored / explorationSize;

        // Store as pending - don't apply yet
        scene.PendingExplorationProgress = cellNewPercent - currentCell.ExploredPercent;
        scene.HasPendingExploration = true;
        scene.IsLocationExploration = false;

        return MoveStatus.Success;
    }

    /// <summary>
    /// Commit pending exploration progress. Call this on combat victory.
    /// On defeat or flee, simply don't call this method - the pending exploration is discarded.
    /// </summary>
    public static void CommitPendingExploration(Scene scene)
    {
        if (!scene.HasPendingExploration) return;

        var currentLocation = SaveGameService.CurrentLocation;
        var currentCell = SaveGameService.CurrentCell;

        if (scene.IsLocationExploration && currentLocation != null)
        {
            // Apply location exploration
            currentLocation.ExploredPercent = Math.Min(1.0,
                currentLocation.ExploredPercent + scene.PendingExplorationProgress);
        }
        else if (!scene.IsLocationExploration && currentCell != null)
        {
            // Apply cell exploration
            currentCell.ExploredPercent = Math.Min(1.0,
                currentCell.ExploredPercent + scene.PendingExplorationProgress);

            // Discover locations if cell fully explored
            if (Math.Abs(currentCell.ExploredPercent - 1) < .01)
            {
                currentCell.ExploredPercent = 1;
                currentCell.locations.ForEach(x => x.Discovered = true);
            }
            else
            {
                // Roll for location discovery
                var chance = new Chance();
                var dice = chance.Dice(20);
                var undiscoveredLocations = currentCell.locations.Where(x => !x.Discovered).ToList();

                if (dice > 0 && undiscoveredLocations.Count > 0)
                {
                    var random1 = new Random();
                    var pick1 = random1.Next(0, undiscoveredLocations.Count);
                    undiscoveredLocations[pick1].Discovered = true;
                    undiscoveredLocations.RemoveAt(pick1);
                }

                if (dice >= 12 && undiscoveredLocations.Count > 0)
                {
                    var random2 = new Random();
                    var pick2 = random2.Next(0, undiscoveredLocations.Count);
                    undiscoveredLocations[pick2].Discovered = true;
                }
            }
        }

        // Clear pending state
        scene.HasPendingExploration = false;
        scene.PendingExplorationProgress = 0;
    }
}