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

    public static readonly string Version = "DaniShell v4.1.0";
    public static int LastExitCode { get; private set; }
    public static void SetExitCode(int code) { LastExitCode = code; Environment.SetEnvironmentVariable("DANISHELL_LASTEXITCODE", code.ToString()); }
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
            try { ExecuteLine(input.Trim(), CurrentDirectory); }
            catch (Exception ex) { SetExitCode(2); ThemeManager.WriteError(ex.Message); }
        }
    }

    private static void HandleCtrlC(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        if (PipelineRunner.Cancel()) return;

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
║                              DaniShell v4.1.0                                      ║
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

    private static string HandleCommand(string input, string? standardInput = null)
    {
        try
        {
            var parts = CommandLine.ParseArguments(input);
            if (parts.Length == 0) return CurrentDirectory;

            var cmd = parts[0].ToLowerInvariant();
            int previousCode = LastExitCode;
            LastExitCode = 0;
            var args = parts.Skip(1).ToArray();

            if (cmd == "status") { Console.WriteLine(previousCode); return CurrentDirectory; }
            if (cmd is "exit" or "quit") Environment.Exit(args.Length > 0 && int.TryParse(args[0], out var requested) ? requested : previousCode);
            if (NativeShell.TryShortcut(input, CurrentDirectory, out var nativeCode)) { SetExitCode(nativeCode); return CurrentDirectory; }
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
                var result = Commands.LinuxCommands.Execute(cmd, args, CurrentDirectory, standardInput);
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
                string result;
                if (plugin is Commands.IStreamingCommand streaming)
                { SetExitCode(streaming.ExecuteStream(args, CurrentDirectory, Console.In, Console.Out, Console.Error)); result = CurrentDirectory; }
                else result = plugin.Execute(args, CurrentDirectory);
                BeepDone();
                return string.IsNullOrEmpty(result) ? CurrentDirectory : result;
            }

            // 5. Ejecutar como comando externo (CMD/PowerShell)
            ExecuteExternal(input, CurrentDirectory, standardInput);
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

    public static string ExecuteLine(string input, string directory, string? standardInput = null)
    {
        CurrentDirectory = CommandExecutor.Execute(ExpandAlias(input), directory, standardInput);
        return CurrentDirectory;
    }
    public static string DispatchStage(string input, string directory, bool hasInput)
    {
        CurrentDirectory = directory;
        var result = HandleCommand(ExpandAlias(input), hasInput ? "" : null);
        Environment.SetEnvironmentVariable("DANISHELL_LASTEXITCODE", LastExitCode.ToString());
        return result;
    }

    public static void ExecuteExternal(string input, string workDir, string? standardInput = null)
    {
        var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var arguments = windows ? new[] { "/d", "/s", "/c", input } : new[] { "-c", input };
        var psi = ProcessRunner.Create(windows ? "cmd.exe" : "/bin/bash", arguments, workDir);
        // CMD expects its command expression as a single raw command line.
        if (windows)
        {
            psi.ArgumentList.Clear();
            psi.Arguments = "/d /s /c \"" + input + "\"";
        }
        SetExitCode(ProcessRunner.Run(psi, active: TrackProcess, inputReader: standardInput != null ? Console.In : null));
    }

    public static void TrackProcess(Process? process) => _activeProcess = process;

    private static string AutoComplete(string partial) => Completion.Candidates(partial, CurrentDirectory).FirstOrDefault() ?? partial;

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
