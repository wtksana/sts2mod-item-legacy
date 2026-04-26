using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Logging;

namespace ItemLegacy;

public sealed class LegacyConfig
{
    private const string ConfigFileName = "ItemLegacy.cfg";

    private static readonly Lazy<LegacyConfig> LazyCurrent = new(Load);

    public bool InheritCardUpgradesAndEnchantments { get; init; }

    public IReadOnlySet<RelicRarity> InheritableRelicRarities { get; init; } = DefaultRelicRarities;

    public static LegacyConfig Current => LazyCurrent.Value;

    private static LegacyConfig Load()
    {
        string path = GetConfigPath();
        try
        {
            if (!File.Exists(path))
            {
                LegacyConfig config = new();
                SaveDefault(path);
                return config;
            }

            Dictionary<string, string> values = ReadValues(path);
            return new LegacyConfig
            {
                InheritCardUpgradesAndEnchantments = GetBool(values, "Cards.InheritUpgradesAndEnchantments"),
                InheritableRelicRarities = GetRelicRarities(values, "Relics.InheritableRarities"),
            };
        }
        catch (Exception ex)
        {
            Log.Warn($"ItemLegacy failed to load config at {path}, using defaults. Error: {ex}");
            return new LegacyConfig();
        }
    }

    private static Dictionary<string, string> ReadValues(string path)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        string section = string.Empty;

        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
            {
                section = line[1..^1].Trim();
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator < 0)
            {
                continue;
            }

            string key = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();
            string fullKey = string.IsNullOrEmpty(section) ? key : $"{section}.{key}";
            values[fullKey] = value;
        }

        return values;
    }

    private static bool GetBool(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out string? value))
        {
            return false;
        }

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        return value is "1" or "yes" or "Yes" or "YES" or "on" or "On" or "ON";
    }

    private static IReadOnlySet<RelicRarity> GetRelicRarities(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            return DefaultRelicRarities;
        }

        string[] entries = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (entries.Any(static entry => string.Equals(entry, "All", StringComparison.OrdinalIgnoreCase)))
        {
            return Enum.GetValues<RelicRarity>().ToHashSet();
        }

        HashSet<RelicRarity> rarities = new();
        foreach (string entry in entries)
        {
            if (Enum.TryParse(entry, ignoreCase: true, out RelicRarity rarity))
            {
                rarities.Add(rarity);
            }
            else
            {
                Log.Warn($"ItemLegacy ignored unknown relic rarity in config: {entry}");
            }
        }

        return rarities.Count > 0 ? rarities : DefaultRelicRarities;
    }

    private static void SaveDefault(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, DefaultConfigText);
    }

    private static string GetConfigPath()
    {
        string? assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string? directory = string.IsNullOrEmpty(assemblyLocation)
            ? AppContext.BaseDirectory
            : Path.GetDirectoryName(assemblyLocation);

        return Path.Combine(directory ?? AppContext.BaseDirectory, ConfigFileName);
    }

    private const string DefaultConfigText =
        """
        ## Item Legacy 配置
        ## 修改后需要重启游戏才会生效。

        [Cards]

        ## 卡牌遗产是否继承上一局的升级和附魔。
        ## false：只继承同名基础版卡牌。
        ## true：继承升级等级、附魔和保存属性。
        # Setting type: Boolean
        # Default value: false
        InheritUpgradesAndEnchantments = false

        [Relics]

        ## 允许继承的遗物种类，使用英文枚举名，多个值用英文逗号分隔。
        ## 可用值：None, Starter, Common, Uncommon, Rare, Shop, Event, Ancient
        ## 也可以填 All 允许所有种类。
        # Setting type: String
        # Default value: Common, Uncommon, Rare, Shop
        InheritableRarities = Common, Uncommon, Rare, Shop
        """;

    private static readonly IReadOnlySet<RelicRarity> DefaultRelicRarities = new HashSet<RelicRarity>
    {
        RelicRarity.Common,
        RelicRarity.Uncommon,
        RelicRarity.Rare,
        RelicRarity.Shop,
    };
}
