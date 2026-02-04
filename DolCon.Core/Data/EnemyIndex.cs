using DolCon.Core.Enums;
using DolCon.Core.Models.Combat;

namespace DolCon.Core.Data;

/// <summary>
/// Central repository for all enemy definitions in the game
/// Organized by category: Nature, Human, Undead, Demon
/// </summary>
public static class EnemyIndex
{
    private static List<Enemy> _allEnemies = new();
    private static bool _initialized = false;
    
    public static List<Enemy> AllEnemies
    {
        get
        {
            if (!_initialized)
                Initialize();
            return _allEnemies;
        }
    }
    
    /// <summary>
    /// Get all enemies of a specific category
    /// </summary>
    public static List<Enemy> GetByCategory(EnemyCategory category)
    {
        return AllEnemies.Where(e => e.Category == category).ToList();
    }
    
    /// <summary>
    /// Get all enemies of a specific subcategory
    /// </summary>
    public static List<Enemy> GetBySubcategory(EnemySubcategory subcategory)
    {
        return AllEnemies.Where(e => e.Subcategory == subcategory).ToList();
    }
    
    /// <summary>
    /// Get enemies by challenge rating range
    /// </summary>
    public static List<Enemy> GetByChallengeRating(double minCR, double maxCR)
    {
        return AllEnemies.Where(e => e.ChallengeRating >= minCR && e.ChallengeRating <= maxCR).ToList();
    }
    
    /// <summary>
    /// Get enemies that can spawn in a specific biome
    /// </summary>
    public static List<Enemy> GetByBiome(BiomeType biome)
    {
        return AllEnemies.Where(e => e.Biomes.Contains(biome)).ToList();
    }
    
    /// <summary>
    /// Get enemies suitable for a specific biome and challenge rating
    /// </summary>
    public static List<Enemy> GetForEncounter(BiomeType biome, double targetCR, double crVariance = 0.5)
    {
        return AllEnemies.Where(e => 
            e.Biomes.Contains(biome) && 
            e.ChallengeRating >= targetCR - crVariance && 
            e.ChallengeRating <= targetCR + crVariance
        ).ToList();
    }
    
    /// <summary>
    /// Get a random enemy from the index
    /// </summary>
    public static Enemy? GetRandomEnemy(Random rng)
    {
        if (AllEnemies.Count == 0) return null;
        return AllEnemies[rng.Next(AllEnemies.Count)];
    }
    
    /// <summary>
    /// Initialize the enemy index with all enemy definitions
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        
        _allEnemies = new List<Enemy>();
        
        // Add all enemy categories
        AddNatureEnemies();
        AddHumanEnemies();
        AddUndeadEnemies();
        AddDemonEnemies();
        
        // Calculate XP for all enemies
        foreach (var enemy in _allEnemies)
        {
            enemy.CalculateExperienceValue();
        }
        
        _initialized = true;
    }
    
    /// <summary>
    /// Add a custom enemy to the index
    /// </summary>
    public static void AddEnemy(Enemy enemy)
    {
        _allEnemies.Add(enemy);
        enemy.CalculateExperienceValue();
    }
    
    // ============================================
    // NATURE ENEMIES
    // ============================================
    private static void AddNatureEnemies()
    {
        // Corrupted Wolf - Low CR
        _allEnemies.Add(new Enemy
        {
            Name = "Corrupted Wolf",
            Description = "A once-noble wolf, twisted by the spreading evil into a feral beast with glowing red eyes.",
            Category = EnemyCategory.Nature,
            Subcategory = EnemySubcategory.Beast,
            ChallengeRating = 1,
            Biomes = new List<BiomeType> { BiomeType.Forest, BiomeType.Plains, BiomeType.CorruptedLands },
            
            Strength = 12,
            Dexterity = 15,
            Constitution = 12,
            Intelligence = 3,
            Wisdom = 12,
            Charisma = 6,
            
            MaxHitPoints = 22,
            CurrentHitPoints = 22,
            ArmorClass = 13,
            
            BehaviorType = EnemyBehaviorType.Aggressive,
            Aggression = 0.8,
            IsCorrupted = true,
            
            Resistances = new List<DamageType> { DamageType.Poison },
            
            SpecialTraits = new List<string> { "Pack Tactics: Advantage on attacks if ally is adjacent to target" }
        });
        
        // Dire Bear - Medium CR
        _allEnemies.Add(new Enemy
        {
            Name = "Dire Bear",
            Description = "A massive bear, driven mad by the corruption seeping through the forest.",
            Category = EnemyCategory.Nature,
            Subcategory = EnemySubcategory.Beast,
            ChallengeRating = 3,
            Biomes = new List<BiomeType> { BiomeType.Forest, BiomeType.Mountains },
            
            Strength = 19,
            Dexterity = 10,
            Constitution = 16,
            Intelligence = 2,
            Wisdom = 13,
            Charisma = 7,
            
            MaxHitPoints = 68,
            CurrentHitPoints = 68,
            ArmorClass = 14,
            
            BehaviorType = EnemyBehaviorType.Berserker,
            Aggression = 0.9,
            IsCorrupted = true,
            
            SpecialTraits = new List<string> { "Rage: Increased damage when below 50% HP" }
        });
        
        // Thornling - Plant creature
        _allEnemies.Add(new Enemy
        {
            Name = "Thornling",
            Description = "A twisted plant creature with razor-sharp thorns and strangling vines.",
            Category = EnemyCategory.Nature,
            Subcategory = EnemySubcategory.Plant,
            ChallengeRating = 2,
            Biomes = new List<BiomeType> { BiomeType.Forest, BiomeType.Swamp },
            
            Strength = 15,
            Dexterity = 8,
            Constitution = 14,
            Intelligence = 4,
            Wisdom = 10,
            Charisma = 3,
            
            MaxHitPoints = 45,
            CurrentHitPoints = 45,
            ArmorClass = 15,
            
            BehaviorType = EnemyBehaviorType.Defensive,
            Aggression = 0.4,
            IsCorrupted = true,
            
            Resistances = new List<DamageType> { DamageType.Poison, DamageType.Physical },
            Vulnerabilities = new List<DamageType> { DamageType.Fire },
            
            SpecialTraits = new List<string> { "Thorn Skin: Reflects damage to attackers" }
        });
        
        // Fire Elemental
        _allEnemies.Add(new Enemy
        {
            Name = "Lesser Fire Elemental",
            Description = "A swirling mass of flames given form and malevolent purpose.",
            Category = EnemyCategory.Nature,
            Subcategory = EnemySubcategory.Elemental,
            ChallengeRating = 4,
            Biomes = new List<BiomeType> { BiomeType.Volcanic, BiomeType.Ruins },
            
            Strength = 10,
            Dexterity = 17,
            Constitution = 16,
            Intelligence = 6,
            Wisdom = 10,
            Charisma = 7,
            
            MaxHitPoints = 72,
            CurrentHitPoints = 72,
            ArmorClass = 14,
            
            BehaviorType = EnemyBehaviorType.Aggressive,
            Aggression = 0.7,
            
            Immunities = new List<DamageType> { DamageType.Fire, DamageType.Poison },
            Vulnerabilities = new List<DamageType> { DamageType.Cold },
            
            SpecialTraits = new List<string> { "Burning Touch: Inflicts burning status", "Illumination: Cannot be blinded" }
        });
    }
    
    // ============================================
    // HUMAN ENEMIES
    // ============================================
    private static void AddHumanEnemies()
    {
        // Bandit
        _allEnemies.Add(new Enemy
        {
            Name = "Bandit",
            Description = "A desperate outlaw preying on travelers. Poverty and desperation have led them astray.",
            Category = EnemyCategory.Human,
            Subcategory = EnemySubcategory.Bandit,
            ChallengeRating = 0.5,
            Biomes = new List<BiomeType> { BiomeType.Forest, BiomeType.Plains, BiomeType.Mountains },
            
            Strength = 11,
            Dexterity = 12,
            Constitution = 12,
            Intelligence = 10,
            Wisdom = 10,
            Charisma = 10,
            
            MaxHitPoints = 18,
            CurrentHitPoints = 18,
            ArmorClass = 12,
            
            BehaviorType = EnemyBehaviorType.Cowardly,
            Aggression = 0.5,
            
            PossibleLoot = new List<LootDrop>
            {
                new LootDrop { ItemName = "Gold Coins", DropChance = 0.8, MinQuantity = 5, MaxQuantity = 15 },
                new LootDrop { ItemName = "Short Sword", DropChance = 0.3 }
            }
        });
        
        // Cultist
        _allEnemies.Add(new Enemy
        {
            Name = "Dark Cultist",
            Description = "A fanatic who has embraced the spreading evil, wielding forbidden magic.",
            Category = EnemyCategory.Human,
            Subcategory = EnemySubcategory.Cultist,
            ChallengeRating = 2,
            Biomes = new List<BiomeType> { BiomeType.Ruins, BiomeType.CorruptedLands, BiomeType.Underground },
            
            Strength = 9,
            Dexterity = 11,
            Constitution = 13,
            Intelligence = 14,
            Wisdom = 12,
            Charisma = 14,
            
            MaxHitPoints = 33,
            CurrentHitPoints = 33,
            ArmorClass = 11,
            
            BehaviorType = EnemyBehaviorType.Tactical,
            Aggression = 0.6,
            IsCorrupted = true,
            
            SpecialTraits = new List<string> { "Dark Magic: Can cast necrotic spells", "Fanatic: Advantage on fear saves" }
        });
        
        // Corrupted Soldier
        _allEnemies.Add(new Enemy
        {
            Name = "Corrupted Soldier",
            Description = "Once a defender of the realm, now twisted by evil into a mindless warrior.",
            Category = EnemyCategory.Human,
            Subcategory = EnemySubcategory.Soldier,
            ChallengeRating = 3,
            Biomes = new List<BiomeType> { BiomeType.CorruptedLands, BiomeType.Ruins },
            
            Strength = 15,
            Dexterity = 13,
            Constitution = 14,
            Intelligence = 8,
            Wisdom = 10,
            Charisma = 9,
            
            MaxHitPoints = 52,
            CurrentHitPoints = 52,
            ArmorClass = 16,
            
            BehaviorType = EnemyBehaviorType.Balanced,
            Aggression = 0.7,
            IsCorrupted = true,
            
            Resistances = new List<DamageType> { DamageType.Necrotic },
            
            SpecialTraits = new List<string> { "Martial Training: Proficient with all weapons and armor" }
        });
        
        // Mercenary Captain
        _allEnemies.Add(new Enemy
        {
            Name = "Mercenary Captain",
            Description = "A skilled warrior for hire, loyal only to coin and combat.",
            Category = EnemyCategory.Human,
            Subcategory = EnemySubcategory.Mercenary,
            ChallengeRating = 5,
            Biomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Mountains, BiomeType.CityOfLight },
            
            Strength = 16,
            Dexterity = 14,
            Constitution = 15,
            Intelligence = 12,
            Wisdom = 13,
            Charisma = 14,
            
            MaxHitPoints = 85,
            CurrentHitPoints = 85,
            ArmorClass = 17,
            
            BehaviorType = EnemyBehaviorType.Tactical,
            Aggression = 0.6,
            IsElite = true,
            
            SpecialTraits = new List<string> 
            { 
                "Leadership: Allies gain +1 to hit",
                "Second Wind: Can heal once per combat",
                "Extra Attack: Can attack twice in Attack phase"
            }
        });
    }
    
    // ============================================
    // UNDEAD ENEMIES
    // ============================================
    private static void AddUndeadEnemies()
    {
        // Zombie (Undead Human)
        _allEnemies.Add(new Enemy
        {
            Name = "Zombie",
            Description = "A reanimated corpse, shambling forward with mindless hunger.",
            Category = EnemyCategory.Undead,
            Subcategory = EnemySubcategory.UndeadHuman,
            ChallengeRating = 0.25,
            Biomes = new List<BiomeType> { BiomeType.CorruptedLands, BiomeType.Ruins, BiomeType.Swamp },
            
            Strength = 13,
            Dexterity = 6,
            Constitution = 16,
            Intelligence = 3,
            Wisdom = 6,
            Charisma = 5,
            
            MaxHitPoints = 32,
            CurrentHitPoints = 32,
            ArmorClass = 8,
            
            BehaviorType = EnemyBehaviorType.Aggressive,
            Aggression = 1.0,
            
            Immunities = new List<DamageType> { DamageType.Poison },
            Vulnerabilities = new List<DamageType> { DamageType.Radiant },
            
            SpecialTraits = new List<string> { "Undead Fortitude: Can potentially survive lethal damage" }
        });
        
        // Skeleton Warrior (Undead Human)
        _allEnemies.Add(new Enemy
        {
            Name = "Skeleton Warrior",
            Description = "Animated bones clad in rusted armor, wielding ancient weapons.",
            Category = EnemyCategory.Undead,
            Subcategory = EnemySubcategory.UndeadHuman,
            ChallengeRating = 1,
            Biomes = new List<BiomeType> { BiomeType.Ruins, BiomeType.Underground, BiomeType.CorruptedLands },
            
            Strength = 10,
            Dexterity = 14,
            Constitution = 15,
            Intelligence = 6,
            Wisdom = 8,
            Charisma = 5,
            
            MaxHitPoints = 28,
            CurrentHitPoints = 28,
            ArmorClass = 13,
            
            BehaviorType = EnemyBehaviorType.Balanced,
            Aggression = 0.7,
            
            Immunities = new List<DamageType> { DamageType.Poison },
            Vulnerabilities = new List<DamageType> { DamageType.Physical }, // Bludgeoning specifically
            Resistances = new List<DamageType> { DamageType.Cold, DamageType.Necrotic },
            
            SpecialTraits = new List<string> { "Reassemble: May resurrect if not destroyed completely" }
        });
        
        // Wraith (Undead Creature)
        _allEnemies.Add(new Enemy
        {
            Name = "Wraith",
            Description = "A spectral creature of pure malevolence, feeding on the life force of the living.",
            Category = EnemyCategory.Undead,
            Subcategory = EnemySubcategory.UndeadCreature,
            ChallengeRating = 5,
            Biomes = new List<BiomeType> { BiomeType.Ruins, BiomeType.CorruptedLands, BiomeType.Underground },
            
            Strength = 6,
            Dexterity = 16,
            Constitution = 16,
            Intelligence = 12,
            Wisdom = 14,
            Charisma = 15,
            
            MaxHitPoints = 67,
            CurrentHitPoints = 67,
            ArmorClass = 14,
            
            BehaviorType = EnemyBehaviorType.Tactical,
            Aggression = 0.8,
            
            Immunities = new List<DamageType> { DamageType.Poison, DamageType.Necrotic },
            Vulnerabilities = new List<DamageType> { DamageType.Radiant },
            Resistances = new List<DamageType> { DamageType.Physical, DamageType.Cold },
            
            SpecialTraits = new List<string> 
            { 
                "Incorporeal: Can pass through walls",
                "Life Drain: Attacks reduce max HP",
                "Sunlight Sensitivity: Disadvantage in sunlight"
            }
        });
        
        // Corrupted Hound (Undead Creature)
        _allEnemies.Add(new Enemy
        {
            Name = "Corrupted Hound",
            Description = "A nightmarish undead beast with rotting flesh and burning eyes.",
            Category = EnemyCategory.Undead,
            Subcategory = EnemySubcategory.UndeadCreature,
            ChallengeRating = 2,
            Biomes = new List<BiomeType> { BiomeType.CorruptedLands, BiomeType.Swamp },
            
            Strength = 14,
            Dexterity = 15,
            Constitution = 13,
            Intelligence = 4,
            Wisdom = 12,
            Charisma = 6,
            
            MaxHitPoints = 39,
            CurrentHitPoints = 39,
            ArmorClass = 12,
            
            BehaviorType = EnemyBehaviorType.Aggressive,
            Aggression = 0.9,
            
            Immunities = new List<DamageType> { DamageType.Poison },
            Vulnerabilities = new List<DamageType> { DamageType.Radiant },
            
            SpecialTraits = new List<string> { "Pack Tactics", "Diseased Bite: Poison damage on hit" }
        });
    }
    
    // ============================================
    // DEMON ENEMIES
    // ============================================
    private static void AddDemonEnemies()
    {
        // Imp (Demon Creature)
        _allEnemies.Add(new Enemy
        {
            Name = "Imp",
            Description = "A small, mischievous demon with leathery wings and a barbed tail.",
            Category = EnemyCategory.Demon,
            Subcategory = EnemySubcategory.DemonCreature,
            ChallengeRating = 1,
            Biomes = new List<BiomeType> { BiomeType.CorruptedLands, BiomeType.Ruins, BiomeType.Volcanic },
            
            Strength = 6,
            Dexterity = 17,
            Constitution = 13,
            Intelligence = 11,
            Wisdom = 12,
            Charisma = 14,
            
            MaxHitPoints = 20,
            CurrentHitPoints = 20,
            ArmorClass = 13,
            
            BehaviorType = EnemyBehaviorType.Cowardly,
            Aggression = 0.5,
            
            Resistances = new List<DamageType> { DamageType.Cold, DamageType.Physical },
            Immunities = new List<DamageType> { DamageType.Fire, DamageType.Poison },
            Vulnerabilities = new List<DamageType> { DamageType.Radiant },
            
            SpecialTraits = new List<string> 
            { 
                "Shapechanger: Can polymorph",
                "Devil's Sight: Can see in magical darkness",
                "Flight: Can fly"
            }
        });
        
        // Possessed Human (Demon Human)
        _allEnemies.Add(new Enemy
        {
            Name = "Possessed Villager",
            Description = "A human whose body is controlled by a demonic entity, their eyes burning with unholy fire.",
            Category = EnemyCategory.Demon,
            Subcategory = EnemySubcategory.DemonHuman,
            ChallengeRating = 3,
            Biomes = new List<BiomeType> { BiomeType.CorruptedLands, BiomeType.Plains },
            
            Strength = 15,
            Dexterity = 12,
            Constitution = 16,
            Intelligence = 10,
            Wisdom = 8,
            Charisma = 16,
            
            MaxHitPoints = 58,
            CurrentHitPoints = 58,
            ArmorClass = 12,
            
            BehaviorType = EnemyBehaviorType.Berserker,
            Aggression = 0.9,
            IsCorrupted = true,
            
            Resistances = new List<DamageType> { DamageType.Fire, DamageType.Necrotic },
            Vulnerabilities = new List<DamageType> { DamageType.Radiant },
            
            SpecialTraits = new List<string> 
            { 
                "Demonic Strength: Enhanced physical power",
                "Unholy Resistance: Advantage on saves vs holy magic",
                "Fear Aura: Can frighten nearby enemies"
            }
        });
        
        // Hell Knight (Demon Human)
        _allEnemies.Add(new Enemy
        {
            Name = "Hell Knight",
            Description = "A fallen paladin consumed by demonic power, wielding cursed weapons.",
            Category = EnemyCategory.Demon,
            Subcategory = EnemySubcategory.DemonHuman,
            ChallengeRating = 7,
            Biomes = new List<BiomeType> { BiomeType.CorruptedLands, BiomeType.Ruins },
            
            Strength = 18,
            Dexterity = 14,
            Constitution = 17,
            Intelligence = 12,
            Wisdom = 13,
            Charisma = 16,
            
            MaxHitPoints = 112,
            CurrentHitPoints = 112,
            ArmorClass = 18,
            
            BehaviorType = EnemyBehaviorType.Tactical,
            Aggression = 0.7,
            IsElite = true,
            IsCorrupted = true,
            
            Resistances = new List<DamageType> { DamageType.Fire, DamageType.Necrotic, DamageType.Physical },
            Vulnerabilities = new List<DamageType> { DamageType.Radiant },
            
            SpecialTraits = new List<string> 
            { 
                "Fallen Oath: Can use corrupted paladin abilities",
                "Aura of Despair: Debuffs nearby heroes",
                "Extra Attack",
                "Dark Smite: Necrotic damage on weapon attacks"
            }
        });
        
        // Demon Beast (Demon Creature)
        _allEnemies.Add(new Enemy
        {
            Name = "Demon Beast",
            Description = "A massive, hulking creature from the depths of corruption, all muscle and fury.",
            Category = EnemyCategory.Demon,
            Subcategory = EnemySubcategory.DemonCreature,
            ChallengeRating = 6,
            Biomes = new List<BiomeType> { BiomeType.CorruptedLands, BiomeType.Volcanic },
            
            Strength = 20,
            Dexterity = 12,
            Constitution = 19,
            Intelligence = 5,
            Wisdom = 10,
            Charisma = 8,
            
            MaxHitPoints = 95,
            CurrentHitPoints = 95,
            ArmorClass = 15,
            
            BehaviorType = EnemyBehaviorType.Berserker,
            Aggression = 0.95,
            
            Resistances = new List<DamageType> { DamageType.Fire, DamageType.Physical },
            Immunities = new List<DamageType> { DamageType.Poison },
            Vulnerabilities = new List<DamageType> { DamageType.Radiant, DamageType.Cold },
            
            SpecialTraits = new List<string> 
            { 
                "Reckless: Attacks with advantage but grants advantage to attackers",
                "Rampage: Extra attack on kill",
                "Fire Breath: Can breathe fire in a cone"
            }
        });
        
        // Arch-Demon (Boss)
        _allEnemies.Add(new Enemy
        {
            Name = "Corrupted Arch-Demon",
            Description = "A towering demon lord, commander of the corrupting forces. Its very presence spreads evil.",
            Category = EnemyCategory.Demon,
            Subcategory = EnemySubcategory.DemonCreature,
            ChallengeRating = 12,
            Biomes = new List<BiomeType> { BiomeType.CorruptedLands },
            
            Strength = 22,
            Dexterity = 16,
            Constitution = 21,
            Intelligence = 18,
            Wisdom = 17,
            Charisma = 20,
            
            MaxHitPoints = 250,
            CurrentHitPoints = 250,
            ArmorClass = 19,
            
            BehaviorType = EnemyBehaviorType.Tactical,
            Aggression = 0.8,
            IsBoss = true,
            IsElite = true,
            IsCorrupted = true,
            
            Resistances = new List<DamageType> { DamageType.Fire, DamageType.Lightning, DamageType.Physical, DamageType.Necrotic },
            Immunities = new List<DamageType> { DamageType.Poison },
            Vulnerabilities = new List<DamageType> { DamageType.Radiant },
            
            SpecialTraits = new List<string> 
            { 
                "Legendary Resistance: Can succeed on failed saves 3/day",
                "Corruption Aura: All enemies take necrotic damage each turn",
                "Multi-Attack: Can make 3 attacks per turn",
                "Summon Demons: Can summon lesser demons",
                "Dark Magic Mastery: Can cast high-level spells",
                "Regeneration: Regains HP each turn unless damaged by radiant"
            },
            
            PossibleLoot = new List<LootDrop>
            {
                new LootDrop { ItemName = "Demonic Core", DropChance = 1.0 },
                new LootDrop { ItemName = "Legendary Weapon", DropChance = 0.5 },
                new LootDrop { ItemName = "Ancient Artifact", DropChance = 0.3 }
            }
        });
    }
}
