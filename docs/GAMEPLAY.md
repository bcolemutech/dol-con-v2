# DolCon - Dominion of Light

## Complete Gameplay Documentation

Welcome to DolCon, a turn-based role-playing game set in a dark fantasy world threatened by spreading corruption. This document explains everything you need to know to play the game.

---

## Table of Contents

1. [Story & Setting](#story--setting)
2. [Characters & Attributes](#characters--attributes)
3. [Core Gameplay Loop](#core-gameplay-loop)
4. [Exploration & Movement](#exploration--movement)
5. [Combat System](#combat-system)
6. [Enemies & Challenges](#enemies--challenges)
7. [Economy & Equipment](#economy--equipment)
8. [Controls & Interface](#controls--interface)

---

## Story & Setting

### The World

The Dominion of Light stands as civilization's last great hope against an encroaching darkness. At its heart lies the **City of Light**, the largest settlement and safest haven in the realm. From this bastion of safety, adventurers venture outward into a world increasingly touched by corruption.

### The Premise

You begin your journey in the City of Light, where the forces of good still hold sway. As you travel outward from this sanctuary, the land grows more dangerous. The corruption has twisted the natural world, turned soldiers against their oaths, raised the dead from their graves, and opened pathways for demons to enter the realm.

### The Corruption

The spreading evil manifests throughout the land:

- **Corrupted creatures** roam the wilderness, once-noble beasts now twisted into feral monsters
- **Former defenders** have fallen under the evil influence, becoming mindless warriors
- **The undead** rise in areas where death has been perverted
- **Demons** emerge in the most corrupted regions, serving dark masters

The further you travel from the City of Light, the stronger the corruption grows. Challenge and reward both increase with distance from civilization.

### The Factions

Four categories of enemies represent different aspects of the corruption:

| Category | Description | Theme |
|----------|-------------|-------|
| **Nature** | Beasts, plants, and elementals | The natural world made dangerous |
| **Human** | Bandits, cultists, corrupted soldiers | Humanity's capacity for darkness |
| **Undead** | Zombies, skeletons, wraiths | Death perverted and denied rest |
| **Demon** | Imps, possessed beings, demon lords | Pure evil incarnate |

---

## Characters & Attributes

### The Six Attributes

Every character has six core attributes that define their capabilities:

| Attribute | Abbr. | Effect in Combat |
|-----------|-------|------------------|
| **Strength** | STR | Attack rolls and melee damage |
| **Dexterity** | DEX | Initiative order |
| **Constitution** | CON | Hit points and survivability |
| **Intelligence** | INT | Reserved for future abilities |
| **Wisdom** | WIS | Reserved for future abilities |
| **Charisma** | CHA | Reserved for future abilities |

**Modifier Calculation**: Your modifier for any attribute equals (Attribute - 10) / 2, rounded down.

Example: A Strength of 15 gives a +2 modifier. A Dexterity of 8 gives a -1 modifier.

### Armor Class

Your Armor Class (AC) determines how hard you are to hit. The base AC is 10, modified by:

| Equipment Slot | AC Bonus |
|----------------|----------|
| Body Armor | +3 |
| Shield | +2 |
| Leg Armor | +2 |
| Head Armor | +1 |
| Hand Armor | +1 |
| Foot Armor | +1 |

Item rarity adds additional bonuses to armor pieces (except shields, which are always +2).

### Stamina

Stamina is your party's most important resource:

- **Movement** consumes stamina based on terrain difficulty
- **Exploration** costs stamina to search areas
- **Combat** uses stamina as hit points (stamina percentage x 100 = HP)
- **Recovery** happens through rest, camping, or lodging services

When stamina runs low, you become vulnerable in combat. Manage it carefully.

---

## Core Gameplay Loop

### The Cycle

1. **Explore** the world from the Home screen
2. **Navigate** to new cells, burgs, or locations
3. **Encounter** challenges through exploration
4. **Combat** enemies when they appear
5. **Collect** rewards and loot
6. **Rest** to recover stamina
7. **Return** to the cycle

### Rewards

Successful encounters provide:

- **Coin**: Experience points convert directly to currency (2 XP = 1 coin)
- **Loot**: Enemies may drop equipment based on their loot tables
- **Exploration**: Victory commits your exploration progress (MonoGame version)

### Progression

Characters grow stronger through:

- **Better equipment**: Higher rarity items provide better stats
- **Exploration**: Discovering new areas and completing locations
- **Resources**: Accumulating wealth to purchase upgrades

---

## Exploration & Movement

### World Structure

The world consists of three nested layers:

1. **Cells**: Grid tiles representing terrain areas with biomes
2. **Burgs**: Settlements containing shops, services, and safe locations
3. **Locations**: Specific places to explore within cells or burgs

### Biomes and Movement Costs

Different terrain types require different amounts of stamina to traverse:

| Biome | Difficulty | Description |
|-------|------------|-------------|
| Grassland | Easy | Open plains, minimal obstacles |
| Forest | Moderate | Dense trees slow progress |
| Mountains | Hard | Steep terrain requires effort |
| Swamp | Hard | Treacherous footing |
| Volcanic | Very Hard | Extreme heat and terrain |
| Corrupted Lands | Very Hard | The corruption itself drains vitality |

### Exploration Mechanics

Each location and cell tracks an **exploration percentage**:

- Start at 0% when first discovered
- Each exploration attempt increases the percentage
- Higher exploration reveals more of an area's secrets
- Some locations have special events at certain exploration levels

### Camping

When in the wilderness with low stamina, you can camp to recover:

- Camping restores stamina when below 50%
- Available in cells (wilderness), not in burgs or locations
- No cost, but provides basic recovery only

### Services

Burgs offer services that enhance your capabilities:

| Service | Effect |
|---------|--------|
| **Lodging** | Superior stamina recovery based on quality |
| **Buy** | Purchase equipment and supplies |
| **Sell** | Sell unwanted items for coin |

*Note: Healing and Repair services are planned but not yet available.*

---

## Combat System

### Overview

Combat in DolCon is turn-based, with initiative determining the order of actions. The system draws inspiration from tabletop RPGs, using dice rolls to resolve attacks.

### Starting Combat

Combat begins when you encounter enemies during exploration:

1. **Initiative Roll**: All combatants roll a 20-sided die plus their Dexterity modifier
2. **Turn Order**: Combatants act from highest to lowest initiative
3. **Ties**: Random determination for equal initiatives

### Combat Actions

On your turn, you have three options:

#### Attack

1. Select a target from the enemy list
2. Roll to hit: d20 + Strength modifier versus target's Armor Class
3. On hit, deal damage based on your weapon and rarity bonuses

**Attack Resolution**:
- Roll at or above the target's AC = Hit
- Roll a natural 20 = Critical Hit (double damage)
- Roll a natural 1 = Automatic miss

**Damage Calculation**:
- Unarmed: 2 base damage
- One-handed weapon: 4 base damage
- Two-handed weapon: 6 base damage
- Add (rarity level x 2) bonus damage

#### Defend

Take a defensive stance:

- Gain +2 to your Armor Class until your next turn
- Useful when low on health or facing powerful enemies

#### Flee

Attempt to escape combat:

- **Only available on the first turn** of combat
- Success ends the encounter immediately
- Costs 5% stamina as a penalty
- Exploration progress is NOT committed when fleeing

### Combat Outcomes

| Result | Effect |
|--------|--------|
| **Victory** | Restore 50% of lost stamina, earn coin and loot. In MonoGame, also commits exploration progress. |
| **Defeat** | Suffer 50% stamina penalty, combat ends |
| **Fled** | Lose 5% stamina, exploration not committed |

### Combat Tips

- **Watch your health**: Defending when low can save you
- **Target wisely**: Eliminate weaker enemies first to reduce incoming damage
- **Know when to flee**: A strategic retreat is better than defeat
- **Manage stamina**: Don't enter combat with low stamina

---

## Enemies & Challenges

### Challenge Rating

Every enemy has a Challenge Rating (CR) indicating their power level:

| CR Range | Threat Level | Typical Location |
|----------|--------------|------------------|
| 0.25 - 1 | Trivial | Near City of Light |
| 2 - 3 | Low | Close to civilization |
| 4 - 5 | Moderate | Moderate distance |
| 6 - 7 | High | Far from safety |
| 8+ | Deadly | Deep wilderness |
| 12+ | Boss | Corrupted heartlands |

### Encounter Difficulty

Encounters are scaled to party strength:

| Difficulty | Description |
|------------|-------------|
| **Easy** | Minor challenge, little resource cost |
| **Medium** | Moderate threat requiring tactics |
| **Hard** | Significant danger, may drain resources |
| **Deadly** | Extreme risk, potential for defeat |

### Enemy Roster

#### Nature Enemies

Creatures of the natural world, many twisted by corruption.

| Enemy | CR | Description |
|-------|-----|-------------|
| **Corrupted Wolf** | 1 | A once-noble wolf with glowing red eyes, hunting in packs. Has Pack Tactics advantage when allies are near. |
| **Dire Bear** | 3 | A massive bear driven mad by corruption. Enters a rage when below half health, dealing increased damage. |
| **Thornling** | 2 | A twisted plant creature with razor-sharp thorns. Resistant to poison and physical damage, vulnerable to fire. Reflects damage to attackers. |
| **Lesser Fire Elemental** | 4 | A swirling mass of flames with malevolent purpose. Immune to fire and poison, vulnerable to cold. Inflicts burning status. |

#### Human Enemies

Those who have chosen darkness or been twisted by it.

| Enemy | CR | Description |
|-------|-----|-------------|
| **Bandit** | 0.5 | A desperate outlaw. Cowardly behavior, may flee. Drops gold coins and occasionally weapons. |
| **Dark Cultist** | 2 | A fanatic wielding forbidden dark magic. Can cast necrotic spells. Resistant to fear effects. |
| **Corrupted Soldier** | 3 | Once a defender of the realm, now a mindless warrior. Well-armored with martial training. Resistant to necrotic damage. |
| **Mercenary Captain** | 5 | A skilled warrior for hire, loyal only to coin. Elite enemy with leadership abilities, healing, and extra attacks. |

#### Undead Enemies

The dead that will not rest, corrupted by dark forces.

| Enemy | CR | Description |
|-------|-----|-------------|
| **Zombie** | 0.25 | A reanimated corpse with mindless hunger. Immune to poison, vulnerable to radiant damage. May survive lethal damage through Undead Fortitude. |
| **Skeleton Warrior** | 1 | Animated bones in rusted armor. Immune to poison, resistant to cold and necrotic. Vulnerable to bludgeoning. May reassemble after destruction. |
| **Corrupted Hound** | 2 | A nightmarish undead beast with rotting flesh. Pack hunter with poisonous diseased bite. Vulnerable to radiant damage. |
| **Wraith** | 5 | A spectral creature of pure malevolence. Incorporeal, can pass through walls. Life Drain attacks reduce maximum HP. Weakness to sunlight and radiant damage. |

#### Demon Enemies

Pure evil from beyond the mortal realm.

| Enemy | CR | Description |
|-------|-----|-------------|
| **Imp** | 1 | A small, mischievous demon with wings and barbed tail. Can shapechange, sees through magical darkness, and flies. Immune to fire and poison, vulnerable to radiant. |
| **Possessed Villager** | 3 | A human controlled by a demonic entity, eyes burning with unholy fire. Enhanced strength, fear aura, resistant to fire and necrotic. |
| **Demon Beast** | 6 | A hulking creature of muscle and fury. Attacks recklessly with advantage but grants advantage to attackers. Can breathe fire and rampage on kills. |
| **Hell Knight** | 7 | A fallen paladin consumed by demonic power. Elite enemy with corrupted paladin abilities, aura of despair, and dark smite attacks. Heavily armored (AC 18). |
| **Corrupted Arch-Demon** | 12 | **BOSS** - A towering demon lord commanding the corrupting forces. 250 HP, AC 19. Has Legendary Resistance (auto-succeed saves 3/day), Corruption Aura (passive necrotic damage), Multi-Attack (3 attacks), can summon demons, and regenerates unless damaged by radiant. Drops guaranteed Demonic Core plus chance for Legendary Weapon and Ancient Artifact. |

### Damage Types and Resistances

Enemies may have special defenses:

- **Resistance**: Takes reduced damage from that type
- **Immunity**: Takes no damage from that type
- **Vulnerability**: Takes increased damage from that type

**Common Patterns**:
- Undead are immune to poison, vulnerable to radiant
- Demons resist fire, vulnerable to radiant
- Plants resist poison, vulnerable to fire

---

## Economy & Equipment

### Currency

The realm uses a three-tier currency system:

| Denomination | Value | Icon |
|--------------|-------|------|
| **Copper** | 1 | Base unit |
| **Silver** | 10 copper | |
| **Gold** | 100 silver (1000 copper) | |

Currency displays as `gold|silver|copper`. Example: `2|50|3` means 2 gold, 50 silver, 3 copper.

### Earning Coin

- **Combat Victory**: XP earned converts to coin at 2:1 ratio
- **Selling Items**: Shops buy items at 50% of their value
- **Exploration**: Some locations contain treasure

### Item Rarity

Items come in five quality tiers:

| Rarity | Color | Effect |
|--------|-------|--------|
| **Common** | Grey | Base stats |
| **Uncommon** | Green | Slightly improved |
| **Rare** | Blue | Notably better |
| **Epic** | Purple | Significantly powerful |
| **Legendary** | Gold | Exceptional items |

Higher rarity provides:
- +1 AC per rarity level on armor
- +2 damage per rarity level on weapons

### Equipment Slots

Characters can equip items in these slots:

| Slot | Purpose | Notes |
|------|---------|-------|
| Head | Helmets, caps | +1 base AC |
| Body | Armor, robes | +3 base AC |
| Legs | Greaves, pants | +2 base AC |
| Feet | Boots, shoes | +1 base AC |
| Hands | Gloves, gauntlets | +1 base AC |
| One-Handed | Swords, axes | 4 base damage, allows shield |
| Two-Handed | Greatswords, polearms | 6 base damage, no shield |
| Shield | Shields | +2 AC, requires one-handed weapon |

### Inventory

- Each character can carry up to **50 items**
- Manage inventory through the Inventory screen
- Sell unwanted items at shops to make room

### Shopping

At vendor locations:

1. **Buying**: Browse available items, purchase if you have enough coin
2. **Selling**: Select items from your inventory to sell at 50% value
3. **Lodging**: Pay for quality rest that restores more stamina

---

## Controls & Interface

### Screen Navigation

| Key | Screen | Description |
|-----|--------|-------------|
| H | Home | Party status and location overview |
| N | Navigation | 3x3 grid for cell movement |
| L | Location | Location selection and entry |
| I | Inventory | Item and equipment management |
| M | Map | Opens the world map image |
| Esc | Exit/Back | Exit or return to previous screen |

### Navigation Screen Controls

The Navigation screen displays a 3x3 spatial grid showing your current cell in the center and adjacent cells around it. Each cell shows its biome color, name, exploration %, and burg indicator.

| Key | Action |
|-----|--------|
| 1-9 | Move to adjacent cell (numpad layout: 7=NW, 8=N, 9=NE, 4=W, 6=E, 1=SW, 2=S, 3=SE) |
| E | Explore current cell |
| B | Enter nearby burg |
| C | Camp (when in wilderness) |
| L | Open Location screen |

### Location Screen Controls

The Location screen shows discovered locations within your current cell or burg.

| Key | Action |
|-----|--------|
| 1-9 | Enter selected location |
| Up/Down | Navigate location list |
| E | Explore current location |
| C | Camp (when at explorable location) |
| N | Return to Navigation screen |
| Esc | Leave current location |

### Combat Controls

During battle:

| Key | Action |
|-----|--------|
| A | Attack selected target (executes immediately) |
| D | Defend (gain +2 AC) |
| F | Flee (first turn only) |
| W/S or Arrow Keys | Select target |
| Any key | Continue after result screen |

### Inventory Controls

In the inventory screen:

| Key | Action |
|-----|--------|
| W/S or Arrow Keys | Select item |
| A/Z | Previous/Next page |
| E | Equip or unequip selected item |
| D | Delete selected item |

### Platform Differences

| Platform | Modifier Key |
|----------|--------------|
| Windows/Linux | Alt |
| macOS | Option |

---

## Quick Reference Card

### Combat Formula

```
Attack Roll = d20 + Strength Modifier
Hit if Roll >= Target AC
Natural 20 = Critical (2x damage)
Natural 1 = Auto-miss

Damage = Weapon Base + (Rarity x 2)
  Unarmed: 2
  One-handed: 4
  Two-handed: 6
```

### AC Formula

```
AC = 10 + Armor Bonuses

Body: +3, Shield: +2, Legs: +2
Head: +1, Hands: +1, Feet: +1
Add rarity bonus to armor (not shields)
```

### Stamina

```
Movement: Cost varies by biome
Combat HP = Stamina% x 100
Victory: Recover 50% of lost stamina
Defeat: Lose 50% stamina
Flee: Lose 5% stamina
```

---

*For terminology definitions, see the [Glossary](GLOSSARY.md).*

*For your first 30 minutes of gameplay, see the [New Player Guide](NEW_PLAYER_GUIDE.md).*

*For a one-page summary, see the [Quick Reference](QUICK_REFERENCE.md).*
