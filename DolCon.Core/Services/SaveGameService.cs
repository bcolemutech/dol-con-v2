namespace DolCon.Core.Services;

using System.Text.Json;
using DolCon.Core.Models;
using DolCon.Core.Models.World;

public interface ISaveGameService
{
    Task<string> SaveGame();
    IEnumerable<FileInfo> GetSaves();
    Task LoadGame(FileInfo saveFile);
    void DeleteSave(FileInfo saveFile);
}

public class SaveGameService : ISaveGameService
{
    public static WorldLocation? CurrentLocation =>
        Party switch
        {
            { Location: not null, Burg: not null } => CurrentWorld.Burgs
                .Find(b => b.Id == Party.Burg.Value)?.Locations.FirstOrDefault(x => x.Id == Party.Location.Value),
            { Location: not null } => CurrentWorld.Cells[Party.Cell]
                .Locations.FirstOrDefault(x => x.Id == Party.Location.Value),
            _ => null
        };
    public static DolWorld CurrentWorld { get; set; } = new();
    public static Party Party { get; set; } = new();
    public static Guid CurrentPlayerId { get; set; } = Guid.NewGuid();

    /// <summary>True once a baked world has been loaded (new game started or save loaded).</summary>
    public static bool HasWorld => CurrentWorld.Cells.Count > 0;

    public static WorldCell CurrentCell => CurrentWorld.Cells[Party.Cell];

    public static WorldBurg? CurrentBurg => Party.Burg.HasValue
        ? CurrentWorld.Burgs.Find(b => b.Id == Party.Burg.Value)
        : null;

    public static WorldProvince CurrentProvince => CurrentWorld.Provinces[CurrentCell.Province];

    public static WorldState CurrentState => CurrentWorld.States[CurrentCell.State];

    public static string CurrentBiome => CurrentWorld.Biomes[CurrentCell.Biome];

    public static string? CurrentSaveName { get; set; }

    private readonly string _savesPath;

    public SaveGameService()
    {
        _savesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon",
            "Saves");
        if (Directory.Exists(_savesPath)) return;
        Directory.CreateDirectory(_savesPath);
    }

    public async Task<string> SaveGame()
    {
        if (CurrentSaveName == null)
        {
            var existingFiles = Directory.GetFiles(_savesPath, "*.json")
                .Select(Path.GetFileName).ToArray();
            var worldName = string.IsNullOrEmpty(CurrentWorld.Info.Name) ? "unknown" : CurrentWorld.Info.Name;
            var player = Party.Players.FirstOrDefault(p => p.Id == CurrentPlayerId);
            var playerName = SanitizeFileComponent(player?.Name ?? "Unknown");
            CurrentSaveName = GenerateSaveName(worldName, playerName, existingFiles!);
        }

        var saveGamePath = Path.Combine(_savesPath, $"{CurrentSaveName}.json");
        var save = new SaveGame { World = CurrentWorld, Party = Party, CurrentPlayerId = CurrentPlayerId };
        await File.WriteAllTextAsync(saveGamePath, JsonSerializer.Serialize(save, DolWorldSerializer.Options));
        return saveGamePath;
    }

    public IEnumerable<FileInfo> GetSaves()
    {
        return new DirectoryInfo(_savesPath).GetFiles("*.json");
    }

    public async Task LoadGame(FileInfo saveFile)
    {
        var fileStream = File.OpenRead(saveFile.FullName);
        var save = await JsonSerializer.DeserializeAsync<SaveGame>(fileStream, DolWorldSerializer.Options);
        fileStream.Close();

        if (save is null) throw new DolSaveGameException("Failed to load game");

        CurrentWorld = save.World;
        Party = save.Party;
        CurrentPlayerId = save.CurrentPlayerId;
        CurrentSaveName = Path.GetFileNameWithoutExtension(saveFile.Name);
    }

    public void DeleteSave(FileInfo saveFile)
    {
        var savesFullPath = Path.GetFullPath(_savesPath);
        var fileFullPath = Path.GetFullPath(saveFile.FullName);

        if (!fileFullPath.StartsWith(savesFullPath, StringComparison.OrdinalIgnoreCase))
            throw new DolSaveGameException("Cannot delete files outside the saves directory");

        if (saveFile.Exists)
            saveFile.Delete();
    }

    public static WorldCell GetCell(int cellId)
    {
        return CurrentWorld.Cells[cellId];
    }

    public static WorldBurg? GetBurg(int cellBurg)
    {
        return CurrentWorld.Burgs.Find(x => x.Id == cellBurg);
    }

    public static string GetBiome(int cellBiome)
    {
        return CurrentWorld.Biomes[cellBiome];
    }

    public static WorldProvince GetProvince(int cellProvince)
    {
        return CurrentWorld.Provinces[cellProvince];
    }

    public static WorldState GetState(int cellState)
    {
        return CurrentWorld.States[cellState];
    }

    public static string SanitizeFileComponent(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();

        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
    }

    public static string GenerateSaveName(string mapName, string playerName, string[] existingFileNames)
    {
        var sanitizedPlayer = SanitizeFileComponent(playerName);
        var sanitizedMap = SanitizeFileComponent(mapName);
        var baseName = $"{sanitizedMap}.{sanitizedPlayer}";

        var existing = new HashSet<string>(existingFileNames, StringComparer.OrdinalIgnoreCase);

        if (!existing.Contains($"{baseName}.json"))
            return baseName;

        var counter = 2;
        while (existing.Contains($"{baseName}-{counter}.json"))
            counter++;

        return $"{baseName}-{counter}";
    }

    public static string FormatSaveDisplayName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('.', 2);
        return parts.Length == 2 ? $"{parts[1]} ({parts[0]})" : name;
    }
}
