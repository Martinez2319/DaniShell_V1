using DaniShell.Core;

using System.Text;
using System.Text.Json;
using DaniShell.Plugins;

namespace DaniShell;

class Program
{
    static int Main(string[] args)
    {
        Console.InputEncoding = new UTF8Encoding(false);
        Console.OutputEncoding = new UTF8Encoding(false);
        try
        {
            if (args.FirstOrDefault() is "--help" or "-h")
            {
                Console.WriteLine("DaniShell [--shell cmd|powershell] | [-c comando] | [--shell cmd|powershell -c script]");
                return 0;
            }
            if (args.FirstOrDefault() == "--version") { Console.WriteLine(Shell.Version); return 0; }
            Configuration.Initialize();
            if (args.FirstOrDefault() == "--stage")
            {
                if (args.Length != 2) throw new ArgumentException("Etapa inválida.");
                if (Environment.GetEnvironmentVariable("DANISHELL_STAGE_CONFIG") is string config)
                    Configuration.Config = JsonSerializer.Deserialize<ShellConfig>(config) ?? new();
                Configuration.Config.SoundEnabled = false;
                if (Environment.GetEnvironmentVariable("DANISHELL_STAGE_ALIASES") is string aliases)
                    foreach (var item in JsonSerializer.Deserialize<Dictionary<string, string>>(aliases) ?? new()) Configuration.Aliases[item.Key] = item.Value;
                PluginLoader.LoadPlugins(quiet: true);
                Shell.DispatchStage(args[1], Directory.GetCurrentDirectory(), Console.IsInputRedirected);
                return Shell.LastExitCode;
            }
            if (args.FirstOrDefault() == "--shell")
            {
                if (args.Length < 2 || args[1] is not ("cmd" or "powershell")) throw new ArgumentException("Motor esperado: cmd o powershell.");
                if (args.Length == 2) return NativeShell.Open(args[1], Directory.GetCurrentDirectory());
                if (args.Length == 4 && args[2] is "-c" or "--command")
                    return ProcessRunner.Run(NativeShell.Command(args[1], args[3], Directory.GetCurrentDirectory()));
                throw new ArgumentException("Uso: --shell cmd|powershell [-c script]");
            }
            if (args.Length > 0)
            {
                if (args.Length != 2 || args[0] is not ("-c" or "--command")) throw new ArgumentException("Usa --help para ver las opciones.");
                PluginLoader.LoadPlugins(quiet: true);
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; PipelineRunner.Cancel(); };
                Shell.ExecuteLine(args[1], Directory.GetCurrentDirectory());
                return Shell.LastExitCode;
            }
            if (Console.IsInputRedirected)
            {
                PluginLoader.LoadPlugins(quiet: true);
                string directory = Directory.GetCurrentDirectory(); string? line;
                while ((line = Console.ReadLine()) != null) directory = Shell.ExecuteLine(line, directory);
                return Shell.LastExitCode;
            }
            Shell.Run();
            return Shell.LastExitCode;
        }
        catch (IOException) when (args.FirstOrDefault() == "--stage") { return 1; }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 2; }
    }
}
