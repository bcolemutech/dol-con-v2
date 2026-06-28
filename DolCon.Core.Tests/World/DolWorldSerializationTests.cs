namespace DolCon.Core.Tests.World;

using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.World;
using FluentAssertions;

public class DolWorldSerializationTests
{
    private static DolWorld BuildSampleWorld(bool withEnrichment)
    {
        var world = new DolWorld
        {
            Info = new WorldInfo
            {
                Name = "Test World",
                SourceSeed = "123456",
                SourceAzgaarVersion = "1.99",
                GeneratedAt = new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc),
                GeneratorVersion = "0.1.0"
            },
            Coords = new WorldCoords { LatT = 90, LatN = 60, LatS = 20, LonT = 180, LonW = -40, LonE = 40 },
            Vertices = new List<WorldVertex>
            {
                new() { P = new[] { 0.0, 0.0 } },
                new() { P = new[] { 10.0, 0.0 } },
                new() { P = new[] { 5.0, 10.0 } }
            },
            Biomes = new List<string> { "Marine", "Hot desert", "Temperate deciduous forest" },
            States = new List<WorldState> { new() { Id = 1, Name = "Aurelia", FullName = "Kingdom of Aurelia" } },
            Provinces = new List<WorldProvince> { new() { Id = 1, Name = "Dawnmoor", FullName = "Province of Dawnmoor" } },
            Rivers = new List<WorldRiver>
            {
                new() { Id = 1, Name = "Silverflow", Cells = new List<int> { 0, 1 }, Width = 2.5, Length = 40, Source = 0, Mouth = 1 }
            },
            Cells = new List<WorldCell>
            {
                new()
                {
                    Id = 0,
                    VertexIndices = new List<int> { 0, 1, 2 },
                    Neighbors = new List<int> { 1 },
                    Center = new[] { 5.0, 3.0 },
                    Area = 120,
                    Pop = 4.2m,
                    Biome = 2,
                    State = 1,
                    Province = 1,
                    Burg = 1,
                    ChallengeRating = 0.0,
                    Locations = new List<WorldLocation>
                    {
                        new() { Id = Guid.NewGuid(), Name = "ruins", TypeKey = "ruins", Rarity = Rarity.Uncommon }
                    }
                },
                new()
                {
                    Id = 1,
                    VertexIndices = new List<int> { 1, 2, 0 },
                    Neighbors = new List<int> { 0 },
                    Center = new[] { 8.0, 6.0 },
                    Area = 80,
                    Pop = 0.0m,
                    Biome = 1,
                    State = 1,
                    Province = 1,
                    Burg = 0,
                    ChallengeRating = 4.5
                }
            },
            Burgs = new List<WorldBurg>
            {
                new()
                {
                    Id = 1,
                    Name = "Lumengarde",
                    Cell = 0,
                    Population = 1050,
                    Size = BurgSize.City,
                    IsCityOfLight = true,
                    X = 5.0,
                    Y = 3.0,
                    HasPort = true,
                    HasTemple = true,
                    Locations = new List<WorldLocation>
                    {
                        new() { Id = Guid.NewGuid(), Name = "Lumengarde tavern", TypeKey = "tavern", Rarity = Rarity.Common }
                    }
                }
            }
        };

        if (withEnrichment)
        {
            world.Enrichment = new Enrichment
            {
                Status = EnrichmentStatus.Draft,
                History = new List<HistoryEvent>
                {
                    new() { Title = "The Sundering", Era = "First Age", Description = "The world split." }
                },
                Notes = "Seed lore here."
            };
            world.Burgs[0].Enrichment = new Enrichment
            {
                Status = EnrichmentStatus.Authored,
                Npcs = new List<Npc>
                {
                    new() { Id = "npc-1", Name = "Mayor Voss", Role = "Mayor", Description = "Stern but fair." }
                },
                Assets = new List<AssetRef>
                {
                    new() { Key = "lumengarde-banner", Kind = "icon", Spec = "A radiant sunburst banner.", Status = EnrichmentStatus.Draft }
                }
            };
        }

        return world;
    }

    [Fact]
    public void DolWorld_FullRoundTrip_IsLossless()
    {
        // Arrange
        var world = BuildSampleWorld(withEnrichment: true);

        // Act
        var json = DolWorldSerializer.Serialize(world);
        var restored = DolWorldSerializer.Deserialize(json);

        // Assert
        restored.Should().BeEquivalentTo(world);
    }

    [Fact]
    public void DolWorld_MinimalWorldWithoutEnrichment_OmitsNullContainersAndRoundTrips()
    {
        // Arrange
        var world = BuildSampleWorld(withEnrichment: false);

        // Act
        var json = DolWorldSerializer.Serialize(world);
        var restored = DolWorldSerializer.Deserialize(json);

        // Assert
        json.Should().NotContain("\"enrichment\"");
        restored.Should().BeEquivalentTo(world);
    }

    [Fact]
    public void DolWorld_Serializes_WithHumanReadableEnumsAndSchemaVersion()
    {
        // Arrange
        var world = BuildSampleWorld(withEnrichment: true);

        // Act
        var json = DolWorldSerializer.Serialize(world);

        // Assert
        json.Should().Contain("\"schemaVersion\": 1");
        json.Should().Contain("\"size\": \"City\"");
        json.Should().Contain("\"status\": \"Draft\"");
        json.Should().Contain("\"rarity\": \"Common\"");
    }

    [Fact]
    public void WorldLocation_TypeKey_RehydratesFromLocationTypesCatalog()
    {
        // Arrange
        var world = BuildSampleWorld(withEnrichment: false);
        var bakedLocation = world.Burgs[0].Locations[0];

        // Act
        var locationType = LocationTypes.Types.FirstOrDefault(t => t.Type == bakedLocation.TypeKey);

        // Assert
        locationType.Should().NotBeNull();
        locationType!.Type.Should().Be("tavern");
    }
}
