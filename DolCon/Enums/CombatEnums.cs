namespace DolCon.Enums;

/// <summary>
/// Main enemy categories matching your requested structure
/// </summary>
public enum EnemyCategory
{
    Nature,
    Human,
    Undead,
    Demon
}

/// <summary>
/// Subcategories for more specific enemy types
/// </summary>
public enum EnemySubcategory
{
    // Nature subcategories
    Beast,
    Plant,
    Elemental,
    
    // Human subcategories (both for Human and corrupted variants)
    Bandit,
    Cultist,
    Soldier,
    Mercenary,
    
    // Undead subcategories
    UndeadHuman,
    UndeadCreature,
    
    // Demon subcategories
    DemonHuman,
    DemonCreature,
    
    // General
    None
}

/// <summary>
/// Biome types where enemies can spawn
/// </summary>
public enum BiomeType
{
    Forest,
    Mountains,
    Plains,
    Swamp,
    Desert,
    Tundra,
    Underground,
    Ruins,
    CityOfLight,
    CorruptedLands,
    Coastal,
    Volcanic
}

/// <summary>
/// Enemy behavior patterns for AI
/// </summary>
public enum EnemyBehaviorType
{
    Aggressive,     // Always attacks
    Defensive,      // Focuses on defense
    Balanced,       // Mix of offense and defense
    Tactical,       // Uses abilities strategically
    Berserker,      // High aggression, low defense
    Cowardly,       // Flees when low health
    Supportive      // Buffs allies
}

/// <summary>
/// Damage types for resistances/vulnerabilities
/// </summary>
public enum DamageType
{
    Physical,
    Fire,
    Cold,
    Lightning,
    Poison,
    Necrotic,
    Radiant,
    Psychic,
    Force,
    Acid
}

/// <summary>
/// Status effect types
/// </summary>
public enum StatusEffectType
{
    Poisoned,
    Stunned,
    Paralyzed,
    Blinded,
    Deafened,
    Frightened,
    Charmed,
    Restrained,
    Grappled,
    Prone,
    Invisible,
    Blessed,
    Cursed,
    Burning,
    Frozen,
    Bleeding
}
