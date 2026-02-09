namespace DolCon.Core.Services;

using System.Text.Json;
using DolCon.Core.Models;
using DolCon.Core.Models.BaseTypes;

public interface ISaveGameService
{
    Task<string> SaveGame();
    IEnumerable<FileInfo> GetSaves();
    Task LoadGame(FileInfo saveFile);
    void DeleteSave(FileInfo saveFile);
}

public class SaveGameService : ISaveGameService
{
    public static Location? CurrentLocation =>
        Party switch
        {
            { Location: not null, Burg: not null } => CurrentMap.Collections.burgs
                .Find(b => b.i == Party.Burg.Value)?.locations.FirstOrDefault(x => x.Id == Party.Location.Value),
            { Location: not null } => CurrentMap.Collections.cells[Party.Cell]
                .locations.FirstOrDefault(x => x.Id == Party.Location.Value),
            _ => null
        };
    public static Map CurrentMap { get; set; } = new();
    public static Party Party { get; set; } = new();
    public static Guid CurrentPlayerId { get; set; } = Guid.NewGuid();

    public static Cell CurrentCell => CurrentMap.Collections.cells[Party.Cell];

    public static Burg? CurrentBurg => Party.Burg.HasValue
        ? CurrentMap.Collections.burgs.Find(b => b.i == Party.Burg.Value)
        : null;

    public static Province CurrentProvince => CurrentMap.Collections.provinces[CurrentCell.province];

    public static State CurrentState => CurrentMap.Collections.states[CurrentCell.state];

    public static string CurrentBiome => CurrentMap.biomes.name[CurrentCell.biome];

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
            var mapName = CurrentMap.info?.mapName ?? "unknown";
            var player = Party.Players.FirstOrDefault(p => p.Id == CurrentPlayerId);
            var playerName = SanitizeFileComponent(player?.Name ?? "Unknown");
            CurrentSaveName = GenerateSaveName(mapName, playerName, existingFiles!);
        }

        var saveGamePath = Path.Combine(_savesPath, $"{CurrentSaveName}.json");
        CurrentMap.Party = Party;
        CurrentMap.CurrentPlayerId = CurrentPlayerId;
        await File.WriteAllTextAsync(saveGamePath, JsonSerializer.Serialize(CurrentMap));
        return saveGamePath;
    }

    public IEnumerable<FileInfo> GetSaves()
    {
        return new DirectoryInfo(_savesPath).GetFiles("*.json");
    }

    public async Task LoadGame(FileInfo saveFile)
    {
        var fileStream = File.OpenRead(saveFile.FullName);
        var map = await JsonSerializer.DeserializeAsync<Map>(fileStream);
        fileStream.Close();

        CurrentMap = map ?? throw new DolSaveGameException("Failed to load game");
        Party = CurrentMap.Party;
        CurrentPlayerId = CurrentMap.CurrentPlayerId;
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

    public static Cell GetCell(int cellId)
    {
        return CurrentMap.Collections.cells[cellId];
    }

    public static Burg? GetBurg(int cellBurg)
    {
        return CurrentMap.Collections.burgs.Find(x => x.i == cellBurg);
    }

    public static string GetBiome(int cellBiome)
    {
        return CurrentMap.biomes.name[cellBiome];
    }

    public static Province GetProvince(int cellProvince)
    {
        return CurrentMap.Collections.provinces[cellProvince];
    }

    public static State GetState(int cellState)
    {
        return CurrentMap.Collections.states[cellState];
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
