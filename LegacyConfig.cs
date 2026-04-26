using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;

namespace ItemLegacy;

public sealed class LegacyConfig
{
    private const string ConfigFileName = "ItemLegacy.cfg";

    private static readonly Lazy<LegacyConfig> LazyCurrent = new(Load);

    public bool InheritCardUpgradesAndEnchantments { get; init; }

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
                SaveDefault(path);
                return config;
            }

            Dictionary<string, string> values = ReadValues(path);
            return new LegacyConfig
            {
                InheritCardUpgradesAndEnchantments = GetBool(values, "Cards.InheritUpgradesAndEnchantments"),
                InheritAllRelicRarities = GetBool(values, "Relics.InheritAllRarities"),
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

        ## 遗物遗产是否允许继承所有种类的遗物。
        ## false：只继承普通、罕见、稀有、商店遗物。
        ## true：允许上一局历史中的所有遗物种类进入候选。
        # Setting type: Boolean
        # Default value: false
        InheritAllRarities = false
        """;
}
