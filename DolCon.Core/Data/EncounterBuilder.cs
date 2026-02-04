using DolCon.Core.Enums;
using DolCon.Core.Models.Combat;

namespace DolCon.Core.Data;

/// <summary>
/// Utility class for building balanced combat encounters
/// Uses D&D-style encounter building principles
/// </summary>
public class EncounterBuilder
{
    private readonly Random _rng;
    
    public EncounterBuilder(Random? rng = null)
    {
        _rng = rng ?? new Random();
    }
    
    /// <summary>
    /// Build a random encounter for a specific biome and party level
    /// </summary>
    public List<Enemy> BuildEncounter(BiomeType biome, int partyLevel, int partySize, EncounterDifficulty difficulty = EncounterDifficulty.Medium)
    {
        var targetCR = CalculateTargetCR(partyLevel, partySize, difficulty);
        var availableEnemies = EnemyIndex.GetByBiome(biome);
        
        return BuildEncounterFromPool(availableEnemies, targetCR);
    }
    
    /// <summary>
    /// Build an encounter using specific enemy categories
    /// </summary>
    public List<Enemy> BuildThemedEncounter(EnemyCategory category, int partyLevel, int partySize, EncounterDifficulty difficulty = EncounterDifficulty.Medium)
    {
        var targetCR = CalculateTargetCR(partyLevel, partySize, difficulty);
        var availableEnemies = EnemyIndex.GetByCategory(category);
        
        return BuildEncounterFromPool(availableEnemies, targetCR);
    }
    
    /// <summary>
    /// Build an encounter from a specific pool of enemies
    /// </summary>
    private List<Enemy> BuildEncounterFromPool(List<Enemy> availableEnemies, double targetCR)
    {
        if (availableEnemies.Count == 0)
            return new List<Enemy>();
        
        var encounter = new List<Enemy>();
        var currentCR = 0.0;
        
        // Sort enemies by CR
        var sortedEnemies = availableEnemies.OrderBy(e => e.ChallengeRating).ToList();
        
        // Try to build encounter close to target CR
        while (currentCR < targetCR && encounter.Count < 10) // Max 10 enemies
        {
            var remainingCR = targetCR - currentCR;
            
            // Find suitable enemies for remaining CR
            var suitableEnemies = sortedEnemies
                .Where(e => e.ChallengeRating <= remainingCR * 1.2) // Allow slight overage
                .ToList();
            
            if (suitableEnemies.Count == 0)
                break;
            
            // Pick random enemy from suitable ones
            var selectedEnemy = suitableEnemies[_rng.Next(suitableEnemies.Count)];
            
            // Create a copy of the enemy (so we don't modify the template)
            var enemyCopy = CreateEnemyCopy(selectedEnemy);
            encounter.Add(enemyCopy);
            
            // Update CR with multiplier based on number of enemies
            currentCR += selectedEnemy.ChallengeRating * GetEnemyMultiplier(encounter.Count);
        }
        
        return encounter;
    }
    
    /// <summary>
    /// Calculate target CR based on party composition and desired difficulty
    /// Based on D&D 5e encounter building guidelines
    /// </summary>
    private double CalculateTargetCR(int partyLevel, int partySize, EncounterDifficulty difficulty)
    {
        // Base XP thresholds per player per level (simplified)
        var xpThreshold = difficulty switch
        {
            EncounterDifficulty.Easy => partyLevel * 50,
            EncounterDifficulty.Medium => partyLevel * 100,
            EncounterDifficulty.Hard => partyLevel * 150,
            EncounterDifficulty.Deadly => partyLevel * 200,
            _ => partyLevel * 100
        };
        
        var totalXP = xpThreshold * partySize;
        
        // Convert XP back to approximate CR
        // This is a rough conversion
        return totalXP switch
        {
            < 100 => 0.25,
            < 200 => 0.5,
            < 450 => 1,
            < 700 => 2,
            < 1100 => 3,
            < 1800 => 4,
            < 2300 => 5,
            _ => 5 + (totalXP - 2300) / 600.0
        };
    }
    
    /// <summary>
    /// Get encounter multiplier based on number of enemies
    /// More enemies = higher effective difficulty
    /// </summary>
    private double GetEnemyMultiplier(int enemyCount)
    {
        return enemyCount switch
        {
            1 => 1.0,
            2 => 1.5,
            3 or 4 or 5 or 6 => 2.0,
            7 or 8 or 9 or 10 => 2.5,
            _ => 3.0
        };
    }
    
    /// <summary>
    /// Create a copy of an enemy for use in combat
    /// </summary>
    private Enemy CreateEnemyCopy(Enemy template)
    {
        return new Enemy
        {
            Id = Guid.NewGuid(), // New unique ID
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            Subcategory = template.Subcategory,
            ChallengeRating = template.ChallengeRating,
            Biomes = new List<BiomeType>(template.Biomes),
            
            Strength = template.Strength,
            Dexterity = template.Dexterity,
            Constitution = template.Constitution,
            Intelligence = template.Intelligence,
            Wisdom = template.Wisdom,
            Charisma = template.Charisma,
            
            MaxHitPoints = template.MaxHitPoints,
            CurrentHitPoints = template.MaxHitPoints, // Start at full health
            ArmorClass = template.ArmorClass,
            
            StrengthSave = template.StrengthSave,
            DexteritySave = template.DexteritySave,
            ConstitutionSave = template.ConstitutionSave,
            IntelligenceSave = template.IntelligenceSave,
            WisdomSave = template.WisdomSave,
            CharismaSave = template.CharismaSave,
            
            ExperienceValue = template.ExperienceValue,
            PossibleLoot = new List<LootDrop>(template.PossibleLoot),
            
            BehaviorType = template.BehaviorType,
            Aggression = template.Aggression,
            
            IsBoss = template.IsBoss,
            IsElite = template.IsElite,
            IsCorrupted = template.IsCorrupted,
            
            Resistances = new List<DamageType>(template.Resistances),
            Vulnerabilities = new List<DamageType>(template.Vulnerabilities),
            Immunities = new List<DamageType>(template.Immunities),
            
            SpecialTraits = new List<string>(template.SpecialTraits),
            AvailableAbilities = new List<CombatAbility>(template.AvailableAbilities)
        };
    }
    
    /// <summary>
    /// Create a boss encounter
    /// </summary>
    public List<Enemy> BuildBossEncounter(Enemy bossTemplate, int partyLevel, int partySize)
    {
        var encounter = new List<Enemy>();
        
        // Scale boss to party level if needed
        var targetCR = partyLevel + 2; // Boss should be above party level
        var boss = bossTemplate.ChallengeRating < targetCR 
            ? bossTemplate.CreateScaledVersion(targetCR)
            : CreateEnemyCopy(bossTemplate);
        
        encounter.Add(boss);
        
        // Add some minions (optional)
        var minionCR = Math.Max(1, targetCR / 3);
        var availableMinions = EnemyIndex.GetByChallengeRating(minionCR - 1, minionCR + 1)
            .Where(e => e.Category == boss.Category && !e.IsBoss)
            .ToList();
        
        if (availableMinions.Any())
        {
            var minionCount = Math.Min(3, partySize / 2);
            for (int i = 0; i < minionCount; i++)
            {
                var minion = availableMinions[_rng.Next(availableMinions.Count)];
                encounter.Add(CreateEnemyCopy(minion));
            }
        }
        
        return encounter;
    }
}

/// <summary>
/// Difficulty levels for encounters
/// </summary>
public enum EncounterDifficulty
{
    Easy,
    Medium,
    Hard,
    Deadly
}
