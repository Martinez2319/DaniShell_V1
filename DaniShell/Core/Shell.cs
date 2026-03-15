using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace DaniShell.Core;

public static class Shell
{
    public static string PreviousDirectory { get; set; } = "";
    public static string CurrentDirectory { get; private set; } = "";
    private static Process? _activeProcess;
    private static readonly Random _rng = new();
    private static Dictionary<string, string> _cmdDescriptions = new(StringComparer.OrdinalIgnoreCase);

    public static readonly string Version = "DaniShell v4.0.0";
    public static readonly string Author = "Desarrollado por Daniel";
    public static readonly string City = "Ciudad Juárez, Chihuahua 🇲🇽";

    private static readonly string[] WelcomeMessages = new[]
    {
        "Bienvenido de vuelta, crack del código",
        "Listo para compilar sueños en C#",
        "Don't Panic: tu shell te cuida",
        "Be water, my friend… y compila",
        "Hoy sale deploy sin bugs",
        "Que la fuerza del código te acompañe",
        "Console.WriteLine(\"Éxito\"); ¡otra vez!",
        "Hora de debuggear como un pro",
        "Compila tranquilo, los bugs son opcionales",
        "El café y el código nunca fallan",
        "Prepárate para dominar la terminal",
        "Que tus variables siempre estén inicializadas",
        "Aquí no hay stack overflow… aún",
        "Linux + Windows = DaniShell",
        "rm -rf bugs && make success"
    };

    public static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Inicializar configuración
        Configuration.Initialize();
        InputHandler.LoadHistory();
        LoadCmdDescriptions();
        Plugins.PluginLoader.LoadPlugins();

        // Directorio inicial
        if (!string.IsNullOrEmpty(Configuration.Config.StartDirectory) && 
            Directory.Exists(Configuration.Config.StartDirectory))
        {
            CurrentDirectory = Configuration.Config.StartDirectory;
        }
        else
        {
            CurrentDirectory = Directory.GetCurrentDirectory();
        }
        Directory.SetCurrentDirectory(CurrentDirectory);

        // Manejar Ctrl+C
        Console.CancelKeyPress += HandleCtrlC;

        // Mostrar banner
        if (Configuration.Config.ShowBanner)
        {
            DrawBanner();
            RandomWelcome();
        }

        // Bucle principal
        while (true)
        {
            PrintPrompt(CurrentDirectory);
            
            var input = InputHandler.ReadLine(CurrentDirectory, AutoComplete);
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            InputHandler.AddToHistory(input);
            
            // Procesar el comando
            CurrentDirectory = HandleCommand(input.Trim());
        }
    }

    private static void HandleCtrlC(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;

        if (_activeProcess != null && !_activeProcess.HasExited)
        {
            try
            {
                _activeProcess.Kill(entireProcessTree: true);
                _activeProcess.WaitForExit();
                Console.WriteLine("🔹 Proceso interrumpido.");
            }
            catch { }
            finally
            {
                _activeProcess = null;
            }
        }
        else
        {
            Console.WriteLine();
        }

        // Limpiar buffer
        while (Console.KeyAvailable)
            Console.ReadKey(true);

        PrintPrompt(CurrentDirectory);
    }

    public static void DrawBanner()
    {
        Console.Clear();

        string bannerText = LoadRandomBanner();
        
        Console.ForegroundColor = Configuration.ParseColor(Configuration.CurrentTheme.Banner);
        
        using var sr = new StringReader(bannerText);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            Console.WriteLine(line);
            Thread.Sleep(15);
        }
        Console.ResetColor();

        var isAdmin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        Console.WriteLine($"Usuario: {Environment.UserName} | Admin: {isAdmin} | Tema: {Configuration.Config.Theme}");
        ThemeManager.WriteMuted("Escribe 'help' para ver comandos disponibles.");
    }

    private static string LoadRandomBanner()
    {
        try
        {
            var bannerFolder = Path.Combine(AppContext.BaseDirectory, "banners");
            
            if (!Directory.Exists(bannerFolder))
            {
                Directory.CreateDirectory(bannerFolder);
                return GetEmbeddedBanner();
            }

            var files = Directory.GetFiles(bannerFolder, "*.txt");
            if (files.Length == 0)
                return GetEmbeddedBanner();

            var content = File.ReadAllText(files[_rng.Next(files.Length)], Encoding.UTF8);
            
            // Limpiar caracteres @" del inicio de línea (bug de los banners originales)
            content = content.Replace("@\"", "").Replace("\",", "");
            
            return content;
        }
        catch
        {
            return GetEmbeddedBanner();
        }
    }

    private static string GetEmbeddedBanner()
    {
        return @"
╔═══════════════════════════════════════════════════════════════════════════════════╗
║                                                                                   ║
║     ██████╗  █████╗ ███╗   ██╗██╗███████╗██╗  ██╗███████╗██╗     ██╗              ║
║     ██╔══██╗██╔══██╗████╗  ██║██║██╔════╝██║  ██║██╔════╝██║     ██║              ║
║     ██║  ██║███████║██╔██╗ ██║██║███████╗███████║█████╗  ██║     ██║              ║
║     ██║  ██║██╔══██║██║╚██╗██║██║╚════██║██╔══██║██╔══╝  ██║     ██║              ║
║     ██████╔╝██║  ██║██║ ╚████║██║███████║██║  ██║███████╗███████╗███████╗         ║
║     ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚══════╝╚══════╝         ║
║                                                                                   ║
║                              DaniShell v4.0                                       ║
║                   La seguridad no es un producto, es un proceso                   ║
╚═══════════════════════════════════════════════════════════════════════════════════╝";
    }

    public static void RandomWelcome()
    {
        var msg = WelcomeMessages[_rng.Next(WelcomeMessages.Length)];
        ThemeManager.WriteLine($"  ✨ {msg}", "Info");
    }

    public static void PrintPrompt(string dir, bool newLine = true)
    {
        if (newLine)
            Console.WriteLine();

        var prompt = Configuration.Config.Prompt
            .Replace("{user}", Environment.UserName)
            .Replace("{host}", Environment.MachineName)
            .Replace("{dir}", dir)
            .Replace("{time}", DateTime.Now.ToString("HH:mm:ss"))
            .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));

        var dateStr = DateTime.Now.ToString(Configuration.Config.DateFormat);

        ThemeManager.Write($"[{dateStr}] ", "Muted");
        ThemeManager.Write(prompt, "Prompt");
        Console.Write($" {dir}> ");
    }

    private static string HandleCommand(string input)
    {
        try
        {
            // Expandir alias
            input = ExpandAlias(input);

            // Soporte para pipes básico (cmd | cmd)
            if (input.Contains('|'))
            {
                return HandlePipe(input);
            }

            var parts = ParseCommandLine(input);
            if (parts.Length == 0) return CurrentDirectory;

            var cmd = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            // 1. Verificar si es comando builtin
            if (Commands.BuiltinCommands.IsBuiltin(cmd))
            {
                var result = Commands.BuiltinCommands.Execute(cmd, args, CurrentDirectory);
                if (!string.IsNullOrEmpty(result))
                {
                    BeepDone();
                    return result;
                }
                return CurrentDirectory;
            }

            // 2. Verificar si es comando Linux
            if (Commands.LinuxCommands.IsLinuxCommand(cmd))
            {
                var result = Commands.LinuxCommands.Execute(cmd, args, CurrentDirectory);
                if (result != null)
                {
                    BeepDone();
                    return string.IsNullOrEmpty(result) ? CurrentDirectory : result;
                }
            }

            // 3. Verificar si es comando de red
            if (Commands.NetworkCommands.IsNetworkCommand(cmd))
            {
                Commands.NetworkCommands.Execute(cmd, args, CurrentDirectory);
                BeepDone();
                return CurrentDirectory;
            }

            // 4. Verificar plugins
            var plugin = Plugins.PluginLoader.GetPlugin(cmd);
            if (plugin != null)
            {
                var result = plugin.Execute(args, CurrentDirectory);
                BeepDone();
                return string.IsNullOrEmpty(result) ? CurrentDirectory : result;
            }

            // 5. Ejecutar como comando externo (CMD/PowerShell)
            ExecuteExternal(input, CurrentDirectory);
            BeepDone();
            return CurrentDirectory;
        }
        catch (Exception ex)
        {
            ThemeManager.WriteError($"Error: {ex.Message}");
            return CurrentDirectory;
        }
    }

    private static string ExpandAlias(string input)
    {
        var parts = input.Split(' ', 2);
        if (parts.Length == 0) return input;

        if (Configuration.Aliases.TryGetValue(parts[0], out var alias))
        {
            return parts.Length > 1 
                ? $"{alias} {parts[1]}" 
                : alias;
        }
        return input;
    }

    private static string[] ParseCommandLine(string input)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        char quoteChar = '"';

        foreach (var c in input)
        {
            if ((c == '"' || c == '\'') && !inQuotes)
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (c == quoteChar && inQuotes)
            {
                inQuotes = false;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result.ToArray();
    }

    private static string HandlePipe(string input)
    {
        // Soporte básico de pipes
        var commands = input.Split('|').Select(c => c.Trim()).ToArray();
        
        ThemeManager.WriteMuted("Ejecutando pipeline...");
        
        // Por ahora, ejecutar como comando externo
        ExecuteExternal(input, CurrentDirectory);
        return CurrentDirectory;
    }

    public static void ExecuteExternal(string input, string workDir)
    {
        try
        {
            var shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash";
            var shellArg = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c" : "-c";

            var psi = new ProcessStartInfo(shell, $"{shellArg} {input}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };

            _activeProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

            _activeProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine(e.Data);
            };

            _activeProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    ThemeManager.WriteError(e.Data);
            };

            _activeProcess.Start();
            _activeProcess.BeginOutputReadLine();
            _activeProcess.BeginErrorReadLine();
            _activeProcess.WaitForExit();
        }
        catch (Exception ex)
        {
            ThemeManager.WriteError($"Error: {ex.Message}");
        }
        finally
        {
            _activeProcess = null;
        }
    }

    private static string AutoComplete(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial)) return partial;

        var parts = partial.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var last = parts.LastOrDefault() ?? "";

        // Opciones de autocompletado
        var options = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Builtins
            "help", "exit", "quit", "cls", "clear", "cd", "dir", "ls", "lsd",
            "whoami", "theme", "runas", "sysinfo", "about", "sound", "history",
            "alias", "unalias", "config", "plugins", "reload", "banner", "prompt",
            
            // Linux
            "cat", "touch", "rm", "cp", "mv", "grep", "pwd", "head", "tail",
            "mkdir", "rmdir", "chmod", "find", "wc", "sort", "uniq", "diff",
            "tar", "curl", "wget", "awk", "sed", "echo", "stat", "file", "which",
            "env", "export", "unset", "printenv",
            
            // Network
            "ping", "tracert", "ipinfo", "netstat", "nmap", "nslookup",
            "httpget", "httphead", "httppost", "ftplist", "ftpget", "ftpput", "ssh"
        };

        // Agregar aliases
        foreach (var alias in Configuration.Aliases.Keys)
            options.Add(alias);

        // Agregar archivos y directorios del directorio actual
        try
        {
            foreach (var d in Directory.GetDirectories(CurrentDirectory))
                options.Add(Path.GetFileName(d)!);
            foreach (var f in Directory.GetFiles(CurrentDirectory))
                options.Add(Path.GetFileName(f)!);
        }
        catch { }

        // Buscar coincidencias
        var matches = options
            .Where(o => o.StartsWith(last, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s)
            .ToList();

        if (matches.Count == 1)
        {
            parts[^1] = matches[0];
            return string.Join(' ', parts);
        }
        else if (matches.Count > 1)
        {
            Console.WriteLine();
            ThemeManager.WriteMuted(string.Join("   ", matches));
            PrintPrompt(CurrentDirectory, false);
            Console.Write(partial);
        }

        return partial;
    }

    private static void LoadCmdDescriptions()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "commands_cmd.json");
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                _cmdDescriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            catch { }
        }
    }

    private static void BeepDone()
    {
        if (!Configuration.Config.SoundEnabled) return;
        
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Beep(880, 80);
                Console.Beep(988, 90);
            }
        }
        catch { }
    }
}