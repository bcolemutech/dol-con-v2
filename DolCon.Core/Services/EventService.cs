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
                scene.MoveStatus = ProcessExploration();
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
                        // Combat encounter - set up battle scene
                        scene.Type = SceneType.Battle;
                        scene.EncounterCR = adjustedCR;
                        scene.IsCompleted = false;
                        scene.Message = $"Combat encounter! (CR: {adjustedCR:F2})";
                        return scene;
                    }
                    else
                    {
                        // No encounter - exploration but no rewards
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

    private static MoveStatus ProcessExploration()
    {
        var party = SaveGameService.Party;
        const int defaultExploration = 100;
        var currentLocation = SaveGameService.CurrentLocation;
        int locationExplorationSize;
        double explored;
        var currentBurg = SaveGameService.CurrentBurg;
        if (currentLocation is not null)
        {
            if (!party.TryMove(.005)) return MoveStatus.Failure;

            locationExplorationSize = (int)currentLocation.Type.Size * 100;
            explored = currentLocation.ExploredPercent * locationExplorationSize;

            explored += defaultExploration;

            currentLocation.ExploredPercent = explored / locationExplorationSize;

            return MoveStatus.Success;
        }

        if (currentBurg is not null) return MoveStatus.None;

        if (!party.TryMove(.05)) return MoveStatus.Failure;

        var currentCell = SaveGameService.CurrentCell;

        locationExplorationSize = currentCell.CellSize == CellSize.small ? 300 : 500;

        explored = currentCell.ExploredPercent * locationExplorationSize;
        explored += defaultExploration;
        currentCell.ExploredPercent = explored / locationExplorationSize;

        if (Math.Abs(currentCell.ExploredPercent - 1) < .01)
        {
            currentCell.ExploredPercent = 1;
            currentCell.locations.ForEach(x => x.Discovered = true);
            return MoveStatus.Success;
        }

        var chance = new Chance();
        var dice = chance.Dice(20);
        if (dice > 0)
        {
            var random1 = new Random();
            var pick1 = random1.Next(0, currentCell.locations.Count(x => !x.Discovered));
            var location1 = currentCell.locations.Where(x => !x.Discovered).Skip(pick1)
                .Take(1).First();
            location1.Discovered = true;
        }

        if (dice < 12) return MoveStatus.Success;

        var random2 = new Random();
        var pick2 = random2.Next(0, currentCell.locations.Count(x => !x.Discovered));
        var location2 = currentCell.locations.Where(x => !x.Discovered).Skip(pick2)
            .Take(1).First();
        location2.Discovered = true;
        return MoveStatus.Success;
    }
}