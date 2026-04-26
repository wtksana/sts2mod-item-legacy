using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Logging;

namespace ItemLegacy;

public sealed class LegacyConfig
{
    private const string ConfigFileName = "ItemLegacy.config";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly Lazy<LegacyConfig> LazyCurrent = new(Load);

    [JsonPropertyName("inherit_card_upgrades_and_enchantments")]
    public bool InheritCardUpgradesAndEnchantments { get; init; }

    [JsonPropertyName("inherit_all_relic_rarities")]
    public bool InheritAllRelicRarities { get; init; }

    public static LegacyConfig Current => LazyCurrent.Value;

    private static LegacyConfig Load()
    {
        string path = GetConfigPath();
        try
        {
            if (!File.Exists(path))
            {
                LegacyConfig config = new();
                SaveDefault(path, config);
                return config;
            }

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LegacyConfig>(json, JsonOptions) ?? new LegacyConfig();
        }
        catch (Exception ex)
        {
            Log.Warn($"ItemLegacy failed to load config at {path}, using defaults. Error: {ex}");
            return new LegacyConfig();
        }
    }

    private static void SaveDefault(string path, LegacyConfig config)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions) + Environment.NewLine);
    }

    private static string GetConfigPath()
    {
        string? assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string? directory = string.IsNullOrEmpty(assemblyLocation)
            ? AppContext.BaseDirectory
            : Path.GetDirectoryName(assemblyLocation);

        return Path.Combine(directory ?? AppContext.BaseDirectory, ConfigFileName);
    }
}
