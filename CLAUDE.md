# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DolCon is a role-playing game written in C# (.NET 10.0) with a MonoGame graphical UI. The game features map-based exploration with parties traveling across cells, burgs (settlements), and locations while managing stamina, inventory, and currency. Players can explore locations, engage in turn-based combat, interact with vendors, and manage equipment.

## Project Structure

The solution consists of 3 projects:

```
dol-con-v2/
├── DolCon.Core/           # Shared game logic library (Models, Services, Enums, Data)
├── DolCon.Core.Tests/     # Tests for Core library
├── DolCon.MonoGame/       # MonoGame graphical application
├── version.json           # Semantic versioning
└── Directory.Build.props  # Shared build properties
```

### DolCon.Core (Shared Library)
Contains all UI-agnostic game logic:
- **Models/**: Player, Party, Item, Location, Scene, Combat models, BaseTypes (Map data)
- **Services/**: CombatService, MapService, MoveService, EventService, ShopService, SaveGameService
- **Data/**: EnemyIndex (100+ enemies), EncounterBuilder
- **Enums/**: Game enums (Direction, Biome, Equipment, CombatEnums, etc.)
- **Utilities/**: PaginatedList

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

- **SaveGameService**: Manages game save/load operations. Contains static properties for global game state: `CurrentMap`, `Party`, `CurrentCell`, `CurrentBurg`, `CurrentLocation`, `CurrentProvince`, `CurrentState`, `CurrentBiome`, and `CurrentPlayerId`.
- **MapService**: Loads map JSON files from `%APPDATA%/DolCon/Maps`, handles direction calculations between coordinates, and generates location types for cells and burgs.
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
- **HomeScreen**: Displays party status, current location, and navigation options.
- **NavigationScreen**: Handles movement UI with directional navigation and movement costs.
- **ShopScreen**: Renders shop interactions for buying/selling items and services.
- **BattleScreen**: Renders combat encounters with turn order, health bars, and action selection.
- **InventoryScreen**: Displays player inventory with equipment management.

### Models

Models are in `DolCon.Core/Models/`:

- **BaseTypes/**: Contains map data structures deserialized from JSON files (Map, Cell, Burg, State, Province, etc.). These represent the world geography imported from external map generation tools.
- **Player**: Represents a character with inventory and currency (coin subdivided into copper/silver/gold).
- **Party**: Contains multiple players, tracks current Cell/Burg/Location, and manages stamina for movement.
- **Location**: Represents explorable places within cells or burgs, with discovery/exploration tracking.
- **Item**: Equipment and goods with tags determining type (weapon, armor, etc.) and rarity.
- **Scene**: Manages the current interaction state (shop selections, event type, completion status).
- **Combat/**: Contains combat system models including `Enemy`, `CombatState`, `PlayerCombatant`, `CombatEntity`, `AttackResult`, and `CombatSupport`.

### Game Flow

1. Application starts via MonoGame's `Game1` class which initializes the ScreenManager.
2. MainMenuScreen presents New Game or Load Game options.
3. Player selection populates `SaveGameService.CurrentMap` and `SaveGameService.Party`.
4. User navigates between screens using keyboard input handled by InputManager.
5. Movement handled by `MoveService`, which updates Party position and triggers `EventService`.
6. Events determine scene type (exploration rewards, shop interaction, services, combat).
7. Shops use `ShopService` to present buying/selling interfaces.
8. Game state saves on exit.

### Key Data Patterns

- **Static State**: `SaveGameService.CurrentMap` and `SaveGameService.Party` are static and accessed globally throughout services.
- **Scene Management**: The Scene model acts as a state machine for multi-step interactions, tracking completion and selections.
- **Currency System**: Coin values use integer math: 1 gold = 100 silver = 1000 copper = 1000 coin.

### External Resources

- **Items.json**: Defines all items/equipment with tags (TagType enum) for categorization.
- **Services.json**: Defines available services at locations (inns, vendors, etc.).
- **PrebuiltMaps/**: Contains pre-generated world maps that are copied to `%APPDATA%/DolCon/Maps` on first run.

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

Tests use xUnit, NSubstitute for mocking, and FluentAssertions. Located in `DolCon.Core.Tests/`. Key test files include `CombatServiceTests` (extensive combat mechanics), `MapServiceTests` (direction calculation), `PlayerTests`, and `AttackResultTests`.

GitHub Actions runs tests on Ubuntu, Windows, and macOS to ensure cross-platform compatibility.

## Important Notes

- Game state is managed through static properties on SaveGameService rather than passed parameters.
- Map JSON structure comes from an external map generator tool and follows a specific schema (cells, burgs, states, provinces, rivers).
- Movement costs vary by biome and are calculated in MoveService based on map cell data.
- The UI is fully keyboard-driven with no mouse support.
- Inventory is limited to 50 items per player.
- Location exploration uses a percentage system that increases over time.
- Encounter difficulty scales based on cell distance from the map center (challenge rating increases further from civilization).
