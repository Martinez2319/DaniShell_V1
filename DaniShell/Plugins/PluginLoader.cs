using System.Reflection;
using System.Text.Json;
using System.Runtime.Loader;

namespace DaniShell.Plugins;

public static class PluginLoader
{
    private static readonly List<Commands.ICommand> _plugins = new();
    private static readonly List<AssemblyLoadContext> _contexts = new();
    
    public static IReadOnlyList<Commands.ICommand> Plugins => _plugins.AsReadOnly();

    public static void LoadPlugins(string? directory = null, bool quiet = false)
    {
        _plugins.Clear();
        foreach (var context in _contexts) context.Unload();
        _contexts.Clear();
        
        var pluginsDir = directory ?? Core.Configuration.PluginsDir;
        if (!Directory.Exists(pluginsDir))
        {
            Directory.CreateDirectory(pluginsDir);
            return;
        }

        var dllFiles = Directory.GetFiles(pluginsDir, "*.dll");
        foreach (var dll in dllFiles)
        {
            try
            {
                LoadPlugin(dll);
            }
            catch (Exception ex)
            {
                if (!quiet) Console.Error.WriteLine($"Error cargando plugin {Path.GetFileName(dll)}: {ex.Message}");
            }
        }

        if (_plugins.Count > 0 && !quiet)
        {
            Core.ThemeManager.WriteInfo($"Cargados {_plugins.Count} plugin(s)");
        }
    }

    private static void LoadPlugin(string dllPath)
    {
        var manifestPath = Path.ChangeExtension(dllPath, ".plugin.json");
        if (!File.Exists(manifestPath)) throw new InvalidDataException("Falta el manifiesto .plugin.json (API 1).");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (!manifest.RootElement.TryGetProperty("apiVersion", out var api) || api.GetInt32() != 1)
            throw new InvalidDataException("Versión de API de plugin incompatible.");
        if (manifest.RootElement.TryGetProperty("minimumVersion", out var minimum) &&
            (!Version.TryParse(minimum.GetString(), out var version) || version > new Version(4, 1, 0)))
            throw new InvalidDataException("El plugin requiere otra versión de DaniShell.");
        var context = new PluginContext(dllPath);
        _contexts.Add(context);
        // Loading from a stream allows replacing/reloading a plugin DLL on Windows.
        using var source = File.OpenRead(dllPath);
        var assembly = context.LoadFromStream(source);
        var commandTypes = assembly.GetTypes()
            .Where(t => typeof(Commands.ICommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in commandTypes)
        {
            if (Activator.CreateInstance(type) is Commands.ICommand command)
            {
                if (string.IsNullOrWhiteSpace(command.Name) || Commands.BuiltinCommands.IsBuiltin(command.Name) || Commands.LinuxCommands.IsLinuxCommand(command.Name) ||
                    Commands.NetworkCommands.IsNetworkCommand(command.Name) || GetPlugin(command.Name) != null)
                    throw new InvalidDataException("Nombre de plugin vacío, duplicado o reservado.");
                _plugins.Add(command);
            }
        }
    }

    public static Commands.ICommand? GetPlugin(string name)
    {
        return _plugins.FirstOrDefault(p => 
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            p.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    public static void ListPlugins()
    {
        if (_plugins.Count == 0)
        {
            Core.ThemeManager.WriteInfo("No hay plugins instalados.");
            Core.ThemeManager.WriteMuted($"Coloca archivos .dll en: {Core.Configuration.PluginsDir}");
            return;
        }

        Core.ThemeManager.WriteLine("Plugins instalados:", "Info");
        foreach (var plugin in _plugins)
        {
            Console.WriteLine($"  {plugin.Name,-15} {plugin.Description}");
            if (plugin.Aliases.Length > 0)
            {
                Core.ThemeManager.WriteMuted($"    Aliases: {string.Join(", ", plugin.Aliases)}");
            }
        }
    }

    public static void ReloadPlugins()
    {
        LoadPlugins();
        Core.ThemeManager.WriteSuccess("Plugins recargados.");
    }

    private sealed class PluginContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        public PluginContext(string path) : base(isCollectible: true) => _resolver = new(path);
        protected override Assembly? Load(AssemblyName name)
        {
            if (name.Name == typeof(Commands.ICommand).Assembly.GetName().Name) return typeof(Commands.ICommand).Assembly;
            var dependency = _resolver.ResolveAssemblyToPath(name);
            return dependency == null ? null : LoadFromAssemblyPath(dependency);
        }
    }
}
