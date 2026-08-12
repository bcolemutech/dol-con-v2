# DolCon - Dominion of Light

A MonoGame RPG where players explore a hand-curated fantasy world, navigate between cells and settlements, engage in turn-based combat, and manage inventory and equipment.

## Requirements

- .NET 10.0 SDK

## Running

```bash
dotnet run --project DolCon.MonoGame/DolCon.MonoGame.csproj
```

## Testing

```bash
dotnet test
```

## World Pipeline

The world is **baked at design time, not generated at runtime**. WorldForge turns an
[Azgaar Fantasy-Map-Generator](https://github.com/Azgaar/Fantasy-Map-Generator) export into a
canonical `world.dol`; the game loads it as-is, so the City of Light, challenge ratings, and
locations are identical for every player on every run.

```
Azgaar JSON ──▶ DolCon.WorldForge ──▶ world.dol ──▶ DolCon.MonoGame
 (committed)      bake (seeded)      (build output)   (loads & plays)
```

`world.dol` is a **build artifact and is not committed** — it is regenerated from the Azgaar export,
which keeps the repository small as the world grows. `dotnet build` bakes it automatically, so a
fresh clone just works. Released worlds are attached to the corresponding
[GitHub Release](https://github.com/bcolemutech/dol-con-v2/releases).

```bash
# Bake by hand (optional — the build does this for you)
dotnet run --project DolCon.WorldForge/DolCon.WorldForge.csproj -- \
  bake "DolCon.MonoGame/PrebuiltMaps/Ouvia Full 2023-07-08-13-04.json" \
  -o DolCon.MonoGame/Worlds/Ouvia.world.dol
```

Bakes are deterministic — pass `--seed <int>`, or omit it to derive a stable seed from the export.
Setting `SOURCE_DATE_EPOCH` (or `--generated-at`) pins the timestamp for a byte-reproducible bake,
which CI verifies on every pull request. See
[`docs/WORLD_DOL_FORMAT.md`](docs/WORLD_DOL_FORMAT.md) for the schema contract.

## Documentation

- [`docs/GAMEPLAY.md`](docs/GAMEPLAY.md) — systems and mechanics
- [`docs/NEW_PLAYER_GUIDE.md`](docs/NEW_PLAYER_GUIDE.md) — getting started
- [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md) — controls and cheat sheet
- [`docs/CHALLENGE_RATINGS.md`](docs/CHALLENGE_RATINGS.md) — encounter difficulty and enemy selection
- [`docs/GLOSSARY.md`](docs/GLOSSARY.md) — terminology
- [`docs/WORLD_DOL_FORMAT.md`](docs/WORLD_DOL_FORMAT.md) — the `world.dol` canonical format
