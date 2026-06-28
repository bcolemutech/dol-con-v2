# The `world.dol` Canonical World Format

`world.dol` is the **baked, canonical "Dominion of Light" world** that
[WorldForge](https://github.com/bcolemutech/dol-con-v2/issues/72) produces from an
[Azgaar Fantasy-Map-Generator](https://github.com/Azgaar/Fantasy-Map-Generator) export, and that the
game loads at runtime. It replaces the old flow where the game parsed the full raw Azgaar JSON and
re-provisioned the world (City of Light, challenge ratings, locations, burg sizes) every playthrough
with a non-seeded `new Random()`.

This document describes the **schema contract** defined in Phase 0 (#73). It is the foundation the
later phases build on:

- **Phase 1 (#74):** WorldForge bakes an Azgaar export into a `world.dol`.
- **Phase 2 (#75):** the game loads `world.dol` and retires runtime provisioning.
- **Phases 3–5 (#76–#78):** AI-assisted enrichment fills the reserved containers described below.

## Where the model lives

All types live in `DolCon.Core/Models/World/` so both WorldForge and the game reference one
definition. The root is `DolWorld`. Serialization goes through `DolWorldSerializer`, which is the
**single source of truth** for the JSON options — both the baker and the loader must use it.

## Serialization

- **Library:** `System.Text.Json`.
- **Options (`DolWorldSerializer.Options`):**
  - `PropertyNamingPolicy = CamelCase` — `ChallengeRating` → `challengeRating`.
  - `JsonStringEnumConverter` — enums are written as **human-readable strings** (`"size": "City"`,
    `"rarity": "Common"`, `"status": "Authored"`), never integers.
  - `WriteIndented = true` — pretty-printed.
  - `DefaultIgnoreCondition = WhenWritingNull` — null containers (e.g. an absent `enrichment` block)
    are omitted entirely.

These choices keep a baked world **pleasant to author or edit by hand or by an AI agent**, which
phases 3–5 rely on.

## Versioning strategy

The root carries `schemaVersion` (currently **`1`**).

- Add the field to every baked world.
- **Additive, backward-compatible changes** (new optional fields, new enrichment types) do **not**
  bump the version — older worlds still deserialize because missing properties fall back to defaults.
- **Breaking changes** (renames, removals, semantic changes) bump `schemaVersion`. Loaders should
  read the version first and migrate or reject worlds they don't understand.

## What the schema contains

The schema is deliberately three things and nothing else:

### 1. The Azgaar subset the game actually reads

Audited from `DolCon.Core/Models/BaseTypes/` against every service and screen — only fields the game
consumes survive.

| `DolWorld` member | Source (Azgaar `Map`) | Notes |
| --- | --- | --- |
| `vertices[].p` | `vertices[].p` | Cell-polygon rendering. |
| `cells[].id / vertexIndices / neighbors / center` | cell `i / v / c / p` | Geometry + navigation. |
| `cells[].area / pop / biome` | cell `area / pop / biome` | Feed derived `CellSize` / `PopDensity` / `Biome`. |
| `cells[].state / province / burg` | cell `state / province / burg` | Lookups into the lists below. |
| `burgs[].id / name / cell / x / y` | burg `i / name / cell / x / y` | Identity + map placement. |
| `burgs[].hasPort / hasCitadel / hasPlaza / hasWalls / hasShanty / hasTemple` | burg `port / citadel / plaza / walls / shanty / temple` (nullable 0/1 → bool) | Settlement features. |
| `states[].id / name / fullName` | state `i / name / fullName` | Minimal; the game only surfaces names. |
| `provinces[].id / name / fullName` | province `i / name / fullName` | `fullName` is what the nav UI shows. |
| `rivers[]` (slim) | rivers | Not read today; retained for future rendering. |
| `coords` | coords | Retained for future rendering. |
| `biomes[]` | `biomes.name[]` | Biome-name lookup table. |

### 2. Provisioning baked in (was re-rolled each play)

These used to be computed in `MapService.ProvisionMap()` and thrown away. They are now part of the
canonical world:

| `DolWorld` member | Replaces runtime computation |
| --- | --- |
| `burgs[].isCityOfLight` | Highest-population burg flagged as the City of Light. |
| `cells[].challengeRating` | Encounter difficulty from distance to the City of Light. |
| `cells[].locations` / `burgs[].locations` | Generated `Location`s. |
| `burgs[].population` / `burgs[].size` | Adjusted population and resulting `BurgSize`. |

**Locations are stored by reference, not by value.** A `WorldLocation` keeps only `id`, `name`,
`typeKey`, and the rolled `rarity`. `typeKey` is the unique `LocationType.Type` string (e.g.
`"tavern"`); the full template is rehydrated at load time from the static `LocationTypes.Types`
catalog. This keeps the file slim and avoids duplicating the catalog into every location.

### 3. Reserved enrichment containers (empty for now)

Every enrichable object (`DolWorld`, `WorldCell`, `WorldBurg`, `WorldLocation`) has an **optional**
`enrichment` block. It is `null`/omitted by default, so Phase 1 can bake a fully valid world with no
authored content. Phases 3–5 fill it.

- `status` — `EnrichmentStatus` (`Empty` | `Draft` | `Authored` | `Reviewed`); the per-object
  flag/worklist marker Phase 3 drives.
- `npcs`, `history`, `pointsOfInterest` — AI-friendly content lists (minimal placeholder shapes for
  now, expanded in Phase 4).
- `assets` — references to **external** art. The AI authors the `spec` (prompt/spec) and manages the
  manifest; the actual image is procured externally and its `path` filled in later (Phase 5).
- `notes` — free-form authoring notes.

## What was deliberately dropped vs. the raw Azgaar export

The game never reads these, so they are **not** in `world.dol`:

- **Heraldry:** `Coa`, `Charge`, `Ordinary`, `Division`.
- **Biome detail:** `biomesMartix`, `habitability`, `iconsDensity`, `icons`, `cost` (only the
  name lookup is kept).
- **Generation helpers:** `nameBases`, `cultures`.
- **Unused state data:** `military` / `U`, `campaigns`, `diplomacy`, `neighbors`, `pole`, plus
  `markers`, and most of `settings` / `options`.
- **Unused cell fields:** `g`, `h`, `f`, `t`, `haven`, `harbor`, `fl`, `r`, `conf`, `s`, `culture`,
  `road`, `crossroad`.
- **Unused burg fields:** `feature`, `capital`, `type`, `coa`.

It also deliberately **excludes per-playthrough player progress**. The canonical world is immutable;
exploration/discovery state (`ExploredPercent`, `Discovered`, `LastExplored`) and player/party data
(`Party`, `CurrentPlayerId`) live in the **save game**, not in `world.dol`.

## Example

A trimmed `world.dol` (one cell, one burg with an authored enrichment block):

```json
{
  "schemaVersion": 1,
  "info": {
    "name": "Sample World",
    "sourceSeed": "123456",
    "sourceAzgaarVersion": "1.99",
    "generatedAt": "2026-06-28T12:00:00Z",
    "generatorVersion": "0.1.0"
  },
  "coords": { "latT": 90, "latN": 60, "latS": 20, "lonT": 180, "lonW": -40, "lonE": 40 },
  "vertices": [ { "p": [0, 0] }, { "p": [10, 0] }, { "p": [5, 10] } ],
  "cells": [
    {
      "id": 0,
      "vertexIndices": [0, 1, 2],
      "neighbors": [1],
      "center": [5, 3],
      "area": 120,
      "pop": 4.2,
      "biome": 2,
      "state": 1,
      "province": 1,
      "burg": 1,
      "challengeRating": 0,
      "locations": [
        { "id": "11111111-1111-1111-1111-111111111111", "name": "ruins", "typeKey": "ruins", "rarity": "Uncommon" }
      ]
    }
  ],
  "burgs": [
    {
      "id": 1,
      "name": "Lumengarde",
      "cell": 0,
      "population": 1050,
      "size": "City",
      "isCityOfLight": true,
      "x": 5,
      "y": 3,
      "hasPort": true,
      "hasCitadel": false,
      "hasPlaza": false,
      "hasWalls": false,
      "hasShanty": false,
      "hasTemple": true,
      "locations": [
        { "id": "22222222-2222-2222-2222-222222222222", "name": "Lumengarde tavern", "typeKey": "tavern", "rarity": "Common" }
      ],
      "enrichment": {
        "status": "Authored",
        "npcs": [ { "id": "npc-1", "name": "Mayor Voss", "role": "Mayor", "description": "Stern but fair." } ],
        "history": [],
        "pointsOfInterest": [],
        "assets": [
          { "key": "lumengarde-banner", "kind": "icon", "spec": "A radiant sunburst banner.", "status": "Draft" }
        ]
      }
    }
  ],
  "states": [ { "id": 1, "name": "Aurelia", "fullName": "Kingdom of Aurelia" } ],
  "provinces": [ { "id": 1, "name": "Dawnmoor", "fullName": "Province of Dawnmoor" } ],
  "rivers": [ { "id": 1, "name": "Silverflow", "cells": [0, 1], "width": 2.5, "length": 40, "source": 0, "mouth": 1 } ],
  "biomes": ["Marine", "Hot desert", "Temperate deciduous forest"]
}
```

## Tests

Round-trip coverage lives in `DolCon.Core.Tests/World/DolWorldSerializationTests.cs`: a full
lossless round-trip (`serialize → deserialize → BeEquivalentTo`), a minimal world that omits null
enrichment, human-readable enum + `schemaVersion` assertions, and `typeKey` catalog rehydration.
