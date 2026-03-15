using System.Reflection;

namespace DaniShell.Plugins;

public static class PluginLoader
{
    private static readonly List<Commands.ICommand> _plugins = new();
    
    public static IReadOnlyList<Commands.ICommand> Plugins => _plugins.AsReadOnly();

    public static void LoadPlugins()
    {
        _plugins.Clear();
        
        var pluginsDir = Core.Configuration.PluginsDir;
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
                Core.ThemeManager.WriteWarning($"Error cargando plugin {Path.GetFileName(dll)}: {ex.Message}");
            }
        }

        if (_plugins.Count > 0)
        {
            Core.ThemeManager.WriteInfo($"Cargados {_plugins.Count} plugin(s)");
        }
    }

    private static void LoadPlugin(string dllPath)
    {
        var assembly = Assembly.LoadFrom(dllPath);
        var commandTypes = assembly.GetTypes()
            .Where(t => typeof(Commands.ICommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in commandTypes)
        {
            if (Activator.CreateInstance(type) is Commands.ICommand command)
            {
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
}
