using DolCon.Enums;

namespace DolCon.Models.Combat;

/// <summary>
/// Represents an enemy combatant with challenge rating and biome information
/// Extends the base CombatEntity with enemy-specific data
/// </summary>
public class Enemy : CombatEntity
{
    // Enemy Classification
    public EnemyCategory Category { get; set; }
    public EnemySubcategory Subcategory { get; set; }
    
    // Challenge Rating (D&D style)
    public double ChallengeRating { get; set; }
    
    // Biome/Environment where this enemy can be found
    public List<BiomeType> Biomes { get; set; } = new();
    
    // Loot and Experience
    public int ExperienceValue { get; set; }
    public List<LootDrop> PossibleLoot { get; set; } = new();
    
    // Behavior AI
    public EnemyBehaviorType BehaviorType { get; set; }
    public double Aggression { get; set; } = 0.5; // 0.0 to 1.0
    
    // Special Enemy Flags
    public bool IsBoss { get; set; }
    public bool IsElite { get; set; }
    public bool IsCorrupted { get; set; } // Fits your "evil corruption" theme
    
    // Damage Resistances and Vulnerabilities (D&D style)
    public List<DamageType> Resistances { get; set; } = new();
    public List<DamageType> Vulnerabilities { get; set; } = new();
    public List<DamageType> Immunities { get; set; } = new();
    
    // Special Abilities
    public List<string> SpecialTraits { get; set; } = new();
    
    // Calculate XP based on Challenge Rating (D&D 5e formula)
    public void CalculateExperienceValue()
    {
        ExperienceValue = ChallengeRating switch
        {
            0 => 10,
            0.125 => 25,
            0.25 => 50,
            0.5 => 100,
            1 => 200,
            2 => 450,
            3 => 700,
            4 => 1100,
            5 => 1800,
            6 => 2300,
            7 => 2900,
            8 => 3900,
            9 => 5000,
            10 => 5900,
            _ => (int)(ExperienceValue * Math.Pow(1.5, ChallengeRating - 10))
        };
    }
    
    // Check if enemy can spawn in specific biome
    public bool CanSpawnIn(BiomeType biome)
    {
        return Biomes.Contains(biome);
    }
    
    // Create a scaled version of this enemy for higher/lower CR
    public Enemy CreateScaledVersion(double newCR)
    {
        var scaleFactor = newCR / ChallengeRating;
        
        return new Enemy
        {
            Name = IsBoss ? $"{Name} (Empowered)" : Name,
            Description = Description,
            Category = Category,
            Subcategory = Subcategory,
            ChallengeRating = newCR,
            Biomes = new List<BiomeType>(Biomes),
            
            // Scale stats
            Strength = (int)(Strength * Math.Sqrt(scaleFactor)),
            Dexterity = (int)(Dexterity * Math.Sqrt(scaleFactor)),
            Constitution = (int)(Constitution * Math.Sqrt(scaleFactor)),
            Intelligence = Intelligence,
            Wisdom = Wisdom,
            Charisma = Charisma,
            
            MaxHitPoints = (int)(MaxHitPoints * scaleFactor),
            CurrentHitPoints = (int)(MaxHitPoints * scaleFactor),
            ArmorClass = ArmorClass + (int)(scaleFactor - 1.0),
            
            Resistances = new List<DamageType>(Resistances),
            Vulnerabilities = new List<DamageType>(Vulnerabilities),
            Immunities = new List<DamageType>(Immunities),
            
            BehaviorType = BehaviorType,
            Aggression = Aggression,
            IsBoss = IsBoss,
            IsElite = IsElite || scaleFactor > 1.5,
            IsCorrupted = IsCorrupted
        };
    }
}
