namespace DolCon.Core.Models.World;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Single source of truth for how <c>world.dol</c> is serialized. WorldForge (the baker) and the
/// game (the loader) both go through here so the format is identical on both sides: camelCase
/// property names, enums written as human-readable strings, indented, and null containers omitted —
/// keeping baked worlds pleasant to author by hand or by an AI agent.
/// </summary>
public static class DolWorldSerializer
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(DolWorld world) => JsonSerializer.Serialize(world, Options);

    public static DolWorld? Deserialize(string json) => JsonSerializer.Deserialize<DolWorld>(json, Options);
}
