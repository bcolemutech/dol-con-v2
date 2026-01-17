# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DolCon is a console-based role-playing game written in C# (.NET 8.0) using Spectre.Console for the UI. The game features map-based exploration with parties traveling across cells, burgs (settlements), and locations while managing stamina, inventory, and currency. Players can explore locations, interact with vendors, and manage equipment.

## Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the main application
dotnet run --project DolCon/DolCon.csproj

# Build in Release mode
dotnet build -c Release
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests in a specific project
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

- **SaveGameService**: Manages game save/load operations. Contains a static `CurrentMap` property that serves as the global game state reference. Also manages the static `Party` object that tracks player location and stamina.
- **MapService**: Loads map JSON files from `%APPDATA%/DolCon/Maps`, handles direction calculations between coordinates, and generates location types for cells and burgs.
- **PlayerService**: Manages player creation and persistence.
- **MoveService**: Handles party movement between cells, calculates movement costs based on biome, and manages stamina consumption.
- **EventService**: Processes events when entering locations (exploration, rewards, services).
- **ShopService**: Handles buying/selling items and equipment at vendor locations. Generates random loot rewards.
- **ServicesService**: Loads available services from `Resources/Services.json`.
- **ItemsService**: Loads item definitions from `Resources/Items.json`.
- **ImageService**: Opens map images using the default system image viewer.
- **MainMenuService**: Displays the main menu (New Game, Load Game, Exit).

### View Layer

Views are in `DolCon/Views/`. The primary view is `GameService`, which is a partial class split across multiple files:

- **GameService.cs**: Core game loop using Spectre.Console's `LiveDisplay`. Processes keyboard input and routes to appropriate screen renderers.
- **GameService.Home.cs**: Renders the home screen showing party status, location, and available navigation options.
- **GameService.Navigation.cs**: Handles movement UI - shows directional navigation with compass directions and movement costs.
- **GameService.Scene.cs**: Renders scene interactions (shops, services, events).
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
- **Currency System**: Coin values use integer math: 1 gold = 10 silver = 100 copper = 100 coin.

### External Resources

- **Items.json**: Defines all items/equipment with tags (TagType enum) for categorization.
- **Services.json**: Defines available services at locations (inns, vendors, etc.).
- **PrebuiltMaps/**: Contains pre-generated world maps that are copied to `%APPDATA%/DolCon/Maps` on first run.

### Testing

Tests use xUnit, NSubstitute for mocking, and FluentAssertions. Located in `DolCon.Tests/`. Test key services like MapService (direction calculation), PlayerService, and GameService.

## Important Notes

- Game state is managed through static properties on SaveGameService rather than passed parameters.
- Map JSON structure comes from an external map generator tool and follows a specific schema (cells, burgs, states, provinces, rivers).
- Movement costs vary by biome and are calculated in MoveService based on map cell data.
- The UI is fully keyboard-driven with no mouse support.
- Inventory is limited to 50 items per player.
- Location exploration uses a percentage system that increases over time.
