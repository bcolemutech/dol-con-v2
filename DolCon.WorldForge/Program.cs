using System.Globalization;
using System.Text.Json;
using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Models.World;
using DolCon.Core.Services;
using DolCon.WorldForge;

return WorldForgeCli.Run(args);

internal static class WorldForgeCli
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        return args[0] switch
        {
            "bake" => Bake(args[1..]),
            "-h" or "--help" or "help" => PrintUsage(),
            _ => Fail($"Unknown command '{args[0]}'.")
        };
    }

    private static int Bake(string[] args)
    {
        string? input = null;
        string? output = null;
        int? seed = null;
        DateTime? generatedAt = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o" or "--output":
                    if (++i >= args.Length) return Fail("Missing value for -o/--output.");
                    output = args[i];
                    break;
                case "--seed":
                    if (++i >= args.Length) return Fail("Missing value for --seed.");
                    if (!int.TryParse(args[i], out var parsedSeed)) return Fail($"--seed must be an integer, got '{args[i]}'.");
                    seed = parsedSeed;
                    break;
                case "--generated-at":
                    if (++i >= args.Length) return Fail("Missing value for --generated-at.");
                    if (!TryParseTimestamp(args[i], out var parsedStamp))
                        return Fail($"--generated-at must be an ISO-8601 UTC timestamp, got '{args[i]}'.");
                    generatedAt = parsedStamp;
                    break;
                default:
                    if (input is null) input = args[i];
                    else return Fail($"Unexpected argument '{args[i]}'.");
                    break;
            }
        }

        if (input is null) return Fail("Missing <azgaar-export.json> input path.");
        if (output is null) return Fail("Missing -o <world.dol> output path.");
        if (!File.Exists(input)) return Fail($"Input file not found: {input}");

        Console.WriteLine($"WorldForge: baking '{input}'");

        Map? map;
        try
        {
            using var stream = File.OpenRead(input);
            map = JsonSerializer.Deserialize<Map>(stream);
        }
        catch (JsonException ex)
        {
            return Fail($"Failed to parse Azgaar export: {ex.Message}");
        }

        if (map?.Collections?.cells is null || map.Collections.burgs is null)
        {
            return Fail("Azgaar export is missing required cells/burgs collections.");
        }

        var resolvedSeed = seed ?? DefaultSeed(map);
        Console.WriteLine($"  seed: {resolvedSeed}{(seed is null ? " (derived from map)" : "")}");

        var resolvedStamp = generatedAt ?? SourceDateEpoch();
        if (resolvedStamp is not null)
        {
            Console.WriteLine($"  generatedAt: {resolvedStamp:O} (pinned — reproducible bake)");
        }

        var baker = new WorldBaker();
        var world = baker.Bake(map, resolvedSeed, new ConsoleProvisioningCallback(), resolvedStamp);

        var json = DolWorldSerializer.Serialize(world);
        var outputDir = Path.GetDirectoryName(Path.GetFullPath(output));
        if (!string.IsNullOrEmpty(outputDir)) Directory.CreateDirectory(outputDir);
        File.WriteAllText(output, json);

        PrintSummary(world, output);
        return 0;
    }

    private static void PrintSummary(DolWorld world, string output)
    {
        var cityOfLight = world.Burgs.FirstOrDefault(b => b.IsCityOfLight)?.Name ?? "(none)";
        var locations = world.Cells.Sum(c => c.Locations.Count) + world.Burgs.Sum(b => b.Locations.Count);
        var bytes = new FileInfo(output).Length;

        Console.WriteLine();
        Console.WriteLine("Bake complete:");
        Console.WriteLine($"  world         : {world.Info.Name}");
        Console.WriteLine($"  schemaVersion : {world.SchemaVersion}");
        Console.WriteLine($"  seed          : {world.Info.ProvisioningSeed}");
        Console.WriteLine($"  cells         : {world.Cells.Count}");
        Console.WriteLine($"  burgs         : {world.Burgs.Count}");
        Console.WriteLine($"  City of Light : {cityOfLight}");
        Console.WriteLine($"  locations     : {locations}");
        Console.WriteLine($"  output        : {output} ({bytes:N0} bytes)");
    }

    /// <summary>
    /// Parses an ISO-8601 timestamp and normalises it to UTC, so a pinned bake is byte-identical
    /// regardless of the machine's local time zone.
    /// </summary>
    private static bool TryParseTimestamp(string value, out DateTime utc)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }

        utc = default;
        return false;
    }

    /// <summary>
    /// Honours the reproducible-builds <c>SOURCE_DATE_EPOCH</c> convention (Unix seconds) so CI can
    /// pin the bake timestamp without passing a flag. Ignored when unset or unparseable.
    /// </summary>
    private static DateTime? SourceDateEpoch()
    {
        var raw = Environment.GetEnvironmentVariable("SOURCE_DATE_EPOCH");
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;
    }

    /// <summary>
    /// Default provisioning seed when none is supplied: the numeric Azgaar seed if parseable, else a
    /// stable hash of it. This keeps a no-flag bake of the same export reproducible.
    /// </summary>
    private static int DefaultSeed(Map map)
    {
        var azgaarSeed = map.info?.seed;
        if (string.IsNullOrEmpty(azgaarSeed)) return 0;
        return int.TryParse(azgaarSeed, out var numeric) ? numeric : StableHash(azgaarSeed);
    }

    /// <summary>FNV-1a 32-bit hash — stable across runs/platforms (unlike string.GetHashCode()).</summary>
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = (uint)2166136261;
            foreach (var c in value)
            {
                hash ^= c;
                hash *= 16777619;
            }

            return (int)hash;
        }
    }

    private static int PrintUsage()
    {
        Console.WriteLine("""
            WorldForge — bake an Azgaar export into a canonical world.dol

            Usage:
              worldforge bake <azgaar-export.json> -o <world.dol> [--seed <int>] [--generated-at <iso8601>]

            Options:
              -o, --output     Path to write the baked world.dol (required).
              --seed           Provisioning seed for reproducible bakes (default: derived from the map).
              --generated-at   Pin the generatedAt stamp (ISO-8601 UTC) for a byte-reproducible bake.
                               Also honours the SOURCE_DATE_EPOCH environment variable.
              -h, --help       Show this help.
            """);
        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        return 1;
    }
}
