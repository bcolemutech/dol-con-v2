# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DolCon is a role-playing game written in C# (.NET 10.0) with a MonoGame graphical UI. The game features map-based exploration with parties traveling across cells, burgs (settlements), and locations while managing stamina, inventory, and currency. Players can explore locations, engage in turn-based combat, interact with vendors, and manage equipment.

## Project Structure

The solution consists of 4 projects:

```
dol-con-v2/
├── DolCon.Core/           # Shared game logic library (Models, Services, Enums, Data)
├── DolCon.Core.Tests/     # Tests for Core library
├── DolCon.MonoGame/       # MonoGame graphical application (loads world.dol)
├── DolCon.WorldForge/     # CLI that bakes Azgaar exports into canonical world.dol
├── docs/                  # Gameplay docs + WORLD_DOL_FORMAT.md (the world.dol schema contract)
├── version.json           # Semantic versioning
└── Directory.Build.props  # Shared build properties
```

### World Content Pipeline

World content is **baked at design time, not generated at runtime**:

```
Azgaar JSON ──▶ DolCon.WorldForge ──▶ world.dol ──▶ DolCon.MonoGame
 (repo input)     bake (seeded)        (canonical)     (loads & plays)
```

**`world.dol` is a build artifact, not a source file.** It is gitignored and never committed. The committed source is the raw Azgaar export in `DolCon.MonoGame/PrebuiltMaps/` plus the WorldForge code; `DolCon.MonoGame/Worlds/*.world.dol` is generated output.

Building `DolCon.MonoGame` auto-bakes any missing or out-of-date world (the `BakeWorlds` target in its `.csproj`), so a fresh clone can `dotnet run` with no extra steps. The bake takes well under a second and is skipped when nothing changed.

Because it is regenerated rather than versioned, the bake must stay **byte-reproducible**: pass `--generated-at` or set `SOURCE_DATE_EPOCH` to pin the only wall-clock field, and CI (`verify-bake-reproducible`) fails the build if two bakes of the same export differ. Don't introduce unpinned nondeterminism (`DateTime.Now`, unseeded RNG, hash-order-dependent iteration) into the bake path. See `docs/WORLD_DOL_FORMAT.md`.

### DolCon.Core (Shared Library)
Contains all UI-agnostic game logic:
- **Models/**: Player, Party, Item, Scene, Combat models, `Models/World/` (the `DolWorld` canonical schema + `SaveGame`), BaseTypes (raw Azgaar Map data, WorldForge input)
- **Services/**: CombatService, MapService, MoveService, EventService, ShopService, SaveGameService, WorldProvisioningService, WorldBaker
- **Data/**: EnemyIndex (100+ enemies), EncounterBuilder
- **Enums/**: Game enums (Direction, Biome, Equipment, CombatEnums, etc.)
- **Utilities/**: PaginatedList

### DolCon.WorldForge (CLI)
Bakes a raw Azgaar export into a canonical `world.dol`: `worldforge bake <azgaar-export.json> -o <world.dol> [--seed <int>]`. See `docs/WORLD_DOL_FORMAT.md`.

### DolCon.MonoGame (Graphical Application)
MonoGame-based UI:

- **Screens/**: MainMenuScreen, HomeScreen, NavigationScreen, BattleScreen, InventoryScreen, ShopScreen
- **Input/**: InputManager for keyboard handling
- Uses ScreenManager for navigation between screens

## Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the MonoGame application
dotnet run --project DolCon.MonoGame/DolCon.MonoGame.csproj

# Build in Release mode
dotnet build -c Release
```

### Baking a World

Normally you don't — `dotnet build` bakes automatically. To bake by hand:

```bash
# Bake a raw Azgaar export into a canonical world.dol
dotnet run --project DolCon.WorldForge/DolCon.WorldForge.csproj -- \
  bake "DolCon.MonoGame/PrebuiltMaps/Ouvia Full 2023-07-08-13-04.json" \
  -o DolCon.MonoGame/Worlds/Ouvia.world.dol

# Byte-reproducible bake (pins the generatedAt stamp)
SOURCE_DATE_EPOCH=1750000000 dotnet run --project DolCon.WorldForge -- \
  bake "<export>" -o "<out>.world.dol"
```

Bakes are deterministic: `--seed <int>` sets the provisioning seed, and omitting it derives a stable
seed from the Azgaar export. Re-baking the same input therefore reproduces the same world.

### Testing
```bash
# Run all tests
dotnet test

# Run Core tests only
dotnet test DolCon.Core.Tests/DolCon.Core.Tests.csproj

# Run tests with verbose output
dotnet test -v normal

# Run a specific test
dotnet test --filter "FullyQualifiedName~CombatServiceTests"
```

### Restore Dependencies
```bash
# Restore NuGet packages
dotnet restore
```

## Architecture

### Service Layer

Services are located in `DolCon.Core/Services/` and handle core game logic:

- **SaveGameService**: Manages game save/load operations. Contains static properties for global game state: `CurrentWorld` (a `DolWorld`), `Party`, `CurrentCell`, `CurrentBurg`, `CurrentLocation`, `CurrentProvince`, `CurrentState`, `CurrentBiome`, and `CurrentPlayerId`. Save files are a `SaveGame` (`DolWorld` + party + player progress); the shipped, progress-free `world.dol` is loaded for new games.
- **MapService**: Loads a baked `world.dol` (`DolWorld`) from `%APPDATA%/DolCon/Worlds` via `DolWorldSerializer`, installs shipped worlds on first run, and handles direction calculations. It no longer provisions the world at runtime — only player placement happens at load. World provisioning lives in `WorldProvisioningService` and is pre-baked by WorldForge.
- **WorldProvisioningService / WorldBaker**: The single home for world provisioning (City of Light, challenge ratings, location placement, burg sizing), made deterministic via a seeded RNG. `WorldBaker` provisions an Azgaar `Map` and maps it to a `DolWorld`. Consumed by the `DolCon.WorldForge` CLI (`worldforge bake`), not by the game at runtime.
- **PlayerService**: Manages player creation and persistence.
- **MoveService**: Handles party movement between cells, calculates movement costs based on biome, and manages stamina consumption.
- **EventService**: Processes events when entering locations (exploration, rewards, services, combat encounters).
- **CombatService**: Manages turn-based combat including initiative rolls, turn order, attack resolution, damage calculation, and combat state transitions.
- **ShopService**: Handles buying/selling items and equipment at vendor locations. Generates random loot rewards using Chance.NET.
- **ServicesService**: Loads available services from `Resources/Services.json`.
- **ItemsService**: Loads item definitions from `Resources/Items.json`.

### MonoGame Screen Layer

Screens are in `DolCon.MonoGame/Screens/`:

- **MainMenuScreen**: Entry point with New Game, Load Game, and Exit options.
- **CharacterCreationScreen**: D&D-style point-buy character creation for new games.
- **HomeScreen**: Displays party status, current location, and navigation options.
- **NavigationScreen**: Polygon-based movement UI showing neighboring cells and movement costs.
- **WorldMapScreen**: Renders the full world map with polygon cells and fog-of-war.
- **LocationScreen**: Handles entering and exploring a location within a cell or burg.
- **ShopScreen**: Renders shop interactions for buying/selling items and services.
- **BattleScreen**: Renders combat encounters with turn order, health bars, and action selection.
- **InventoryScreen**: Displays player inventory with equipment management.

### Models

Models are in `DolCon.Core/Models/`:

- **World/**: The canonical baked-world schema the game runs on — `DolWorld` (root), `WorldCell`, `WorldBurg`, `WorldLocation`, `WorldState`, `WorldProvince`, `WorldRiver`, plus `Enrichment` (reserved authored-content containers) and `SaveGame`. `DolWorldSerializer` is the **single source of truth** for JSON options; both the baker and the loader must use it.
- **BaseTypes/**: Raw Azgaar export shapes (Map, Cell, Burg, State, Province, etc.). These are **WorldForge input only** — the game no longer touches them at runtime. Don't add runtime dependencies on them.
- **Player**: Represents a character with inventory and currency (coin subdivided into copper/silver/gold).
- **Party**: Contains multiple players, tracks current Cell/Burg/Location, and manages stamina for movement.
- **Location / LocationType**: `Location` is an explorable place with discovery/exploration tracking. In `world.dol` it is stored slim as a `WorldLocation` (`id`, `name`, `typeKey`, `rarity`); the full template is rehydrated at load time from the static `LocationTypes.Types` catalog, so the catalog is never duplicated into the world file.
- **Item**: Equipment and goods with tags determining type (weapon, armor, etc.) and rarity.
- **Scene**: Manages the current interaction state (shop selections, event type, completion status).
- **Combat/**: Contains combat system models including `Enemy`, `CombatState`, `PlayerCombatant`, `CombatEntity`, `AttackResult`, and `CombatSupport`.

### Game Flow

1. Application starts via MonoGame's `Game1` class which initializes the ScreenManager.
2. MainMenuScreen presents New Game or Load Game options.
3. New Game loads a baked `world.dol` into `SaveGameService.CurrentWorld` and places the player; the world is no longer re-provisioned at runtime, so City of Light, challenge ratings, and locations are identical run-to-run.
4. User navigates between screens using keyboard input handled by InputManager.
5. Movement handled by `MoveService`, which updates Party position and triggers `EventService`.
6. Events determine scene type (exploration rewards, shop interaction, services, combat).
7. Shops use `ShopService` to present buying/selling interfaces.
8. Game state saves on exit.

### Key Data Patterns

- **Static State**: `SaveGameService.CurrentWorld` (a `DolWorld`) and `SaveGameService.Party` are static and accessed globally throughout services. The game depends only on `DolWorld`; raw Azgaar shapes (`Models/BaseTypes`) are now an input to WorldForge, not used at runtime.
- **Scene Management**: The Scene model acts as a state machine for multi-step interactions, tracking completion and selections.
- **Currency System**: Coin values use integer math: 1 gold = 100 silver = 1000 copper = 1000 coin.

### External Resources

- **Items.json**: Defines all items/equipment with tags (TagType enum) for categorization.
- **Services.json**: Defines available services at locations (inns, vendors, etc.).
- **Worlds/**: Baked `world.dol` files — **generated build output, gitignored**, produced by the `BakeWorlds` MSBuild target. Installed to `%APPDATA%/DolCon/Worlds` at startup, refreshed when the shipped copy is newer so a re-bake actually reaches the player.
- **PrebuiltMaps/**: Raw Azgaar exports, committed. These are the true world source and WorldForge's only input; not copied to the game at runtime.

### Combat System

The game features D&D 5e-inspired turn-based combat located in `DolCon.Core/Data/`:

- **EnemyIndex**: Central repository of 100+ enemies organized by `EnemyCategory` (Nature, Human, Undead, Demon) and `EnemySubcategory`. Enemies have CR ratings, biome restrictions, loot drops, and behavior AI. Initialized at startup.
- **EncounterBuilder**: Generates dynamic encounters based on party level and `EncounterDifficulty` (Easy, Medium, Hard, Deadly). Supports boss encounters with `BuildBossEncounter()`.
- **BiomeMapper**: Converts map biomes to combat `BiomeType` values, affecting which enemies can spawn.

Combat flow:
1. `EventService` triggers combat encounters during exploration.
2. `CombatService` manages initiative, turn order, and action resolution.
3. `BattleScreen` renders the combat UI with turn indicators and combat log.
4. Loot and experience awarded on victory.

### Testing

Tests use xUnit, NSubstitute for mocking, and FluentAssertions. Located in `DolCon.Core.Tests/`. Key test files include `CombatServiceTests` (extensive combat mechanics), `MapServiceTests` (direction calculation), `PlayerTests`, and `AttackResultTests`. World-pipeline coverage lives in `DolCon.Core.Tests/World/`: `DolWorldSerializationTests` (lossless round-trip, human-readable enums, `schemaVersion`, `typeKey` rehydration), `WorldBakerTests`, `WorldProvisioningServiceTests` (determinism under a fixed seed), and `GameWorldStateTests`.

GitHub Actions runs tests on Ubuntu, Windows, and macOS to ensure cross-platform compatibility.

## Important Notes

- Game state is managed through static properties on SaveGameService rather than passed parameters.
- The world is **authored, not re-rolled**. Anything that should be identical run-to-run belongs in the bake (WorldForge), not at runtime. Adding a `new Random()` to a runtime code path re-introduces the drift Phase 2 removed.
- The canonical `world.dol` is immutable and progress-free. Player progress (`ExploredPercent`, `Discovered`, `LastExplored`) and party data live in the `SaveGame`; progress fields are `[JsonIgnore(WhenWritingDefault)]` so a fresh bake omits them.
- Schema changes follow `docs/WORLD_DOL_FORMAT.md`: additive optional fields don't bump `schemaVersion`; renames, removals, and semantic changes do.
- Movement costs vary by biome and are calculated in MoveService based on world cell data.
- The UI is fully keyboard-driven with no mouse support.
- Inventory is limited to 50 items per player.
- Location exploration uses a percentage system that increases over time.
- Encounter difficulty scales with a cell's distance from the **City of Light** (`WorldProvisioningService.CalculateChallengeRating`, CR 0–20), so challenge rises the further the party travels from civilization. This is baked into `world.dol`.
