using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Diagnostics;

namespace DaniShell.Commands;

/// <summary>
/// Comandos internos del shell: cd, ls, clear, exit, help, etc.
/// </summary>
public static class BuiltinCommands
{
    public static bool IsBuiltin(string cmd)
    {
        var builtins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "exit", "quit", "cls", "clear", "cd", "dir", "ls", "lsd",
            "whoami", "theme", "runas", "sysinfo", "help", "about",
            "sound", "history", "alias", "unalias", "config",
            "plugins", "reload", "banner", "prompt", "version"
        };
        return builtins.Contains(cmd);
    }

    public static string Execute(string cmd, string[] args, string currentDir)
    {
        return cmd.ToLower() switch
        {
            "exit" or "quit" => Exit(),
            "cls" or "clear" => Clear(),
            "cd" => ChangeDirectory(args, currentDir),
            "dir" or "ls" => ListDirectory(args, currentDir),
            "lsd" => ListDirectoriesOnly(args, currentDir),
            "whoami" => WhoAmI(currentDir),
            "theme" => Theme(args, currentDir),
            "runas" => RunAsAdmin(currentDir),
            "sysinfo" => SystemInfo(currentDir),
            "help" => Help(args, currentDir),
            "about" => About(currentDir),
            "sound" => Sound(args, currentDir),
            "history" => History(args, currentDir),
            "alias" => Alias(args, currentDir),
            "unalias" => Unalias(args, currentDir),
            "config" => Config(args, currentDir),
            "plugins" => PluginsCmd(args, currentDir),
            "reload" => Reload(currentDir),
            "banner" => Banner(currentDir),
            "prompt" => Prompt(args, currentDir),
            "version" => Version(currentDir),
            _ => currentDir
        };
    }

    private static string Exit()
    {
        Environment.Exit(0);
        return "";
    }

    private static string Clear()
    {
        if (Core.Configuration.Config.ShowBanner)
        {
            Core.Shell.DrawBanner();
            Core.Shell.RandomWelcome();
        }
        else
        {
            Console.Clear();
        }
        return "";
    }

    private static string ChangeDirectory(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine(dir);
            return dir;
        }

        var path = args[0].Replace("\"", "");

        // Soporte para cd -
        if (path == "-")
        {
            var prev = Core.Shell.PreviousDirectory;
            if (!string.IsNullOrEmpty(prev) && Directory.Exists(prev))
            {
                Core.Shell.PreviousDirectory = dir;
                Directory.SetCurrentDirectory(prev);
                return prev;
            }
            Core.ThemeManager.WriteError("No hay directorio anterior.");
            return dir;
        }

        // Expansión de ~
        if (path == "~" || path.StartsWith("~/") || path.StartsWith("~\\"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = path.Length == 1 ? home : Path.Combine(home, path[2..]);
        }

        // Ruta relativa o absoluta
        if (!Path.IsPathRooted(path))
            path = Path.Combine(dir, path);

        path = Path.GetFullPath(path);

        if (Directory.Exists(path))
        {
            Core.Shell.PreviousDirectory = dir;
            Directory.SetCurrentDirectory(path);
            return path;
        }

        Core.ThemeManager.WriteError("Directorio no encontrado.");
        return dir;
    }

    private static string ListDirectory(string[] args, string dir)
    {
        var targetDir = dir;
        bool showAll = args.Contains("-a") || args.Contains("-la") || args.Contains("-al");
        bool longFormat = args.Contains("-l") || args.Contains("-la") || args.Contains("-al");

        var pathArg = args.FirstOrDefault(a => !a.StartsWith("-"));
        if (pathArg != null)
        {
            var possible = Path.IsPathRooted(pathArg) ? pathArg : Path.Combine(dir, pathArg);
            if (Directory.Exists(possible))
                targetDir = possible;
        }

        try
        {
            var dirs = Directory.GetDirectories(targetDir);
            var files = Directory.GetFiles(targetDir);

            if (!showAll)
            {
                dirs = dirs.Where(d => !Path.GetFileName(d).StartsWith('.')).ToArray();
                files = files.Where(f => !Path.GetFileName(f).StartsWith('.')).ToArray();
            }

            Core.ThemeManager.WriteLine($"📂 Listando: {targetDir}", "Info");

            int windowWidth = Math.Max(Console.WindowWidth, 40);
            int maxLen = Math.Max(
                dirs.Select(d => Path.GetFileName(d).Length).DefaultIfEmpty(10).Max(),
                files.Select(f => Path.GetFileName(f).Length).DefaultIfEmpty(10).Max());
            int colWidth = Math.Max(maxLen + 6, 20);
            int columns = Math.Max(1, windowWidth / colWidth);

            // Directorios
            if (dirs.Length > 0)
            {
                Core.ThemeManager.WriteLine("📁 Directorios:", "Directory");

                if (longFormat)
                {
                    foreach (var d in dirs)
                    {
                        var info = new DirectoryInfo(d);
                        Core.ThemeManager.Write($"  d----  {info.LastWriteTime:yyyy-MM-dd HH:mm}  ", "Muted");
                        Core.ThemeManager.WriteLine(Path.GetFileName(d), "Directory");
                    }
                }
                else
                {
                    int count = 0;
                    foreach (var d in dirs)
                    {
                        Core.ThemeManager.Write(("📁 " + Path.GetFileName(d)).PadRight(colWidth), "Directory");
                        if (++count % columns == 0) Console.WriteLine();
                    }
                    Console.WriteLine("");
                }
            }

            // Archivos
            if (files.Length > 0)
            {
                Core.ThemeManager.WriteLine("📄 Archivos:", "File");

                if (longFormat)
                {
                    foreach (var f in files)
                    {
                        var info = new FileInfo(f);
                        Core.ThemeManager.Write($"  -----  {info.LastWriteTime:yyyy-MM-dd HH:mm}  {info.Length,10}  ", "Muted");
                        Core.ThemeManager.WriteLine(GetFileIcon(f) + " " + Path.GetFileName(f), "File");
                    }
                }
                else
                {
                    int count = 0;
                    foreach (var f in files)
                    {
                        Core.ThemeManager.Write((GetFileIcon(f) + " " + Path.GetFileName(f)).PadRight(colWidth), "File");
                        if (++count % columns == 0) Console.WriteLine();
                    }
                    Console.WriteLine("");
                }
            }

            Core.ThemeManager.WriteMuted($"  {dirs.Length} directorio(s), {files.Length} archivo(s)");
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"Error al listar: {ex.Message}");
        }

        return dir;
    }

    private static string GetFileIcon(string file)
    {
        var ext = Path.GetExtension(file).ToLower();
        return ext switch
        {
            ".txt" or ".md" or ".log" => "📝",
            ".json" => "🧩",
            ".xml" or ".html" or ".htm" => "🌐",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".ico" => "🖼️",
            ".exe" => "⚙️",
            ".dll" => "🔧",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
            ".cs" or ".js" or ".py" or ".ts" or ".java" or ".cpp" or ".c" or ".h" => "💻",
            ".css" or ".scss" or ".sass" => "🎨",
            ".pdf" => "📕",
            ".doc" or ".docx" => "📘",
            ".xls" or ".xlsx" => "📗",
            ".ppt" or ".pptx" => "📙",
            ".mp3" or ".wav" or ".flac" or ".ogg" => "🎵",
            ".mp4" or ".avi" or ".mkv" or ".mov" => "🎬",
            ".sql" or ".db" => "🗄️",
            ".bat" or ".cmd" or ".ps1" or ".sh" => "📜",
            _ => "📄"
        };
    }

    private static string ListDirectoriesOnly(string[] args, string dir)
    {
        var targetDir = args.Length > 0 ? Path.Combine(dir, args[0]) : dir;
        if (!Directory.Exists(targetDir))
        {
            Core.ThemeManager.WriteError($"Directorio no encontrado: {targetDir}");
            return dir;
        }

        var dirs = Directory.GetDirectories(targetDir);
        foreach (var d in dirs)
        {
            Core.ThemeManager.WriteLine("📁 " + Path.GetFileName(d), "Directory");
        }
        return dir;
    }

    private static string WhoAmI(string dir)
    {
        var user = Environment.UserName;
        var domain = Environment.UserDomainName;
        var isAdmin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        Console.WriteLine($"{domain}\\{user}");
        if (isAdmin)
            Core.ThemeManager.WriteSuccess("(Ejecutando como Administrador)");
        return dir;
    }

    private static string Theme(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Core.ThemeManager.ListThemes();
            return dir;
        }

        var action = args[0].ToLower();

        switch (action)
        {
            case "create" when args.Length > 1:
                Core.ThemeManager.CreateTheme(args[1]);
                break;
            case "edit" when args.Length > 1:
                Core.ThemeManager.EditTheme(args[1]);
                break;
            case "delete" when args.Length > 1:
                Core.ThemeManager.DeleteTheme(args[1]);
                break;
            default:
                Core.ThemeManager.SetTheme(action);
                break;
        }
        return dir;
    }

    private static string RunAsAdmin(string dir)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

            if (isAdmin)
            {
                Console.WriteLine("Ya estás ejecutando como administrador.");
                return dir;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath!,
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = dir
                };
                Process.Start(psi);
                Environment.Exit(0);
            }
            catch
            {
                Core.ThemeManager.WriteError("No se pudo reiniciar como administrador.");
            }
        }
        else
        {
            Console.WriteLine("Este comando solo está disponible en Windows.");
        }
        return dir;
    }

    private static string SystemInfo(string dir)
    {
        Console.WriteLine();
        Core.ThemeManager.WriteLine("=== Información del Sistema ===", "Info");
        Console.WriteLine($"  Usuario: {Environment.UserName}");
        Console.WriteLine($"  Equipo: {Environment.MachineName}");
        Console.WriteLine($"  SO: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"  Arquitectura: {RuntimeInformation.OSArchitecture}");
        Console.WriteLine($"  .NET: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"  Procesadores: {Environment.ProcessorCount}");
        Console.WriteLine($"  Directorio actual: {dir}");

        var isAdmin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        Console.WriteLine($"  Admin: {isAdmin}");
        Console.WriteLine();
        return dir;
    }

    private static string Help(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            ShowMainHelp();
            return dir;
        }

        var topic = args[0].ToLower();
        switch (topic)
        {
            case "linux":
                ShowLinuxHelp();
                break;
            case "net" or "network":
                ShowNetworkHelp();
                break;
            case "alias":
                ShowAliasHelp();
                break;
            case "theme":
                ShowThemeHelp();
                break;
            case "config":
                ShowConfigHelp();
                break;
            default:
                ShowCommandHelp(topic);
                break;
        }
        return dir;
    }

    private static void ShowMainHelp()
    {
        Core.ThemeManager.WriteLine("=== DaniShell v4.0 - Ayuda ===", "Banner");

        Core.ThemeManager.WriteLine("Comandos Internos:", "Info");
        Console.WriteLine("  cd, ls/dir, clear/cls, whoami, theme, runas, sysinfo, about");
        Console.WriteLine("  sound, history, alias, unalias, config, plugins, banner, prompt, exit");

        Core.ThemeManager.WriteLine("Comandos Linux:", "Info");
        Console.WriteLine("  cat, touch, rm, cp, mv, grep, pwd, head, tail, mkdir, rmdir");
        Console.WriteLine("  chmod, find, wc, sort, uniq, diff, tar, curl, wget");
        Console.WriteLine("  awk, sed, echo, basename, dirname, stat, file, which");
        Console.WriteLine("  env, export, unset, printenv");

        Core.ThemeManager.WriteLine("Comandos de Red:", "Info");
        Console.WriteLine("  ping, tracert, ipinfo, netstat, nmap, nslookup");
        Console.WriteLine("  httpget, httphead, httppost, ftplist, ftpget, ftpput, ssh");

        Core.ThemeManager.WriteLine("Ayuda específica:", "Muted");
        Console.WriteLine("  help linux  - Comandos Linux detallados");
        Console.WriteLine("  help net    - Comandos de red");
        Console.WriteLine("  help alias  - Sistema de aliases");
        Console.WriteLine("  help theme  - Personalización de temas");
        Console.WriteLine("  help config - Configuración");
        Console.WriteLine();
    }

    private static void ShowLinuxHelp()
    {
        Core.ThemeManager.WriteLine("=== Comandos Linux ===", "Banner");
        Console.WriteLine(@"
  Archivos:
    cat [-n] archivo          Mostrar contenido (-n: números de línea)
    touch archivo             Crear archivo o actualizar timestamp
    rm [-rf] archivo          Eliminar archivo (-r: recursivo, -f: forzar)
    cp [-r] origen destino    Copiar archivo/directorio
    mv origen destino         Mover/renombrar

  Búsqueda:
    grep [-inrv] patrón arch  Buscar texto en archivos
    find [dir] -name patrón   Buscar archivos por nombre
    which comando             Buscar ejecutable en PATH

  Texto:
    head [-n N] archivo       Mostrar primeras N líneas
    tail [-n N] archivo       Mostrar últimas N líneas
    wc [-lwc] archivo         Contar líneas/palabras/caracteres
    sort [-rnu] archivo       Ordenar líneas
    uniq [-cdu] archivo       Eliminar duplicados consecutivos
    diff archivo1 archivo2    Comparar archivos

  Procesamiento:
    awk '{print $N}' archivo  Extraer columnas
    sed 's/a/b/g' archivo     Buscar y reemplazar

  Utilidades:
    echo texto                Mostrar texto
    pwd                       Directorio actual
    stat archivo              Información del archivo
    file archivo              Tipo de archivo

  Variables de entorno:
    env                       Mostrar todas las variables
    export VAR=valor          Establecer variable
    unset VAR                 Eliminar variable
    printenv VAR              Mostrar variable
");
    }

    private static void ShowNetworkHelp()
    {
        Core.ThemeManager.WriteLine("=== Comandos de Red ===", "Banner");
        Console.WriteLine(@"
  Diagnóstico:
    ping host                 Probar conectividad
    tracert host              Trazar ruta
    ipinfo                    Información IP local
    netstat                   Conexiones activas
    nslookup dominio          Consulta DNS
    nmap objetivo [-p puertos] Escaneo de puertos

  HTTP:
    httpget url [archivo]     Descargar URL
    httphead url              Obtener headers
    httppost url key=val      Enviar POST

  FTP:
    ftplist ftp://user:pass@host/ruta   Listar directorio
    ftpget ftp://... [archivo]          Descargar archivo
    ftpput archivo ftp://...            Subir archivo

  SSH:
    ssh user@host [comando]   Conexión SSH
");
    }

    private static void ShowAliasHelp()
    {
        Core.ThemeManager.WriteLine("=== Sistema de Aliases ===", "Banner");
        Console.WriteLine(@"
  Comandos:
    alias                     Listar todos los aliases
    alias nombre=comando      Crear alias
    unalias nombre            Eliminar alias

  Ejemplos:
    alias ll=ls -la           Listar con detalles
    alias gs=git status       Atajo para git
    alias ..=cd ..            Subir directorio

  Los aliases se guardan en: ~/.danishell/aliases.json
  También puedes editar el archivo directamente.
");
    }

    private static void ShowThemeHelp()
    {
        Core.ThemeManager.WriteLine("=== Personalización de Temas ===", "Banner");
        Console.WriteLine(@"
Comandos:
    theme                     Listar temas disponibles
    theme nombre              Cambiar al tema
    theme create nombre       Crear nuevo tema
    theme edit nombre         Editar colores del tema
    theme delete nombre       Eliminar tema personalizado

  Temas predeterminados: dark, light, hacker, ocean, fire, cyberpunk, neon

  Colores disponibles:
    Black, DarkBlue, DarkGreen, DarkCyan, DarkRed, DarkMagenta,
    DarkYellow, Gray, DarkGray, Blue, Green, Cyan, Red, Magenta,
    Yellow, White

  Los temas se guardan en: ~/.danishell/themes.json");
    }

    private static void ShowConfigHelp()
    {
        Core.ThemeManager.WriteLine("=== Configuración ===", "Banner");
        Console.WriteLine(@"
  Comandos:
    config                    Mostrar configuración actual
    config set key value      Establecer valor
    config reset              Restaurar valores por defecto

  Opciones disponibles:
    theme          Tema de colores
    sound          Sonido on/off
    prompt         Formato del prompt
    startdir       Directorio inicial
    banner         Mostrar banner on/off
    dateformat     Formato de fecha
    historylimit   Límite de historial

  Archivos de configuración:
    ~/.danishell/config.json    Configuración general
    ~/.danishell/aliases.json   Aliases personalizados
    ~/.danishell/themes.json    Temas personalizados
    ~/.danishell/history        Historial de comandos
    ~/.danishell/plugins/       Directorio de plugins");
    }

    private static void ShowCommandHelp(string cmd)
    {
        var cmdHelp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cd"] = "cd [dir] - Cambia el directorio actual. Soporta ~, -, ..",
            ["ls"] = "ls [-la] [dir] - Lista archivos y directorios",
            ["cat"] = "cat [-n] archivo - Muestra el contenido del archivo",
            ["grep"] = "grep [-inrv] patrón archivo - Busca texto en archivos",
            ["rm"] = "rm [-rf] archivo - Elimina archivos o directorios",
            ["cp"] = "cp [-r] origen destino - Copia archivos o directorios",
            ["mv"] = "mv origen destino - Mueve o renombra archivos",
            ["mkdir"] = "mkdir [-p] directorio - Crea directorios",
            ["touch"] = "touch archivo - Crea archivo vacío o actualiza timestamp",
        };

        if (cmdHelp.TryGetValue(cmd, out var help))
        {
            Console.WriteLine($"{help}");
        }
        else
        {
            Core.ThemeManager.WriteWarning($"No hay ayuda específica para '{cmd}'");
            Core.ThemeManager.WriteMuted("Usa 'help' para ver todos los comandos disponibles.");
        }
    }

    private static string About(string dir)
    {
        Core.ThemeManager.WriteLine("=== DaniShell ===", "Banner");
        Console.WriteLine($"  Versión: 4.0.0");
        Console.WriteLine($"  Autor: Daniel");
        Console.WriteLine($"  Ubicación: Ciudad Juárez, Chihuahua 🇲🇽");
        Console.WriteLine($"  Descripción: Terminal avanzada con comandos Linux y Windows");
        Console.WriteLine($"  .NET: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($" \"La seguridad no es un producto, es un proceso\"");
        return dir;
    }

    private static string Sound(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine($"Sonido: {(Core.Configuration.Config.SoundEnabled ? "on" : "off")}");
            return dir;
        }

        var value = args[0].ToLower();
        Core.Configuration.Config.SoundEnabled = value is "on" or "true" or "1";
        Core.Configuration.SaveConfig();
        Console.WriteLine($"Sonido: {(Core.Configuration.Config.SoundEnabled ? "on" : "off")}");
        return dir;
    }

    private static string History(string[] args, string dir)
    {
        if (args.Length > 0 && args[0] == "-c")
        {
            Core.InputHandler.ClearHistory();
            Core.ThemeManager.WriteSuccess("Historial limpiado.");
            return dir;
        }

        int count = 100;
        if (args.Length > 0 && int.TryParse(args[0], out int n))
            count = n;

        var history = Core.InputHandler.GetHistory();
        var start = Math.Max(0, history.Count - count);

        for (int i = start; i < history.Count; i++)
        {
            Core.ThemeManager.Write($"{i + 1,5}  ", "Muted");
            Console.WriteLine(history[i]);
        }
        return dir;
    }

    private static string Alias(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Core.ThemeManager.WriteLine("Aliases configurados:", "Info");
            foreach (var alias in Core.Configuration.Aliases.OrderBy(a => a.Key))
            {
                Core.ThemeManager.Write($"  {alias.Key}", "Highlight");
                Core.ThemeManager.WriteMuted($" = '{alias.Value}'");
            }
            Console.WriteLine();
            return dir;
        }

        var input = string.Join(" ", args);
        var eqIndex = input.IndexOf('=');

        if (eqIndex > 0)
        {
            var name = input[..eqIndex].Trim();
            var value = input[(eqIndex + 1)..].Trim().Trim(' ', '\'', '"');

            Core.Configuration.Aliases[name] = value;
            Core.Configuration.SaveAliases();
            Core.ThemeManager.WriteSuccess($"alias {name}='{value}'");
        }
        else
        {
            if (Core.Configuration.Aliases.TryGetValue(args[0], out var value))
            {
                Console.WriteLine($"alias {args[0]}='{value}'");
            }
            else
            {
                Core.ThemeManager.WriteError($"alias: {args[0]}: no encontrado");
            }
        }
        return dir;
    }

    private static string Unalias(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: unalias <nombre>");
            return dir;
        }

        foreach (var name in args)
        {
            if (Core.Configuration.Aliases.Remove(name))
            {
                Core.ThemeManager.WriteSuccess($"Eliminado alias: {name}");
            }
            else
            {
                Core.ThemeManager.WriteError($"unalias: {name}: no encontrado");
            }
        }
        Core.Configuration.SaveAliases();
        return dir;
    }

    private static string Config(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Core.ThemeManager.WriteLine("Configuración actual:", "Info");
            Console.WriteLine($"  theme: {Core.Configuration.Config.Theme}");
            Console.WriteLine($"  sound: {(Core.Configuration.Config.SoundEnabled ? "on" : "off")}");
            Console.WriteLine($"  prompt: {Core.Configuration.Config.Prompt}");
            Console.WriteLine($"  startdir: {Core.Configuration.Config.StartDirectory}");
            Console.WriteLine($"  banner: {(Core.Configuration.Config.ShowBanner ? "on" : "off")}");
            Console.WriteLine($"  dateformat: {Core.Configuration.Config.DateFormat}");
            Console.WriteLine($"  historylimit: {Core.Configuration.Config.HistoryLimit}");

            if (Core.Configuration.Config.EnvironmentVars.Count > 0)
            {
                Console.WriteLine("Variables de entorno:");
                foreach (var env in Core.Configuration.Config.EnvironmentVars)
                    Console.WriteLine($"    {env.Key}={env.Value}");
            }
            Console.WriteLine();
            return dir;
        }

        if (args[0] == "set" && args.Length >= 3)
        {
            var key = args[1].ToLower();
            var value = string.Join(" ", args.Skip(2));

            switch (key)
            {
                case "theme":
                    Core.Configuration.Config.Theme = value;
                    break;
                case "sound":
                    Core.Configuration.Config.SoundEnabled = value is "on" or "true" or "1";
                    break;
                case "prompt":
                    Core.Configuration.Config.Prompt = value;
                    break;
                case "startdir":
                    Core.Configuration.Config.StartDirectory = value;
                    break;
                case "banner":
                    Core.Configuration.Config.ShowBanner = value is "on" or "true" or "1";
                    break;
                case "dateformat":
                    Core.Configuration.Config.DateFormat = value;
                    break;
                case "historylimit":
                    if (int.TryParse(value, out int limit))
                        Core.Configuration.Config.HistoryLimit = limit;
                    break;
                default:
                    Core.ThemeManager.WriteError($"Configuración desconocida: {key}");
                    return dir;
            }

            Core.Configuration.SaveConfig();
            Core.ThemeManager.WriteSuccess($"Configuración actualizada: {key} = {value}");
        }
        else if (args[0] == "reset")
        {
            Core.Configuration.Config = new Core.ShellConfig();
            Core.Configuration.SaveConfig();
            Core.ThemeManager.WriteSuccess("Configuración restaurada a valores por defecto.");
        }
        else
        {
            Console.WriteLine("Uso: config | config set <key> <value> | config reset");
        }
        return dir;
    }

    private static string PluginsCmd(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Plugins.PluginLoader.ListPlugins();
            return dir;
        }

        if (args[0] == "reload")
        {
            Plugins.PluginLoader.ReloadPlugins();
        }
        else if (args[0] == "dir")
        {
            Console.WriteLine($"Directorio de plugins: {Core.Configuration.PluginsDir}");
        }
        return dir;
    }

    private static string Reload(string dir)
    {
        Core.Configuration.LoadConfig();
        Core.Configuration.LoadAliases();
        Core.Configuration.LoadThemes();
        Plugins.PluginLoader.LoadPlugins();
        Core.ThemeManager.WriteSuccess("Configuración recargada.");
        return dir;
    }

    private static string Banner(string dir)
    {
        Core.Shell.DrawBanner();
        return dir;
    }

    private static string Prompt(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine($"Prompt actual: {Core.Configuration.Config.Prompt}");
            Console.WriteLine("Variables disponibles:");
            Console.WriteLine("  {user}  - Nombre de usuario");
            Console.WriteLine("  {host}  - Nombre del equipo");
            Console.WriteLine("  {dir}   - Directorio actual");
            Console.WriteLine("  {time}  - Hora actual");
            Console.WriteLine("  {date}  - Fecha actual");
            return dir;
        }

        Core.Configuration.Config.Prompt = string.Join(" ", args);
        Core.Configuration.SaveConfig();
        Core.ThemeManager.WriteSuccess($"Prompt actualizado: {Core.Configuration.Config.Prompt}");
        return dir;
    }

    private static string Version(string dir)
    {
        Console.WriteLine("DaniShell v4.0.0");
        return dir;
    }
}