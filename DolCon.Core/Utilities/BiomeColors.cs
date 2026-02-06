namespace DolCon.Core.Utilities;

using DolCon.Core.Enums;

/// <summary>
/// Provides color mappings for biome visualization.
/// </summary>
public static class BiomeColors
{
    private static readonly Dictionary<Biome, string> Colors = new()
    {
        { Biome.Marine, "#1E90FF" },                  // DodgerBlue
        { Biome.HotDesert, "#F4A460" },               // SandyBrown
        { Biome.ColdDesert, "#B0C4DE" },              // LightSteelBlue
        { Biome.Savanna, "#DAA520" },                 // GoldenRod
        { Biome.Grassland, "#90EE90" },               // LightGreen
        { Biome.TropicalSeasonalForest, "#228B22" },  // ForestGreen
        { Biome.TemperateDeciduousForest, "#32CD32" }, // LimeGreen
        { Biome.TropicalRainForest, "#006400" },      // DarkGreen
        { Biome.TemperateRainForest, "#2E8B57" },     // SeaGreen
        { Biome.Taiga, "#556B2F" },                   // DarkOliveGreen
        { Biome.Tundra, "#D3D3D3" },                  // LightGray
        { Biome.Glacier, "#F0F8FF" },                 // AliceBlue
        { Biome.Wetland, "#6B8E23" }                  // OliveDrab
    };

    /// <summary>
    /// Gets the hex color string for a biome.
    /// </summary>
    /// <param name="biome">The biome type.</param>
    /// <returns>A hex color string in #RRGGBB format.</returns>
    public static string GetHexColor(Biome biome) =>
        Colors.TryGetValue(biome, out var color) ? color : "#808080";

    /// <summary>
    /// Parses a hex color string to RGB components.
    /// </summary>
    /// <param name="hex">The hex color string (with or without # prefix).</param>
    /// <returns>A tuple of (R, G, B) byte values.</returns>
    public static (byte R, byte G, byte B) ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        return (
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16)
        );
    }
}
