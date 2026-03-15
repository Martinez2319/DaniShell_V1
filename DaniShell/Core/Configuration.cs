using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaniShell.Core;

public class ShellConfig
{
    public string Theme { get; set; } = "hacker";
    public bool SoundEnabled { get; set; } = true;
    public string Prompt { get; set; } = "[DaniShell:{user}]";
    public string StartDirectory { get; set; } = "";
    public Dictionary<string, string> EnvironmentVars { get; set; } = new();
    public int HistoryLimit { get; set; } = 5000;
    public bool ShowBanner { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-dd hh:mm:ss tt";
}

public class ThemeColors
{
    public string Name { get; set; } = "custom";
    public string Prompt { get; set; } = "Cyan";
    public string Directory { get; set; } = "Yellow";
    public string File { get; set; } = "Green";
    public string Error { get; set; } = "Red";
    public string Success { get; set; } = "Green";
    public string Info { get; set; } = "DarkCyan";
    public string Warning { get; set; } = "DarkYellow";
    public string Highlight { get; set; } = "White";
    public string Muted { get; set; } = "DarkGray";
    public string Banner { get; set; } = "Cyan";
}

public static class Configuration
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".danishell");
    
    public static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");
    public static readonly string AliasFile = Path.Combine(ConfigDir, "aliases.json");
    public static readonly string HistoryFile = Path.Combine(ConfigDir, "history");
    public static readonly string ThemesFile = Path.Combine(ConfigDir, "themes.json");
    public static readonly string PluginsDir = Path.Combine(ConfigDir, "plugins");

    public static ShellConfig Config { get; set; } = new();
    public static Dictionary<string, string> Aliases { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, ThemeColors> Themes { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public static ThemeColors CurrentTheme => Themes.GetValueOrDefault(Config.Theme, GetDefaultThemes()["hacker"]);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static void Initialize()
    {
        EnsureConfigDirectory();
        LoadConfig();
        LoadAliases();
        LoadThemes();
        ApplyEnvironmentVars();
    }

    private static void EnsureConfigDirectory()
    {
        if (!Directory.Exists(ConfigDir))
            Directory.CreateDirectory(ConfigDir);
        if (!Directory.Exists(PluginsDir))
            Directory.CreateDirectory(PluginsDir);
    }

    public static void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                Config = JsonSerializer.Deserialize<ShellConfig>(json, JsonOptions) ?? new ShellConfig();
            }
            else
            {
                Config = new ShellConfig();
                SaveConfig();
            }
        }
        catch
        {
            Config = new ShellConfig();
        }
    }

    public static void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, JsonOptions);
            File.WriteAllText(ConfigFile, json);
        }
        catch { }
    }

    public static void LoadAliases()
    {
        try
        {
            if (File.Exists(AliasFile))
            {
                var json = File.ReadAllText(AliasFile);
                Aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) 
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // Aliases por defecto
                Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ll"] = "ls -la",
                    ["la"] = "ls -a",
                    [".."] = "cd ..",
                    ["..."] = "cd ../..",
                    ["c"] = "clear",
                    ["h"] = "history",
                    ["q"] = "exit",
                    ["md"] = "mkdir",
                    ["rd"] = "rmdir",
                    ["del"] = "rm",
                    ["copy"] = "cp",
                    ["move"] = "mv",
                    ["type"] = "cat",
                    ["cls"] = "clear"
                };
                SaveAliases();
            }
        }
        catch
        {
            Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static void SaveAliases()
    {
        try
        {
            var json = JsonSerializer.Serialize(Aliases, JsonOptions);
            File.WriteAllText(AliasFile, json);
        }
        catch { }
    }

    public static void LoadThemes()
    {
        try
        {
            if (File.Exists(ThemesFile))
            {
                var json = File.ReadAllText(ThemesFile);
                Themes = JsonSerializer.Deserialize<Dictionary<string, ThemeColors>>(json, JsonOptions)
                    ?? GetDefaultThemes();
            }
            else
            {
                Themes = GetDefaultThemes();
                SaveThemes();
            }
        }
        catch
        {
            Themes = GetDefaultThemes();
        }
    }

    public static void SaveThemes()
    {
        try
        {
            var json = JsonSerializer.Serialize(Themes, JsonOptions);
            File.WriteAllText(ThemesFile, json);
        }
        catch { }
    }

    public static Dictionary<string, ThemeColors> GetDefaultThemes()
{
    return new Dictionary<string, ThemeColors>(StringComparer.OrdinalIgnoreCase)
    {
        ["dark"] = new ThemeColors
        {
            Name = "dark",
            Prompt = "Cyan",
            Directory = "Yellow",
            File = "White",
            Error = "Red",
            Success = "Green",
            Info = "DarkCyan",
            Warning = "DarkYellow",
            Highlight = "White",
            Muted = "DarkGray",
            Banner = "Cyan"
        },
        ["light"] = new ThemeColors
        {
            Name = "light",
            Prompt = "DarkBlue",
            Directory = "DarkYellow",
            File = "Black",
            Error = "DarkRed",
            Success = "DarkGreen",
            Info = "DarkCyan",
            Warning = "DarkYellow",
            Highlight = "Black",
            Muted = "Gray",
            Banner = "DarkBlue"
        },
        ["hacker"] = new ThemeColors
        {
            Name = "hacker",
            Prompt = "Green",
            Directory = "DarkGreen",
            File = "Green",
            Error = "Red",
            Success = "Green",
            Info = "DarkGreen",
            Warning = "Yellow",
            Highlight = "White",
            Muted = "DarkGray",
            Banner = "Green"
        },
        ["ocean"] = new ThemeColors
        {
            Name = "ocean",
            Prompt = "Cyan",
            Directory = "DarkCyan",
            File = "Blue",
            Error = "Red",
            Success = "Cyan",
            Info = "DarkCyan",
            Warning = "Yellow",
            Highlight = "White",
            Muted = "DarkGray",
            Banner = "Cyan"
        },
        ["fire"] = new ThemeColors
        {
            Name = "fire",
            Prompt = "Red",
            Directory = "DarkYellow",
            File = "Yellow",
            Error = "DarkRed",
            Success = "Green",
            Info = "DarkYellow",
            Warning = "Red",
            Highlight = "White",
            Muted = "DarkGray",
            Banner = "Red"
        },
        ["cyberpunk"] = new ThemeColors
        {
            Name = "cyberpunk",
            Prompt = "Magenta",
            Directory = "DarkMagenta",
            File = "Cyan",
            Error = "Red",
            Success = "Magenta",
            Info = "DarkMagenta",
            Warning = "Yellow",
            Highlight = "White",
            Muted = "DarkGray",
            Banner = "Magenta"
        },
        ["neon"] = new ThemeColors
        {
            Name = "neon",
            Prompt = "Cyan",
            Directory = "Magenta",
            File = "Yellow",
            Error = "Red",
            Success = "Green",
            Info = "Cyan",
            Warning = "DarkYellow",
            Highlight = "White",
            Muted = "DarkGray",
            Banner = "Magenta"
        }
    };
}
    private static void ApplyEnvironmentVars()
    {
        foreach (var kvp in Config.EnvironmentVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
    }

    public static void SetEnvironmentVar(string key, string value)
    {
        Config.EnvironmentVars[key] = value;
        Environment.SetEnvironmentVariable(key, value);
        SaveConfig();
    }

    public static void RemoveEnvironmentVar(string key)
    {
        Config.EnvironmentVars.Remove(key);
        Environment.SetEnvironmentVariable(key, null);
        SaveConfig();
    }

    public static ConsoleColor ParseColor(string colorName)
    {
        if (Enum.TryParse<ConsoleColor>(colorName, true, out var color))
            return color;
        return ConsoleColor.White;
    }
}
