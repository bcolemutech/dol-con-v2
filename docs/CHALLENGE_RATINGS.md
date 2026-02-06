# Challenge Ratings and Enemy Selection

This document explains how DolCon determines which enemies you encounter and how difficult those encounters will be. The system uses a Challenge Rating (CR) mechanic inspired by tabletop RPGs, combined with biome-based enemy selection.

---

## Table of Contents

1. [What is Challenge Rating?](#what-is-challenge-rating)
2. [Cell Challenge Ratings](#cell-challenge-ratings)
3. [Enemy Challenge Ratings](#enemy-challenge-ratings)
4. [Biome-Based Enemy Selection](#biome-based-enemy-selection)
5. [Encounter Building](#encounter-building)
6. [Encounter Difficulty Scaling](#encounter-difficulty-scaling)
7. [Boss Encounters](#boss-encounters)
8. [Technical Reference](#technical-reference)

---

## What is Challenge Rating?

Challenge Rating (CR) is a numerical value that represents how dangerous something is. In DolCon, CR applies to two things:

1. **Cells** - Each area on the map has a CR based on distance from the City of Light
2. **Enemies** - Each enemy type has a fixed CR indicating its power level

When you explore a cell, its CR determines:
- The difficulty tier of encounters (Easy, Medium, Hard, Deadly)
- Which enemies can appear based on their CR
- How many enemies you might face

---

## Cell Challenge Ratings

### Distance from the City of Light

The world's danger level radiates outward from the City of Light. The CR calculation uses a simple principle: **the further you travel from safety, the more dangerous the world becomes**.

```
Cell CR = (Distance from City of Light / Maximum Map Distance) × 20
```

This creates a gradient where:
- CR 0 at the City of Light (complete safety)
- CR increases gradually as you move outward
- CR 20 at the furthest edges of the map

### CR Values and Threat Levels

| Distance from CoL | Approximate CR | Threat Level |
|-------------------|----------------|--------------|
| 0% (City center) | 0 | Safe |
| 10% | 2 | Trivial |
| 25% | 5 | Low |
| 50% | 10 | Moderate |
| 75% | 15 | High |
| 100% (Map edge) | 20 | Extreme |

### Encounter Variation

When you trigger a combat encounter, the cell's base CR may be modified by a random roll:

| Roll (d20) | CR Modifier | Effect |
|------------|-------------|--------|
| 1-5 | No encounter | Combat avoided entirely |
| 6-10 | Base CR | Standard encounter |
| 11-15 | CR × 1.10 | Slightly harder |
| 16-19 | CR × 1.15 | Notably harder |
| 20 | CR × 1.20 | Critical - significantly harder |

---

## Enemy Challenge Ratings

Each enemy in the game has a fixed CR representing its combat strength. Higher CR enemies have better stats, more health, and deal more damage.

### CR by Enemy Category

#### Nature Enemies

| Enemy | CR | Description |
|-------|-----|-------------|
| Corrupted Wolf | 1 | Pack hunter with advantage near allies |
| Thornling | 2 | Plant creature resistant to physical damage |
| Dire Bear | 3 | Massive beast that rages when wounded |
| Lesser Fire Elemental | 4 | Flame creature immune to fire |

#### Human Enemies

| Enemy | CR | Description |
|-------|-----|-------------|
| Bandit | 0.5 | Cowardly outlaw who may flee |
| Dark Cultist | 2 | Fanatical spellcaster |
| Corrupted Soldier | 3 | Well-armored fallen warrior |
| Mercenary Captain | 5 | Elite leader with multiple attacks |

#### Undead Enemies

| Enemy | CR | Description |
|-------|-----|-------------|
| Zombie | 0.25 | Slow but resilient |
| Skeleton Warrior | 1 | Armored undead fighter |
| Corrupted Hound | 2 | Pack hunter with diseased bite |
| Wraith | 5 | Incorporeal spirit with life drain |

#### Demon Enemies

| Enemy | CR | Description |
|-------|-----|-------------|
| Imp | 1 | Small flying demon |
| Possessed Villager | 3 | Human controlled by a demon |
| Demon Beast | 6 | Reckless berserker |
| Hell Knight | 7 | Fallen paladin with dark powers |
| Corrupted Arch-Demon | 12 | **Boss** - Demon lord |

### Experience Points by CR

Defeating enemies awards XP based on their CR, which converts to coin:

| CR | XP Value |
|----|----------|
| 0.25 | 50 |
| 0.5 | 100 |
| 1 | 200 |
| 2 | 450 |
| 3 | 700 |
| 4 | 1,100 |
| 5 | 1,800 |
| 6 | 2,300 |
| 7 | 2,900 |
| 8+ | Increases further |

---

## Biome-Based Enemy Selection

Not all enemies appear everywhere. Each enemy can only spawn in specific biome types that match their nature.

### Biome Types

The game converts map terrain into combat biomes:

| Map Terrain | Combat Biome |
|-------------|--------------|
| Grassland, Savanna | Plains |
| All forest types | Forest |
| Hot deserts | Desert |
| Cold deserts, Ice | Tundra |
| Wetlands, Marshes | Swamp |
| Ocean, Coast | Coastal |
| Mountains, Glaciers | Mountains |
| Corrupted areas | CorruptedLands |
| Ancient ruins | Ruins |
| Volcanic regions | Volcanic |
| Underground areas | Underground |
| City of Light | CityOfLight |

### Enemy Biome Restrictions

Each enemy is limited to specific biomes:

| Enemy | Allowed Biomes |
|-------|----------------|
| Corrupted Wolf | Forest, Plains, CorruptedLands |
| Dire Bear | Forest, Mountains |
| Thornling | Forest, Swamp |
| Lesser Fire Elemental | Volcanic, Ruins |
| Bandit | Forest, Plains, Mountains |
| Dark Cultist | Ruins, CorruptedLands, Underground |
| Corrupted Soldier | CorruptedLands, Ruins |
| Mercenary Captain | Plains, Mountains, CityOfLight |
| Zombie | CorruptedLands, Ruins, Swamp |
| Skeleton Warrior | Ruins, CorruptedLands, Underground |
| Corrupted Hound | CorruptedLands, Swamp |
| Wraith | Ruins, CorruptedLands, Underground |
| Imp | CorruptedLands, Ruins, Volcanic |
| Possessed Villager | CorruptedLands, Plains |
| Demon Beast | CorruptedLands, Volcanic |
| Hell Knight | CorruptedLands, Ruins |
| Corrupted Arch-Demon | CorruptedLands |

### Implications for Exploration

- **Safe biomes**: Plains near the City of Light have weak bandits and wolves
- **Dangerous biomes**: Volcanic and CorruptedLands have the most powerful enemies
- **Unique encounters**: Some enemies only appear in specific terrain types
- **Fallback**: If no enemies match a biome, the system defaults to Plains enemies

---

## Encounter Building

When combat triggers, the game builds an encounter by selecting enemies that fit the target difficulty.

### The Selection Process

1. **Determine target CR** from cell CR and difficulty
2. **Filter enemies** to those available in the current biome
3. **Select enemies** whose combined CR matches the target
4. **Apply group multiplier** based on number of enemies

### Group Size Multiplier

Fighting multiple enemies is harder than their individual CRs suggest:

| Number of Enemies | Effective CR Multiplier |
|-------------------|-------------------------|
| 1 | 1.0× |
| 2 | 1.5× per enemy |
| 3-6 | 2.0× per enemy |
| 7-10 | 2.5× per enemy |

**Example**: Three CR 1 Corrupted Wolves have an effective CR of 6 (3 × 1 × 2.0), not 3.

### Encounter Constraints

- Maximum of 10 enemies per encounter
- Enemies must fit within 1.2× of the target CR (slight over-budget allowed)
- Encounters try to use varied enemy types when available

---

## Encounter Difficulty Scaling

The cell's CR maps to one of four difficulty tiers:

| Cell CR | Difficulty | Encounter Nature |
|---------|------------|------------------|
| 0 - 0.5 | Easy | Minor threat, minimal risk |
| 0.5 - 1.5 | Medium | Moderate challenge, some risk |
| 1.5 - 3.0 | Hard | Significant danger, resource drain |
| 3.0+ | Deadly | Extreme risk, possible defeat |

### XP Budget by Difficulty

The difficulty determines how much total enemy XP the encounter can contain:

| Difficulty | XP per Party Member |
|------------|---------------------|
| Easy | 50 × party level |
| Medium | 100 × party level |
| Hard | 150 × party level |
| Deadly | 200 × party level |

**Example**: A party of 4 at level 1 facing a Hard encounter:
- XP budget = 150 × 1 × 4 = 600 XP
- Could be: 1 Dire Bear (700 XP) or 3 Corrupted Wolves (600 XP)

---

## Boss Encounters

Special boss fights use enhanced encounter rules.

### Boss CR Scaling

- Boss target CR = Party level + 2
- If the boss template CR is lower than target, stats are scaled up
- If already higher, the boss uses its original stats

### Boss Stat Scaling

When a boss needs scaling:

| Stat | Scaling |
|------|---------|
| HP | × scale factor |
| Strength | × √(scale factor) |
| Dexterity | × √(scale factor) |
| Constitution | × √(scale factor) |
| AC | +1 per 1.0 scale factor above 1.0 |
| Mental stats | Unchanged |

### Minions

Boss encounters include additional enemies:
- 1-3 minions based on party size
- Minions are from the same enemy category as the boss
- Example: Arch-Demon boss brings Hell Knights or Demon Beasts

---

## Technical Reference

### Key Files

| File | Purpose |
|------|---------|
| `DolCon.Core/Data/EnemyIndex.cs` | Enemy definitions and CR values |
| `DolCon.Core/Data/EncounterBuilder.cs` | Encounter generation logic |
| `DolCon.Core/Services/BiomeMapper.cs` | Terrain to biome conversion |
| `DolCon.Core/Services/MapService.cs` | Cell CR calculation |
| `DolCon.Core/Services/CombatService.cs` | Combat initiation |

### CR Calculation Formula

```
ChallengeRating = (Distance / MaxDistance) × 20
Rounded to nearest 0.125 (1/8th)
```

Where:
- Distance = Euclidean distance from City of Light coordinates
- MaxDistance = Half the map's total dimension

### XP to CR Conversion

```
< 100 XP  → CR 0.25
< 200 XP  → CR 0.5
< 450 XP  → CR 1
< 700 XP  → CR 2
< 1100 XP → CR 3
< 1800 XP → CR 4
< 2300 XP → CR 5
≥ 2300 XP → CR 5 + (XP - 2300) / 600
```

---

*For combat mechanics and controls, see [Gameplay](GAMEPLAY.md).*

*For enemy details and special abilities, see the Enemies & Challenges section in [Gameplay](GAMEPLAY.md#enemies--challenges).*

*For quick stats, see the [Quick Reference](QUICK_REFERENCE.md).*
