using ChanceNET;
using DolCon.Enums;
using DolCon.Models;

namespace DolCon.Services;

public interface IEventService
{
    Scene ProcessEvent(Event thisEvent);
}

public class EventService : IEventService
{
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
                    var totalCoin = 0;
                    foreach (var player in SaveGameService.Party.Players)
                    {
                        var random = new Chance().New();
                        var playerCoin = random.Dice(100) * 10;
                        player.coin += playerCoin;
                        totalCoin += playerCoin;
                    }

                    scene.Message = "You have explored the area. You have found " + totalCoin + " coin.";
                }
                else
                {
                    scene.Message = "You are not in a location or area.";
                }

                return scene;
            }
        }

        scene.Message = "You have already explored this area.";
        scene.MoveStatus = MoveStatus.Hold;

        return scene;
    }

    private static Scene ProcessServices(Event thisEvent, Scene scene)
    {
        scene.Message = "Feature not implemented for Services: " + string.Join(", ", thisEvent.Location.Type.Services);
        scene.MoveStatus = MoveStatus.None;
        return scene;
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