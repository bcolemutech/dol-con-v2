namespace DolCon.Core.Tests;

using DolCon.Core.Enums;
using DolCon.Core.Utilities;
using FluentAssertions;

public class BiomeColorsTests
{
    [Theory]
    [InlineData(Biome.Marine, "#1E90FF")]
    [InlineData(Biome.HotDesert, "#F4A460")]
    [InlineData(Biome.ColdDesert, "#B0C4DE")]
    [InlineData(Biome.Savanna, "#DAA520")]
    [InlineData(Biome.Grassland, "#90EE90")]
    [InlineData(Biome.TropicalSeasonalForest, "#228B22")]
    [InlineData(Biome.TemperateDeciduousForest, "#32CD32")]
    [InlineData(Biome.TropicalRainForest, "#006400")]
    [InlineData(Biome.TemperateRainForest, "#2E8B57")]
    [InlineData(Biome.Taiga, "#556B2F")]
    [InlineData(Biome.Tundra, "#D3D3D3")]
    [InlineData(Biome.Glacier, "#F0F8FF")]
    [InlineData(Biome.Wetland, "#6B8E23")]
    public void GetHexColor_ReturnsCorrectColor(Biome biome, string expected)
    {
        BiomeColors.GetHexColor(biome).Should().Be(expected);
    }

    [Fact]
    public void GetHexColor_AllBiomesHaveColors()
    {
        foreach (Biome biome in Enum.GetValues<Biome>())
        {
            var color = BiomeColors.GetHexColor(biome);
            color.Should().NotBeNullOrEmpty($"Biome {biome} should have a color");
            color.Should().StartWith("#", $"Biome {biome} color should be hex format");
            color.Should().HaveLength(7, $"Biome {biome} color should be 7 characters (#RRGGBB)");
        }
    }

    [Theory]
    [InlineData("#1E90FF", 30, 144, 255)]   // Marine - DodgerBlue
    [InlineData("#90EE90", 144, 238, 144)]  // Grassland - LightGreen
    [InlineData("#F0F8FF", 240, 248, 255)]  // Glacier - AliceBlue
    [InlineData("#000000", 0, 0, 0)]        // Black
    [InlineData("#FFFFFF", 255, 255, 255)]  // White
    [InlineData("#FF0000", 255, 0, 0)]      // Red
    public void ParseHexColor_ReturnsCorrectRgb(string hex, byte expectedR, byte expectedG, byte expectedB)
    {
        var (r, g, b) = BiomeColors.ParseHexColor(hex);

        r.Should().Be(expectedR);
        g.Should().Be(expectedG);
        b.Should().Be(expectedB);
    }

    [Theory]
    [InlineData("1E90FF")]   // Without hash
    [InlineData("#1E90FF")]  // With hash
    public void ParseHexColor_HandlesWithAndWithoutHash(string hex)
    {
        var (r, g, b) = BiomeColors.ParseHexColor(hex);

        r.Should().Be(30);
        g.Should().Be(144);
        b.Should().Be(255);
    }
}
