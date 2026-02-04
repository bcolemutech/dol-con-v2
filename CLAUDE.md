# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DolCon is a role-playing game written in C# (.NET 10.0) with both a console UI (Spectre.Console) and a graphical UI (MonoGame). The game features map-based exploration with parties traveling across cells, burgs (settlements), and locations while managing stamina, inventory, and currency. Players can explore locations, engage in turn-based combat, interact with vendors, and manage equipment.

## Project Structure

The solution consists of 5 projects:

```
dol-con-v2/
├── DolCon.Core/           # Shared game logic library (Models, Services, Enums, Data)
├── DolCon.Core.Tests/     # Tests for Core library
├── DolCon/                # Console application (Spectre.Console UI)
├── DolCon.Tests/          # Tests for Console-specific code
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

### DolCon (Console Application)
Spectre.Console-based UI:
- **Views/**: GameService (partial class with Home, Navigation, Scene, Battle, Inventory rendering)
- **Services/**: MainMenuService, ImageService, HostedService

### DolCon.MonoGame (Graphical Application)
MonoGame-based UI with placeholder graphics:
- **Screens/**: MainMenuScreen, HomeScreen, NavigationScreen, BattleScreen, InventoryScreen
- **Input/**: InputManager for keyboard handling
- Uses ScreenManager for navigation between screens

## Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the console application
dotnet run --project DolCon/DolCon.csproj

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

# Run Console tests only
dotnet test DolCon.Tests/DolCon.Tests.csproj

# Run tests with verbose output
dotnet test -v normal

# Run a specific test
dotnet test --filter "FullyQualifiedName~GameServiceTests"
```

### Restore Dependencies
```bash
# Restore NuGet packages
dotnet restore
```

## Architecture

### Dependency Injection & Application Lifecycle

The application uses Microsoft.Extensions.Hosting with dependency injection configured in `Program.cs`. All services are registered as singletons. The `HostedService` orchestrates the application lifecycle and delegates to `MainMenuService` on startup.

### Service Layer

Services are located in `DolCon/Services/` and handle core game logic:

- **SaveGameService**: Manages game save/load operations. Contains static properties for global game state: `CurrentMap`, `Party`, `CurrentCell`, `CurrentBurg`, `CurrentLocation`, `CurrentProvince`, `CurrentState`, `CurrentBiome`, and `CurrentPlayerId`.
- **MapService**: Loads map JSON files from `%APPDATA%/DolCon/Maps`, handles direction calculations between coordinates, and generates location types for cells and burgs.
- **PlayerService**: Manages player creation and persistence.
- **MoveService**: Handles party movement between cells, calculates movement costs based on biome, and manages stamina consumption.
- **EventService**: Processes events when entering locations (exploration, rewards, services, combat encounters).
- **CombatService**: Manages turn-based combat including initiative rolls, turn order, attack resolution, damage calculation, and combat state transitions.
- **ShopService**: Handles buying/selling items and equipment at vendor locations. Generates random loot rewards using Chance.NET.
- **ServicesService**: Loads available services from `Resources/Services.json`.
- **ItemsService**: Loads item definitions from `Resources/Items.json`.
- **ImageService**: Opens map images using the default system image viewer.
- **MainMenuService**: Displays the main menu (New Game, Load Game, Exit).

### View Layer

Views are in `DolCon/Views/`. The primary view is `GameService`, which is a partial class split across multiple files:

- **GameService.cs**: Core game loop using Spectre.Console's `LiveDisplay`. Processes keyboard input and routes to appropriate screen renderers.
- **GameService.Home.cs**: Renders the home screen showing party status, location, and available navigation options.
- **GameService.Navigation.cs**: Handles movement UI - shows directional navigation with compass directions and movement costs. Uses platform-specific modifier key names (Alt on Windows/Linux, Option on macOS).
- **GameService.Scene.cs**: Renders scene interactions (shops, services, events).
- **GameService.Battle.cs**: Renders combat encounters with turn order display, health bars, combat log, and player action selection.
- **GameService.Inventory.cs**: Displays player inventory with equipment management.
- **GameService.NotReady.cs**: Placeholder for unimplemented screens.

The game uses a three-panel layout: Message (top), Display (main content), and Controls (bottom hints).

### Models

Models are in `DolCon/Models/`:

- **BaseTypes/**: Contains map data structures deserialized from JSON files (Map, Cell, Burg, State, Province, etc.). These represent the world geography imported from external map generation tools.
- **Player**: Represents a character with inventory and currency (coin subdivided into copper/silver/gold).
- **Party**: Contains multiple players, tracks current Cell/Burg/Location, and manages stamina for movement.
- **Location**: Represents explorable places within cells or burgs, with discovery/exploration tracking.
- **Item**: Equipment and goods with tags determining type (weapon, armor, etc.) and rarity.
- **Scene**: Manages the current interaction state (shop selections, event type, completion status).
- **Flow**: Tracks UI state including current screen and keyboard input.
- **Combat/**: Contains combat system models including `Enemy`, `CombatState`, `PlayerCombatant`, `CombatEntity`, `AttackResult`, and `CombatSupport`.

### Game Flow

1. Application starts through `HostedService` which calls `MainMenuService`.
2. Player selects New Game or Load Game, which populates `SaveGameService.CurrentMap` and `SaveGameService.Party`.
3. `GameService.Start()` is called, entering the main game loop with `ProcessKey()`.
4. User navigates between screens using number keys (Screen enum values).
5. Movement handled by `MoveService`, which updates Party position and triggers `EventService`.
6. Events determine scene type (exploration rewards, shop interaction, services).
7. Shops use `ShopService` to present buying/selling interfaces with item selections.
8. Game state auto-saves on application shutdown via `HostedService.StopAsync()`.

### Key Data Patterns

- **Static State**: `SaveGameService.CurrentMap` and `SaveGameService.Party` are static and accessed globally throughout services.
- **Screen Routing**: Enum-based screens (Home, Navigation, Scene, Inventory, Quests) map to ConsoleKey numbers for keyboard shortcuts.
- **Scene Management**: The Scene model acts as a state machine for multi-step interactions, tracking completion and selections.
- **Currency System**: Coin values use integer math: 1 gold = 100 silver = 1000 copper = 1000 coin.

### External Resources

- **Items.json**: Defines all items/equipment with tags (TagType enum) for categorization.
- **Services.json**: Defines available services at locations (inns, vendors, etc.).
- **PrebuiltMaps/**: Contains pre-generated world maps that are copied to `%APPDATA%/DolCon/Maps` on first run.

### Combat System

The game features D&D 5e-inspired turn-based combat located in `DolCon/Data/`:

- **EnemyIndex**: Central repository of 100+ enemies organized by `EnemyCategory` (Nature, Human, Undead, Demon) and `EnemySubcategory`. Enemies have CR ratings, biome restrictions, loot drops, and behavior AI. Initialized at startup in `Program.cs`.
- **EncounterBuilder**: Generates dynamic encounters based on party level and `EncounterDifficulty` (Easy, Medium, Hard, Deadly). Supports boss encounters with `BuildBossEncounter()`.
- **BiomeMapper**: Converts map biomes to combat `BiomeType` values, affecting which enemies can spawn.

Combat flow:
1. `EventService` triggers combat encounters during exploration.
2. `CombatService` manages initiative, turn order, and action resolution.
3. `GameService.Battle.cs` renders the combat UI with turn indicators and combat log.
4. Loot and experience awarded on victory.

### Testing

Tests use xUnit, NSubstitute for mocking, and FluentAssertions. Located in `DolCon.Tests/`. Key test files include `CombatServiceTests` (extensive combat mechanics), `MapServiceTests` (direction calculation), `PlayerTests`, and `GameServiceNavigationTests` (platform-specific keyboard support).

GitHub Actions runs tests on Ubuntu, Windows, and macOS to ensure cross-platform compatibility.

## Important Notes

- Game state is managed through static properties on SaveGameService rather than passed parameters.
- Map JSON structure comes from an external map generator tool and follows a specific schema (cells, burgs, states, provinces, rivers).
- Movement costs vary by biome and are calculated in MoveService based on map cell data.
- The UI is fully keyboard-driven with no mouse support.
- Inventory is limited to 50 items per player.
- Location exploration uses a percentage system that increases over time.
- Encounter difficulty scales based on cell distance from the map center (challenge rating increases further from civilization).
- Platform-specific UI: Navigation uses `OperatingSystem.IsMacOS()` to show "Option" vs "Alt" for modifier keys.
