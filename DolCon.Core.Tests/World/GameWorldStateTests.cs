namespace DolCon.Core.Tests.World;

using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.World;
using DolCon.Core.Services;
using FluentAssertions;

/// <summary>
/// Covers the Phase 2 contract: the game runs off a <see cref="DolWorld"/>, per-playthrough progress
/// rides in the save (not the canonical world.dol), and the static save-state accessors resolve.
/// </summary>
public class GameWorldStateTests
{
    private static DolWorld BuildWorld()
    {
        var locationId = Guid.NewGuid();
        return new DolWorld
        {
            Info = new WorldInfo { Name = "Testland" },
            // Biome names parallel the Biome enum, so name[i] == (Biome)i (index 4 == Grassland).
            Biomes = new List<string> { "Marine", "Hot desert", "Cold desert", "Savanna", "Grassland" },
            States = new List<WorldState> { new() { Id = 0, Name = "Aurelia", FullName = "Kingdom of Aurelia" } },
            Provinces = new List<WorldProvince> { new() { Id = 0, Name = "Dawnmoor", FullName = "Province of Dawnmoor" } },
            Cells = new List<WorldCell>
            {
                new() { Id = 0, Center = new[] { 5.0, 5.0 }, Area = 120, Pop = 0.1m, Biome = 4, State = 0, Province = 0, Burg = 7,
                    Locations = new List<WorldLocation> { new() { Id = locationId, Name = "Tavern", TypeKey = "tavern", Rarity = Rarity.Common } } }
            },
            Burgs = new List<WorldBurg>
            {
                new() { Id = 7, Name = "Lumengarde", Cell = 0, Population = 1050, Size = BurgSize.City, IsCityOfLight = true,
                    Locations = new List<WorldLocation> { new() { Id = locationId, Name = "Lumengarde Tavern", TypeKey = "tavern", Rarity = Rarity.Common } } }
            }
        };
    }

    [Fact]
    public void SaveGameService_Accessors_ResolveFromLoadedWorld()
    {
        SaveGameService.CurrentWorld = BuildWorld();
        SaveGameService.Party = new Party { Cell = 0, Burg = 7 };

        SaveGameService.HasWorld.Should().BeTrue();
        SaveGameService.CurrentCell.Id.Should().Be(0);
        SaveGameService.CurrentBurg!.Name.Should().Be("Lumengarde");
        SaveGameService.CurrentBiome.Should().Be("Grassland");
        SaveGameService.CurrentProvince.FullName.Should().Be("Province of Dawnmoor");
        SaveGameService.CurrentState.Name.Should().Be("Aurelia");
        SaveGameService.CurrentCell.BiomeType.Should().Be(Biome.Grassland);
    }

    [Fact]
    public void SaveGameService_CurrentLocation_ResolvesFromBurg()
    {
        var world = BuildWorld();
        var locationId = world.Burgs[0].Locations[0].Id;
        SaveGameService.CurrentWorld = world;
        SaveGameService.Party = new Party { Cell = 0, Burg = 7, Location = locationId };

        SaveGameService.CurrentLocation.Should().NotBeNull();
        SaveGameService.CurrentLocation!.Id.Should().Be(locationId);
    }

    [Fact]
    public void CleanBake_OmitsPlayerProgressFields()
    {
        var json = DolWorldSerializer.Serialize(BuildWorld());

        json.Should().NotContain("exploredPercent");
        json.Should().NotContain("discovered");
        json.Should().NotContain("lastExplored");
    }

    [Fact]
    public void SaveGame_PersistsProgressOverTheWorld()
    {
        var world = BuildWorld();
        world.Cells[0].ExploredPercent = 0.5;
        world.Cells[0].Locations[0].Discovered = true;
        world.Cells[0].Locations[0].ExploredPercent = 0.25;

        var save = new SaveGame
        {
            World = world,
            Party = new Party { Cell = 0, Burg = 7, Stamina = 0.8 },
            CurrentPlayerId = Guid.NewGuid()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(save, DolWorldSerializer.Options);
        var restored = System.Text.Json.JsonSerializer.Deserialize<SaveGame>(json, DolWorldSerializer.Options);

        restored.Should().NotBeNull();
        restored!.Party.Cell.Should().Be(0);
        restored.CurrentPlayerId.Should().Be(save.CurrentPlayerId);
        restored.World.Cells[0].ExploredPercent.Should().Be(0.5);
        restored.World.Cells[0].Locations[0].Discovered.Should().BeTrue();
        restored.World.Cells[0].Locations[0].ExploredPercent.Should().Be(0.25);
    }
}
