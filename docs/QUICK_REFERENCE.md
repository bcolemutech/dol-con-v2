# DolCon Quick Reference

## Controls

| Key | Action |
|-----|--------|
| **H** | Home Screen |
| **N** | Navigation |
| **I** | Inventory |
| **M** | Open Map |
| **Esc** | Exit Game |

### Navigation

| Key | Action |
|-----|--------|
| 0-9 | Select direction/location |
| Alt/Option + 0-9 | Extended selection |
| Enter | Explore |
| B | Enter burg |
| L | Leave |
| C | Camp |

### Combat

| Key | Action |
|-----|--------|
| A | Attack |
| D | Defend (+2 AC) |
| F | Flee (turn 1 only) |
| W/S | Select target |
| Enter | Confirm |

### Inventory

| Key | Action |
|-----|--------|
| W/S | Select item |
| A/Z | Page up/down |
| E | Equip/Unequip |
| D | Delete item |

---

## Combat Formulas

```
Attack = d20 + STR mod vs AC
Nat 20 = Critical (2x damage)
Nat 1 = Auto miss

Damage = Base + (Rarity × 2)
  Unarmed: 2
  1-Hand:  4
  2-Hand:  6
```

---

## Armor Class

| Slot | Base AC |
|------|---------|
| Body | +3 |
| Shield | +2 |
| Legs | +2 |
| Head | +1 |
| Hands | +1 |
| Feet | +1 |

*Add rarity bonus to each*

---

## Item Rarity

| Rarity | Color |
|--------|-------|
| Common | Grey |
| Uncommon | Green |
| Rare | Blue |
| Epic | Purple |
| Legendary | Gold |

---

## Currency

**1 Gold = 100 Silver = 1000 Copper**

Display: `gold|silver|copper`

---

## Stamina

- **Combat HP** = Stamina% × 100
- **Victory**: Recover 50% lost
- **Defeat**: Lose 50%
- **Flee**: Lose 5%

---

## Enemy Categories

| Category | Theme | Vulnerability |
|----------|-------|---------------|
| Nature | Beasts, plants | Fire (plants) |
| Human | Bandits, cultists | — |
| Undead | Zombies, wraiths | Radiant |
| Demon | Imps, demons | Radiant |

---

## Challenge Rating

| CR | Threat |
|----|--------|
| 0-1 | Trivial |
| 2-3 | Low |
| 4-5 | Moderate |
| 6-7 | High |
| 8+ | Deadly |
| 12+ | Boss |

*Danger increases with distance from City of Light*

---

## Gameplay Loop

```
Explore → Navigate → Encounter → Combat → Loot → Rest → Repeat
```

---

*Full docs: [GAMEPLAY.md](GAMEPLAY.md) | [NEW_PLAYER_GUIDE.md](NEW_PLAYER_GUIDE.md) | [GLOSSARY.md](GLOSSARY.md)*
