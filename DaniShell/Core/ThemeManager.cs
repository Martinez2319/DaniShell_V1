namespace DaniShell.Core;

public static class ThemeManager
{
    public static void Write(string text, string colorType = "Highlight")
    {
        var theme = Configuration.CurrentTheme;
        var colorName = colorType switch
        {
            "Prompt" => theme.Prompt,
            "Directory" => theme.Directory,
            "File" => theme.File,
            "Error" => theme.Error,
            "Success" => theme.Success,
            "Info" => theme.Info,
            "Warning" => theme.Warning,
            "Highlight" => theme.Highlight,
            "Muted" => theme.Muted,
            "Banner" => theme.Banner,
            _ => theme.Highlight
        };

        Console.ForegroundColor = Configuration.ParseColor(colorName);
        Console.Write(text);
        Console.ResetColor();
    }

    public static void WriteLine(string text, string colorType = "Highlight")
    {
        Write(text + Environment.NewLine, colorType);
    }

    public static void WriteError(string text)
    {
        WriteLine(text, "Error");
    }

    public static void WriteSuccess(string text)
    {
        WriteLine(text, "Success");
    }

    public static void WriteInfo(string text)
    {
        WriteLine(text, "Info");
    }

    public static void WriteWarning(string text)
    {
        WriteLine(text, "Warning");
    }

    public static void WriteMuted(string text)
    {
        WriteLine(text, "Muted");
    }

    public static void ListThemes()
    {
        WriteLine("Temas disponibles:", "Info");

        foreach (var theme in Configuration.Themes)
        {
            var marker = theme.Key.Equals(Configuration.Config.Theme, StringComparison.OrdinalIgnoreCase)
                ? " [ACTIVO]" : "";

            Console.ForegroundColor = Configuration.ParseColor(theme.Value.Prompt);
            Console.WriteLine($"  {theme.Key}{marker}");
        }

        Console.ResetColor();

        WriteLine("Uso: theme <nombre> | theme create <nombre> | theme edit <nombre>", "Muted");
    }

    public static void CreateTheme(string name)
    {
        if (Configuration.Themes.ContainsKey(name))
        {
            WriteWarning($"El tema '{name}' ya existe. Usa 'theme edit {name}' para modificarlo.");
            return;
        }

        var newTheme = new ThemeColors { Name = name };
        Configuration.Themes[name] = newTheme;
        Configuration.SaveThemes();
        WriteSuccess($"Tema '{name}' creado. Usa 'theme edit {name}' para personalizarlo.");
    }

    public static void EditTheme(string name)
    {
        if (!Configuration.Themes.TryGetValue(name, out var theme))
        {
            WriteError($"Tema '{name}' no encontrado.");
            return;
        }

        WriteLine($"Editando tema: {name}", "Info");

        WriteLine("Colores disponibles: Black, DarkBlue, DarkGreen, DarkCyan, DarkRed, DarkMagenta, DarkYellow, Gray, DarkGray, Blue, Green, Cyan, Red, Magenta, Yellow, White", "Muted");

        WriteLine("Presiona Enter para mantener el valor actual.\n", "Muted");

        theme.Prompt = AskColor("Prompt", theme.Prompt);
        theme.Directory = AskColor("Directory", theme.Directory);
        theme.File = AskColor("File", theme.File);
        theme.Error = AskColor("Error", theme.Error);
        theme.Success = AskColor("Success", theme.Success);
        theme.Info = AskColor("Info", theme.Info);
        theme.Warning = AskColor("Warning", theme.Warning);
        theme.Highlight = AskColor("Highlight", theme.Highlight);
        theme.Muted = AskColor("Muted", theme.Muted);
        theme.Banner = AskColor("Banner", theme.Banner);

        Configuration.Themes[name] = theme;
        Configuration.SaveThemes();
        WriteSuccess($"Tema '{name}' actualizado.");
    }

    private static string AskColor(string property, string current)
    {
        Console.ForegroundColor = Configuration.ParseColor(current);
        Console.Write($"  {property}");
        Console.ResetColor();
        Console.Write($" [{current}]: ");

        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
            return current;

        if (Enum.TryParse<ConsoleColor>(input, true, out _))
            return input;

        WriteWarning($"    Color inválido, manteniendo '{current}'");
        return current;
    }

    public static void SetTheme(string name)
    {
        if (!Configuration.Themes.ContainsKey(name))
        {
            WriteError($"Tema '{name}' no encontrado.");
            ListThemes();
            return;
        }

        Configuration.Config.Theme = name;
        Configuration.SaveConfig();
        WriteSuccess($"Tema cambiado a '{name}'");
    }

    public static void DeleteTheme(string name)
    {
        var defaultThemes = new[] { "dark", "light", "hacker", "ocean", "fire", "cyberpunk", "neon" };

        if (defaultThemes.Contains(name.ToLower()))
        {
            WriteError("No puedes eliminar los temas predeterminados.");
            return;
        }

        if (!Configuration.Themes.ContainsKey(name))
        {
            WriteError($"Tema '{name}' no encontrado.");
            return;
        }

        if (Configuration.Config.Theme.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            Configuration.Config.Theme = "hacker";
            Configuration.SaveConfig();
        }

        Configuration.Themes.Remove(name);
        Configuration.SaveThemes();
        WriteSuccess($"Tema '{name}' eliminado.");
    }
}