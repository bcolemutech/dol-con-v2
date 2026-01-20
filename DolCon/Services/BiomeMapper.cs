using DolCon.Enums;

namespace DolCon.Services;

/// <summary>
/// Maps game Biome enum to combat BiomeType enum for enemy spawning
/// </summary>
public static class BiomeMapper
{
    /// <summary>
    /// Convert game biome to combat system biome type
    /// </summary>
    public static BiomeType MapToCombatBiome(Biome gameBiome)
    {
        return gameBiome switch
        {
            Biome.Grassland => BiomeType.Plains,
            Biome.Savanna => BiomeType.Plains,
            Biome.TropicalSeasonalForest => BiomeType.Forest,
            Biome.TemperateDeciduousForest => BiomeType.Forest,
            Biome.TropicalRainForest => BiomeType.Forest,
            Biome.TemperateRainForest => BiomeType.Forest,
            Biome.Taiga => BiomeType.Forest,
            Biome.HotDesert => BiomeType.Desert,
            Biome.ColdDesert => BiomeType.Tundra,
            Biome.Tundra => BiomeType.Tundra,
            Biome.Glacier => BiomeType.Mountains,
            Biome.Wetland => BiomeType.Swamp,
            Biome.Marine => BiomeType.Coastal,
            _ => BiomeType.Plains
        };
    }
}
