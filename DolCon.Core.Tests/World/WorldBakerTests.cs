namespace DolCon.Core.Tests.World;

using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Models.World;
using DolCon.Core.Services;
using FluentAssertions;

public class WorldBakerTests
{
    private const int CellCount = 6;
    private const string CityOfLightName = "Lumengarde";

    /// <summary>
    /// A small, valid Azgaar-shaped map. A fresh instance per call because provisioning mutates it
    /// in place (flags City of Light, adds locations, sets sizes).
    /// </summary>
    private static Map BuildFixtureMap()
    {
        var cells = new List<Cell>();
        for (var i = 0; i < CellCount; i++)
        {
            cells.Add(new Cell
            {
                i = i,
                v = new List<int> { i, i + 1, i + 2 },
                c = new List<int> { (i + 1) % CellCount },
                p = new List<double> { 10 + i * 20, 10 + i * 15 },
                area = i % 2 == 0 ? 50 : 150,   // mix of small/large CellSize
                pop = i * 0.05m,                 // mix of wild/rural/urban PopDensity
                biome = i % 3,
                state = 1,
                province = 1,
                burg = 0
            });
        }

        var burgs = new List<Burg>
        {
            new() { i = 1, cell = 0, name = "Westford", population = 5, port = 1, x = 10, y = 10 },
            new() { i = 2, cell = 2, name = "Eastvale", population = 40, temple = 1, citadel = 1, x = 50, y = 40 },
            new() { i = 3, cell = 4, name = CityOfLightName, population = 200, port = 1, temple = 1, x = 90, y = 70 }
        };

        return new Map
        {
            info = new Info { mapName = "Fixtureland", seed = "987654", version = "1.99" },
            coords = new Coords { latT = 90, latN = 60, latS = 20, lonT = 180, lonW = -40, lonE = 40 },
            vertices = new List<MapVertex>
            {
                new() { p = new List<double> { 0, 0 }, v = new List<int>(), c = new List<int>() },
                new() { p = new List<double> { 10, 0 }, v = new List<int>(), c = new List<int>() }
            },
            biomes = new Biomes { name = new List<string> { "Marine", "Hot desert", "Temperate deciduous forest" } },
            Collections = new MapCollections
            {
                cells = cells,
                burgs = burgs,
                states = new List<State> { new() { i = 1, name = "Aurelia", fullName = "Kingdom of Aurelia" } },
                provinces = new List<Province> { new() { i = 1, name = "Dawnmoor", fullName = "Province of Dawnmoor" } },
                rivers = new List<River> { new() { i = 1, name = "Silverflow", cells = new List<int> { 0, 1 }, width = 2.5, length = 40, source = 0, mouth = 1 } },
                cultures = new List<Culture>()
            }
        };
    }

    [Fact]
    public void Bake_ProducesExpectedCountsAndStableCityOfLight()
    {
        var baker = new WorldBaker();

        var world = baker.Bake(BuildFixtureMap(), seed: 42, new NoOpMapProvisioningCallback());

        world.SchemaVersion.Should().Be(1);
        world.Info.Name.Should().Be("Fixtureland");
        world.Info.ProvisioningSeed.Should().Be(42);
        world.Cells.Should().HaveCount(CellCount);
        world.Burgs.Should().HaveCount(3);
        world.Biomes.Should().HaveCount(3);
        world.Rivers.Should().HaveCount(1);

        world.Burgs.Should().ContainSingle(b => b.IsCityOfLight)
            .Which.Name.Should().Be(CityOfLightName);

        var locationsPlaced = world.Cells.Sum(c => c.Locations.Count) + world.Burgs.Sum(b => b.Locations.Count);
        locationsPlaced.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Bake_StoresProvisioningOntoTheWorld()
    {
        var baker = new WorldBaker();

        var world = baker.Bake(BuildFixtureMap(), seed: 42, new NoOpMapProvisioningCallback());

        // City of Light cell sits at distance 0 -> CR 0; every cell gets a CR and locations.
        world.Cells.Should().OnlyContain(c => c.Locations.Count > 0);
        var cityOfLight = world.Burgs.Single(b => b.IsCityOfLight);
        cityOfLight.Size.Should().NotBe(default);
        cityOfLight.Locations.Should().NotBeEmpty();

        // Every baked location references a catalog type by key.
        world.Burgs.SelectMany(b => b.Locations).Should().OnlyContain(l => !string.IsNullOrEmpty(l.TypeKey));
    }

    [Fact]
    public void Bake_IsDeterministicForTheSameSeed()
    {
        var baker = new WorldBaker();

        var first = baker.Bake(BuildFixtureMap(), seed: 12345, new NoOpMapProvisioningCallback());
        var second = baker.Bake(BuildFixtureMap(), seed: 12345, new NoOpMapProvisioningCallback());

        // GeneratedAt is wall-clock provenance, not content; everything else must match exactly.
        second.Should().BeEquivalentTo(first, options => options.Excluding(w => w.Info.GeneratedAt));
    }

    [Fact]
    public void Bake_DiffersForDifferentSeeds()
    {
        var baker = new WorldBaker();

        var first = baker.Bake(BuildFixtureMap(), seed: 1, new NoOpMapProvisioningCallback());
        var second = baker.Bake(BuildFixtureMap(), seed: 2, new NoOpMapProvisioningCallback());

        // Different seeds should produce different placement (location ids always differ).
        var firstIds = first.Burgs.SelectMany(b => b.Locations).Select(l => l.Id);
        var secondIds = second.Burgs.SelectMany(b => b.Locations).Select(l => l.Id);
        firstIds.Should().NotIntersectWith(secondIds);
    }

    [Fact]
    public void Bake_RoundTripsThroughDolWorldSerializer()
    {
        var baker = new WorldBaker();
        var world = baker.Bake(BuildFixtureMap(), seed: 7, new NoOpMapProvisioningCallback());

        var json = DolWorldSerializer.Serialize(world);
        var restored = DolWorldSerializer.Deserialize(json);

        restored.Should().BeEquivalentTo(world);
    }
}
