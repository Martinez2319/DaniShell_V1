using System.Text;
using System.Text.RegularExpressions;

namespace DaniShell.Commands;

/// <summary>
/// Comandos Linux implementados nativamente en DaniShell
/// Incluye: cat, touch, rm, cp, mv, grep, pwd, head, tail, mkdir, rmdir,
/// chmod, find, wc, sort, uniq, diff, tar, curl, wget, awk, sed, xargs, tee
/// </summary>
public static class LinuxCommands
{
    public static string Execute(string cmd, string[] args, string currentDir)
    {
        try
        {
            return cmd.ToLower() switch
            {
                "cat" => Cat(args, currentDir),
                "touch" => Touch(args, currentDir),
                "rm" => Rm(args, currentDir),
                "cp" => Cp(args, currentDir),
                "mv" => Mv(args, currentDir),
                "grep" => Grep(args, currentDir),
                "pwd" => Pwd(currentDir),
                "head" => Head(args, currentDir),
                "tail" => Tail(args, currentDir),
                "mkdir" => Mkdir(args, currentDir),
                "rmdir" => Rmdir(args, currentDir),
                "chmod" => Chmod(args, currentDir),
                "find" => Find(args, currentDir),
                "wc" => Wc(args, currentDir),
                "sort" => Sort(args, currentDir),
                "uniq" => Uniq(args, currentDir),
                "diff" => Diff(args, currentDir),
                "tar" => Tar(args, currentDir),
                "curl" => Curl(args),
                "wget" => Wget(args, currentDir),
                "awk" => Awk(args, currentDir),
                "sed" => Sed(args, currentDir),
                "xargs" => Xargs(args, currentDir),
                "tee" => Tee(args, currentDir),
                "echo" => Echo(args),
                "basename" => Basename(args),
                "dirname" => Dirname(args),
                "realpath" => Realpath(args, currentDir),
                "ln" => Ln(args, currentDir),
                "stat" => Stat(args, currentDir),
                "file" => FileCmd(args, currentDir),
                "which" => Which(args),
                "env" => Env(args),
                "export" => Export(args),
                "unset" => Unset(args),
                "printenv" => PrintEnv(args),
                _ => null!
            };
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"Error: {ex.Message}");
            return currentDir;
        }
    }

    public static bool IsLinuxCommand(string cmd)
    {
        var linuxCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cat", "touch", "rm", "cp", "mv", "grep", "pwd", "head", "tail",
            "mkdir", "rmdir", "chmod", "find", "wc", "sort", "uniq", "diff",
            "tar", "curl", "wget", "awk", "sed", "xargs", "tee", "echo",
            "basename", "dirname", "realpath", "ln", "stat", "file", "which",
            "env", "export", "unset", "printenv"
        };
        return linuxCommands.Contains(cmd);
    }

    // === cat: Mostrar contenido de archivos ===
    private static string Cat(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: cat <archivo> [archivo2] [-n]");
            return dir;
        }

        bool showLineNumbers = args.Contains("-n");
        var files = args.Where(a => !a.StartsWith("-")).ToArray();

        foreach (var file in files)
        {
            var path = GetFullPath(file, dir);
            if (!File.Exists(path))
            {
                Core.ThemeManager.WriteError($"cat: {file}: No existe el archivo");
                continue;
            }

            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (showLineNumbers)
                    Console.WriteLine($"{i + 1,6}  {lines[i]}");
                else
                    Console.WriteLine(lines[i]);
            }
        }
        return dir;
    }

    // === touch: Crear archivo vacío o actualizar timestamp ===
    private static string Touch(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: touch <archivo> [archivo2] ...");
            return dir;
        }

        foreach (var file in args)
        {
            var path = GetFullPath(file, dir);
            if (File.Exists(path))
            {
                File.SetLastWriteTime(path, DateTime.Now);
            }
            else
            {
                File.Create(path).Close();
                Core.ThemeManager.WriteSuccess($"Creado: {file}");
            }
        }
        return dir;
    }

    // === rm: Eliminar archivos/directorios ===
    private static string Rm(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: rm [-r] [-f] <archivo/directorio> ...");
            return dir;
        }

        bool recursive = args.Contains("-r") || args.Contains("-R") || args.Contains("-rf");
        bool force = args.Contains("-f") || args.Contains("-rf");
        var targets = args.Where(a => !a.StartsWith("-")).ToArray();

        foreach (var target in targets)
        {
            var path = GetFullPath(target, dir);

            if (Directory.Exists(path))
            {
                if (!recursive)
                {
                    Core.ThemeManager.WriteError($"rm: {target}: Es un directorio (usa -r)");
                    continue;
                }
                Directory.Delete(path, true);
                Core.ThemeManager.WriteSuccess($"Eliminado directorio: {target}");
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
                Core.ThemeManager.WriteSuccess($"Eliminado: {target}");
            }
            else if (!force)
            {
                Core.ThemeManager.WriteError($"rm: {target}: No existe");
            }
        }
        return dir;
    }

    // === cp: Copiar archivos/directorios ===
    private static string Cp(string[] args, string dir)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: cp [-r] <origen> <destino>");
            return dir;
        }

        bool recursive = args.Contains("-r") || args.Contains("-R");
        var paths = args.Where(a => !a.StartsWith("-")).ToArray();

        if (paths.Length < 2)
        {
            Console.WriteLine("Uso: cp [-r] <origen> <destino>");
            return dir;
        }

        var source = GetFullPath(paths[0], dir);
        var dest = GetFullPath(paths[1], dir);

        if (Directory.Exists(source))
        {
            if (!recursive)
            {
                Core.ThemeManager.WriteError("cp: Es un directorio (usa -r)");
                return dir;
            }
            CopyDirectory(source, dest);
            Core.ThemeManager.WriteSuccess($"Copiado directorio: {paths[0]} -> {paths[1]}");
        }
        else if (File.Exists(source))
        {
            if (Directory.Exists(dest))
                dest = Path.Combine(dest, Path.GetFileName(source));
            File.Copy(source, dest, true);
            Core.ThemeManager.WriteSuccess($"Copiado: {paths[0]} -> {paths[1]}");
        }
        else
        {
            Core.ThemeManager.WriteError($"cp: {paths[0]}: No existe");
        }
        return dir;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        foreach (var subdir in Directory.GetDirectories(source))
            CopyDirectory(subdir, Path.Combine(dest, Path.GetFileName(subdir)));
    }

    // === mv: Mover/renombrar archivos ===
    private static string Mv(string[] args, string dir)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: mv <origen> <destino>");
            return dir;
        }

        var source = GetFullPath(args[0], dir);
        var dest = GetFullPath(args[1], dir);

        if (!File.Exists(source) && !Directory.Exists(source))
        {
            Core.ThemeManager.WriteError($"mv: {args[0]}: No existe");
            return dir;
        }

        if (Directory.Exists(dest) && File.Exists(source))
            dest = Path.Combine(dest, Path.GetFileName(source));

        if (File.Exists(source))
            File.Move(source, dest, true);
        else
            Directory.Move(source, dest);

        Core.ThemeManager.WriteSuccess($"Movido: {args[0]} -> {args[1]}");
        return dir;
    }

    // === grep: Buscar texto en archivos ===
    private static string Grep(string[] args, string dir)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Uso: grep [-i] [-n] [-r] [-v] <patrón> [archivo...]");
            return dir;
        }

        bool ignoreCase = args.Contains("-i");
        bool showLineNum = args.Contains("-n");
        bool recursive = args.Contains("-r");
        bool invert = args.Contains("-v");

        var nonFlags = args.Where(a => !a.StartsWith("-")).ToArray();
        if (nonFlags.Length == 0)
        {
            Console.WriteLine("Uso: grep [-i] [-n] [-r] [-v] <patrón> [archivo...]");
            return dir;
        }

        var pattern = nonFlags[0];
        var files = nonFlags.Skip(1).ToArray();

        if (files.Length == 0)
        {
            files = recursive 
                ? Directory.GetFiles(dir, "*", SearchOption.AllDirectories) 
                : Directory.GetFiles(dir);
        }
        else
        {
            files = files.Select(f => GetFullPath(f, dir)).ToArray();
        }

        var regexOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        var regex = new Regex(pattern, regexOptions);

        foreach (var file in files)
        {
            if (!File.Exists(file)) continue;
            
            try
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    bool matches = regex.IsMatch(lines[i]);
                    if (invert) matches = !matches;

                    if (matches)
                    {
                        var prefix = files.Length > 1 ? $"{Path.GetFileName(file)}:" : "";
                        var lineNum = showLineNum ? $"{i + 1}:" : "";
                        
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(prefix);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(lineNum);
                        Console.ResetColor();
                        
                        // Resaltar coincidencias
                        var line = lines[i];
                        var match = regex.Match(line);
                        int lastEnd = 0;
                        while (match.Success)
                        {
                            Console.Write(line[lastEnd..match.Index]);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(match.Value);
                            Console.ResetColor();
                            lastEnd = match.Index + match.Length;
                            match = match.NextMatch();
                        }
                        Console.WriteLine(line[lastEnd..]);
                    }
                }
            }
            catch { }
        }
        return dir;
    }

    // === pwd: Mostrar directorio actual ===
    private static string Pwd(string dir)
    {
        Console.WriteLine(dir);
        return dir;
    }

    // === head: Mostrar primeras líneas ===
    private static string Head(string[] args, string dir)
    {
        int lines = 10;
        var files = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-n" && i + 1 < args.Length)
            {
                int.TryParse(args[++i], out lines);
            }
            else if (args[i].StartsWith("-") && int.TryParse(args[i][1..], out int n))
            {
                lines = n;
            }
            else if (!args[i].StartsWith("-"))
            {
                files.Add(args[i]);
            }
        }

        if (files.Count == 0)
        {
            Console.WriteLine("Uso: head [-n <líneas>] <archivo>");
            return dir;
        }

        foreach (var file in files)
        {
            var path = GetFullPath(file, dir);
            if (!File.Exists(path))
            {
                Core.ThemeManager.WriteError($"head: {file}: No existe");
                continue;
            }

            if (files.Count > 1)
                Console.WriteLine($"==> {file} <==");

            var content = File.ReadLines(path).Take(lines);
            foreach (var line in content)
                Console.WriteLine(line);
        }
        return dir;
    }

    // === tail: Mostrar últimas líneas ===
    private static string Tail(string[] args, string dir)
    {
        int lines = 10;
        var files = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-n" && i + 1 < args.Length)
            {
                int.TryParse(args[++i], out lines);
            }
            else if (args[i].StartsWith("-") && int.TryParse(args[i][1..], out int n))
            {
                lines = n;
            }
            else if (!args[i].StartsWith("-"))
            {
                files.Add(args[i]);
            }
        }

        if (files.Count == 0)
        {
            Console.WriteLine("Uso: tail [-n <líneas>] <archivo>");
            return dir;
        }

        foreach (var file in files)
        {
            var path = GetFullPath(file, dir);
            if (!File.Exists(path))
            {
                Core.ThemeManager.WriteError($"tail: {file}: No existe");
                continue;
            }

            if (files.Count > 1)
                Console.WriteLine($"==> {file} <==");

            var allLines = File.ReadAllLines(path);
            var content = allLines.Skip(Math.Max(0, allLines.Length - lines));
            foreach (var line in content)
                Console.WriteLine(line);
        }
        return dir;
    }

    // === mkdir: Crear directorio ===
    private static string Mkdir(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: mkdir [-p] <directorio> ...");
            return dir;
        }

        bool parents = args.Contains("-p");
        var dirs = args.Where(a => !a.StartsWith("-")).ToArray();

        foreach (var d in dirs)
        {
            var path = GetFullPath(d, dir);
            if (Directory.Exists(path))
            {
                if (!parents)
                    Core.ThemeManager.WriteWarning($"mkdir: {d}: Ya existe");
                continue;
            }
            Directory.CreateDirectory(path);
            Core.ThemeManager.WriteSuccess($"Creado: {d}");
        }
        return dir;
    }

    // === rmdir: Eliminar directorio vacío ===
    private static string Rmdir(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: rmdir <directorio> ...");
            return dir;
        }

        foreach (var d in args)
        {
            var path = GetFullPath(d, dir);
            if (!Directory.Exists(path))
            {
                Core.ThemeManager.WriteError($"rmdir: {d}: No existe");
                continue;
            }
            if (Directory.GetFileSystemEntries(path).Length > 0)
            {
                Core.ThemeManager.WriteError($"rmdir: {d}: Directorio no vacío");
                continue;
            }
            Directory.Delete(path);
            Core.ThemeManager.WriteSuccess($"Eliminado: {d}");
        }
        return dir;
    }

    // === chmod: Cambiar permisos (simulado en Windows) ===
    private static string Chmod(string[] args, string dir)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: chmod <permisos> <archivo>");
            Console.WriteLine("En Windows: +r (solo lectura), -r (quitar solo lectura)");
            return dir;
        }

        var mode = args[0];
        var file = GetFullPath(args[1], dir);

        if (!File.Exists(file) && !Directory.Exists(file))
        {
            Core.ThemeManager.WriteError($"chmod: {args[1]}: No existe");
            return dir;
        }

        var attrs = File.GetAttributes(file);

        if (mode.Contains("+r") || mode == "444" || mode == "555")
        {
            File.SetAttributes(file, attrs | FileAttributes.ReadOnly);
            Core.ThemeManager.WriteSuccess($"Establecido solo lectura: {args[1]}");
        }
        else if (mode.Contains("-r") || mode == "644" || mode == "755" || mode == "777")
        {
            File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);
            Core.ThemeManager.WriteSuccess($"Quitado solo lectura: {args[1]}");
        }
        else
        {
            Core.ThemeManager.WriteInfo($"chmod: En Windows solo soporta +r/-r para solo lectura");
        }
        return dir;
    }

    // === find: Buscar archivos ===
    private static string Find(string[] args, string dir)
    {
        string searchDir = dir;
        string pattern = "*";
        string namePattern = "";
        string typeFilter = "";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-name" when i + 1 < args.Length:
                    namePattern = args[++i];
                    break;
                case "-type" when i + 1 < args.Length:
                    typeFilter = args[++i];
                    break;
                default:
                    if (!args[i].StartsWith("-") && i == 0)
                        searchDir = GetFullPath(args[i], dir);
                    break;
            }
        }

        if (!Directory.Exists(searchDir))
        {
            Core.ThemeManager.WriteError($"find: {searchDir}: No existe");
            return dir;
        }

        var entries = Directory.GetFileSystemEntries(searchDir, "*", SearchOption.AllDirectories);
        
        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            
            if (!string.IsNullOrEmpty(namePattern))
            {
                var regex = new Regex("^" + Regex.Escape(namePattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", 
                    RegexOptions.IgnoreCase);
                if (!regex.IsMatch(name))
                    continue;
            }

            if (typeFilter == "f" && !File.Exists(entry)) continue;
            if (typeFilter == "d" && !Directory.Exists(entry)) continue;

            Console.WriteLine(entry);
        }
        return dir;
    }

    // === wc: Contar líneas, palabras, caracteres ===
    private static string Wc(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: wc [-l] [-w] [-c] <archivo> ...");
            return dir;
        }

        bool countLines = args.Contains("-l") || !args.Any(a => a.StartsWith("-"));
        bool countWords = args.Contains("-w") || !args.Any(a => a.StartsWith("-"));
        bool countChars = args.Contains("-c") || !args.Any(a => a.StartsWith("-"));

        var files = args.Where(a => !a.StartsWith("-")).ToArray();
        int totalLines = 0, totalWords = 0, totalChars = 0;

        foreach (var file in files)
        {
            var path = GetFullPath(file, dir);
            if (!File.Exists(path))
            {
                Core.ThemeManager.WriteError($"wc: {file}: No existe");
                continue;
            }

            var content = File.ReadAllText(path);
            var lines = content.Split(' ').Length;
            var words = content.Split(new[] { ' ', '	', ' ', ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var chars = content.Length;

            totalLines += lines;
            totalWords += words;
            totalChars += chars;

            var output = new StringBuilder();
            if (countLines) output.Append($"{lines,8}");
            if (countWords) output.Append($"{words,8}");
            if (countChars) output.Append($"{chars,8}");
            output.Append($" {file}");
            Console.WriteLine(output.ToString());
        }

        if (files.Length > 1)
        {
            var output = new StringBuilder();
            if (countLines) output.Append($"{totalLines,8}");
            if (countWords) output.Append($"{totalWords,8}");
            if (countChars) output.Append($"{totalChars,8}");
            output.Append(" total");
            Console.WriteLine(output.ToString());
        }
        return dir;
    }

    // === sort: Ordenar líneas ===
    private static string Sort(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: sort [-r] [-n] [-u] <archivo>");
            return dir;
        }

        bool reverse = args.Contains("-r");
        bool numeric = args.Contains("-n");
        bool unique = args.Contains("-u");
        var file = args.FirstOrDefault(a => !a.StartsWith("-"));

        if (file == null)
        {
            Console.WriteLine("Uso: sort [-r] [-n] [-u] <archivo>");
            return dir;
        }

        var path = GetFullPath(file, dir);
        if (!File.Exists(path))
        {
            Core.ThemeManager.WriteError($"sort: {file}: No existe");
            return dir;
        }

        var lines = File.ReadAllLines(path).ToList();

        if (numeric)
            lines = lines.OrderBy(l => double.TryParse(l.Split()[0], out var n) ? n : 0).ToList();
        else
            lines.Sort(StringComparer.OrdinalIgnoreCase);

        if (reverse) lines.Reverse();
        if (unique) lines = lines.Distinct().ToList();

        foreach (var line in lines)
            Console.WriteLine(line);

        return dir;
    }

    // === uniq: Eliminar líneas duplicadas consecutivas ===
    private static string Uniq(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: uniq [-c] [-d] [-u] <archivo>");
            return dir;
        }

        bool count = args.Contains("-c");
        bool onlyDups = args.Contains("-d");
        bool onlyUnique = args.Contains("-u");
        var file = args.FirstOrDefault(a => !a.StartsWith("-"));

        if (file == null) return dir;

        var path = GetFullPath(file, dir);
        if (!File.Exists(path))
        {
            Core.ThemeManager.WriteError($"uniq: {file}: No existe");
            return dir;
        }

        var lines = File.ReadAllLines(path);
        string? prev = null;
        int cnt = 0;

        foreach (var line in lines.Append(null))
        {
            if (line == prev)
            {
                cnt++;
            }
            else
            {
                if (prev != null)
                {
                    bool isDup = cnt > 1;
                    if ((onlyDups && isDup) || (onlyUnique && !isDup) || (!onlyDups && !onlyUnique))
                    {
                        if (count)
                            Console.WriteLine($"{cnt,7} {prev}");
                        else
                            Console.WriteLine(prev);
                    }
                }
                prev = line;
                cnt = 1;
            }
        }
        return dir;
    }

    // === diff: Comparar archivos ===
    private static string Diff(string[] args, string dir)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: diff <archivo1> <archivo2>");
            return dir;
        }

        var file1 = GetFullPath(args[0], dir);
        var file2 = GetFullPath(args[1], dir);

        if (!File.Exists(file1))
        {
            Core.ThemeManager.WriteError($"diff: {args[0]}: No existe");
            return dir;
        }
        if (!File.Exists(file2))
        {
            Core.ThemeManager.WriteError($"diff: {args[1]}: No existe");
            return dir;
        }

        var lines1 = File.ReadAllLines(file1);
        var lines2 = File.ReadAllLines(file2);

        int max = Math.Max(lines1.Length, lines2.Length);
        bool hasDiff = false;

        for (int i = 0; i < max; i++)
        {
            var l1 = i < lines1.Length ? lines1[i] : null;
            var l2 = i < lines2.Length ? lines2[i] : null;

            if (l1 != l2)
            {
                hasDiff = true;
                Console.WriteLine($"{i + 1}c{i + 1}");
                if (l1 != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"< {l1}");
                }
                Console.ResetColor();
                Console.WriteLine("---");
                if (l2 != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"> {l2}");
                }
                Console.ResetColor();
            }
        }

        if (!hasDiff)
            Core.ThemeManager.WriteSuccess("Los archivos son idénticos.");

        return dir;
    }

    // === tar: Comprimir/descomprimir archivos ===
    private static string Tar(string[] args, string dir)
    {
        Console.WriteLine("tar: Usa el comando nativo del sistema:");
        Console.WriteLine("  Crear: tar -cvf archivo.tar directorio/");
        Console.WriteLine("  Extraer: tar -xvf archivo.tar");
        Console.WriteLine("  Listar: tar -tvf archivo.tar");
        
        // Ejecutar tar nativo si existe
        if (args.Length > 0)
        {
            Core.Shell.ExecuteExternal("tar " + string.Join(" ", args), dir);
        }
        return dir;
    }

    // === curl: Descargar/enviar HTTP ===
    private static string Curl(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: curl [-o archivo] [-X método] [-H header] <url>");
            return "";
        }

        string? outputFile = null;
        string method = "GET";
        var headers = new Dictionary<string, string>();
        string? url = null;
        string? data = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "-X" when i + 1 < args.Length:
                    method = args[++i].ToUpper();
                    break;
                case "-H" when i + 1 < args.Length:
                    var header = args[++i].Split(':', 2);
                    if (header.Length == 2)
                        headers[header[0].Trim()] = header[1].Trim();
                    break;
                case "-d" when i + 1 < args.Length:
                    data = args[++i];
                    method = "POST";
                    break;
                default:
                    if (!args[i].StartsWith("-"))
                        url = args[i];
                    break;
            }
        }

        if (url == null)
        {
            Console.WriteLine("Uso: curl [-o archivo] <url>");
            return "";
        }

        try
        {
            using var client = new HttpClient();
            foreach (var h in headers)
                client.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value);

            HttpResponseMessage response;
            if (method == "POST" && data != null)
            {
                var content = new StringContent(data);
                response = client.PostAsync(url, content).Result;
            }
            else
            {
                response = client.GetAsync(url).Result;
            }

            var result = response.Content.ReadAsStringAsync().Result;

            if (outputFile != null)
            {
                File.WriteAllText(outputFile, result);
                Core.ThemeManager.WriteSuccess($"Guardado en: {outputFile}");
            }
            else
            {
                Console.WriteLine(result);
            }
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"curl: {ex.Message}");
        }
        return "";
    }

    // === wget: Descargar archivo ===
    private static string Wget(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: wget [-O archivo] <url>");
            return dir;
        }

        string? outputFile = null;
        string? url = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-O" && i + 1 < args.Length)
                outputFile = args[++i];
            else if (!args[i].StartsWith("-"))
                url = args[i];
        }

        if (url == null)
        {
            Console.WriteLine("Uso: wget [-O archivo] <url>");
            return dir;
        }

        try
        {
            using var client = new HttpClient();
            var bytes = client.GetByteArrayAsync(url).Result;
            
            outputFile ??= Path.GetFileName(new Uri(url).LocalPath);
            if (string.IsNullOrEmpty(outputFile))
                outputFile = "download";

            var path = GetFullPath(outputFile, dir);
            File.WriteAllBytes(path, bytes);
            Core.ThemeManager.WriteSuccess($"Descargado: {outputFile} ({bytes.Length} bytes)");
        }
        catch (Exception ex)
        {
            Core.ThemeManager.WriteError($"wget: {ex.Message}");
        }
        return dir;
    }

    // === awk: Procesamiento de texto básico ===
    private static string Awk(string[] args, string dir)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Uso: awk '{print $N}' <archivo>");
            Console.WriteLine("  $0 = línea completa, $1 = primer campo, $NF = último campo");
            return dir;
        }

        string pattern = "";
        string? file = null;
        string delimiter = @"\s+";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-F" && i + 1 < args.Length)
            {
                delimiter = Regex.Escape(args[++i]);
            }
            else if (args[i].StartsWith("'") || args[i].StartsWith("\""))
            {
                pattern = args[i].Trim(' ', '\'', '"');
            }
            else if (!args[i].StartsWith("-"))
            {
                file = args[i];
            }
        }

        if (file == null)
        {
            Console.WriteLine("Uso: awk [-F delim] '{print $N}' <archivo>");
            return dir;
        }

        var path = GetFullPath(file, dir);
        if (!File.Exists(path))
        {
            Core.ThemeManager.WriteError($"awk: {file}: No existe");
            return dir;
        }

        // Parsear pattern simple: {print $N}
        var printMatch = Regex.Match(pattern, @"\{print\s+(.+)\}");
        if (!printMatch.Success)
        {
            Console.WriteLine("awk: Solo soporta patrones simples como '{print $1, $2}'");
            return dir;
        }

        var fields = printMatch.Groups[1].Value.Split(',', ' ')
            .Where(f => f.StartsWith("$"))
            .Select(f => f.TrimStart('$'))
            .ToArray();

        foreach (var line in File.ReadLines(path))
        {
            var parts = Regex.Split(line.Trim(), delimiter);
            var output = new List<string>();

            foreach (var field in fields)
            {
                if (field == "0")
                    output.Add(line);
                else if (field == "NF")
                    output.Add(parts.Length > 0 ? parts[^1] : "");
                else if (int.TryParse(field, out int idx) && idx > 0 && idx <= parts.Length)
                    output.Add(parts[idx - 1]);
            }

            Console.WriteLine(string.Join(" ", output));
        }
        return dir;
    }

    // === sed: Stream editor básico ===
    private static string Sed(string[] args, string dir)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: sed 's/patron/reemplazo/[g]' <archivo>");
            Console.WriteLine("     sed -i 's/patron/reemplazo/g' <archivo>  (editar in-place)");
            return dir;
        }

        bool inPlace = args.Contains("-i");
        var nonFlags = args.Where(a => !a.StartsWith("-")).ToArray();

        if (nonFlags.Length < 2)
        {
            Console.WriteLine("Uso: sed 's/patron/reemplazo/[g]' <archivo>");
            return dir;
        }

        var expression = nonFlags[0].Trim(' ', '\'', '"');;
        var file = nonFlags[1];

        var path = GetFullPath(file, dir);
        if (!File.Exists(path))
        {
            Core.ThemeManager.WriteError($"sed: {file}: No existe");
            return dir;
        }

        // Parsear s/pattern/replacement/flags
        var match = Regex.Match(expression, @"s/(.+?)/(.*)/(g)?");
        if (!match.Success)
        {
            Console.WriteLine("sed: Solo soporta sustitución: s/patron/reemplazo/[g]");
            return dir;
        }

        var pattern = match.Groups[1].Value;
        var replacement = match.Groups[2].Value;
        bool global = match.Groups[3].Success;

        var lines = File.ReadAllLines(path);
        var result = new List<string>();

        foreach (var line in lines)
        {
            string newLine;
            if (global)
                newLine = Regex.Replace(line, pattern, replacement);
            else
                newLine = Regex.Replace(line, pattern, replacement, RegexOptions.None, TimeSpan.FromSeconds(1));
            result.Add(newLine);
        }

        if (inPlace)
        {
            File.WriteAllLines(path, result);
            Core.ThemeManager.WriteSuccess($"Archivo modificado: {file}");
        }
        else
        {
            foreach (var line in result)
                Console.WriteLine(line);
        }
        return dir;
    }

    // === xargs: Ejecutar comando con argumentos de stdin ===
    private static string Xargs(string[] args, string dir)
    {
        Console.WriteLine("xargs: Usa el comando nativo del sistema o pipes.");
        Console.WriteLine("Ejemplo: find . -name '*.txt' | xargs grep 'patron'");
        return dir;
    }

    // === tee: Escribir a archivo y stdout ===
    private static string Tee(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: echo 'texto' | tee archivo.txt");
            Console.WriteLine("     tee [-a] <archivo>");
            return dir;
        }

        bool append = args.Contains("-a");
        var file = args.FirstOrDefault(a => !a.StartsWith("-"));

        if (file == null) return dir;

        var path = GetFullPath(file, dir);
        Console.WriteLine("Escribe texto (Ctrl+Z para terminar):");

        var lines = new List<string>();
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            Console.WriteLine(line);
            lines.Add(line);
        }

        if (append)
            File.AppendAllLines(path, lines);
        else
            File.WriteAllLines(path, lines);

        return dir;
    }

    // === echo: Mostrar texto ===
    private static string Echo(string[] args)
    {
        bool noNewline = args.Contains("-n");
        var text = string.Join(" ", args.Where(a => a != "-n"));
        
        // Expandir variables de entorno
        text = Environment.ExpandEnvironmentVariables(text);
        
        // Procesar secuencias de escape
        text = text.Replace("", " ")
                   .Replace("", "	")
                   .Replace("", "");

        if (noNewline)
            Console.Write(text);
        else
            Console.WriteLine(text);

        return "";
    }

    // === basename: Nombre del archivo ===
    private static string Basename(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: basename <ruta> [sufijo]");
            return "";
        }

        var name = Path.GetFileName(args[0]);
        if (args.Length > 1 && name.EndsWith(args[1]))
            name = name[..^args[1].Length];

        Console.WriteLine(name);
        return "";
    }

    // === dirname: Directorio padre ===
    private static string Dirname(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: dirname <ruta>");
            return "";
        }

        Console.WriteLine(Path.GetDirectoryName(args[0]) ?? ".");
        return "";
    }

    // === realpath: Ruta absoluta ===
    private static string Realpath(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: realpath <ruta>");
            return dir;
        }

        Console.WriteLine(GetFullPath(args[0], dir));
        return dir;
    }

    // === ln: Crear enlaces (Windows) ===
    private static string Ln(string[] args, string dir)
    {
        Console.WriteLine("ln: En Windows usa:");
        Console.WriteLine("  mklink archivo enlace        (enlace simbólico archivo)");
        Console.WriteLine("  mklink /D directorio enlace  (enlace simbólico directorio)");
        Console.WriteLine("  mklink /H archivo enlace     (enlace duro)");
        return dir;
    }

    // === stat: Información del archivo ===
    private static string Stat(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: stat <archivo>");
            return dir;
        }

        var path = GetFullPath(args[0], dir);
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Core.ThemeManager.WriteError($"stat: {args[0]}: No existe");
            return dir;
        }

        var info = new FileInfo(path);
        Console.WriteLine($"  Archivo: {info.FullName}");
        Console.WriteLine($"  Tamaño: {info.Length} bytes");
        Console.WriteLine($"  Tipo: {(info.Attributes.HasFlag(FileAttributes.Directory) ? "directorio" : "archivo")}");
        Console.WriteLine($"  Acceso: {info.LastAccessTime}");
        Console.WriteLine($"  Modificación: {info.LastWriteTime}");
        Console.WriteLine($"  Creación: {info.CreationTime}");
        Console.WriteLine($"  Atributos: {info.Attributes}");
        return dir;
    }

    // === file: Tipo de archivo ===
    private static string FileCmd(string[] args, string dir)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: file <archivo>");
            return dir;
        }

        var path = GetFullPath(args[0], dir);
        if (Directory.Exists(path))
        {
            Console.WriteLine($"{args[0]}: directory");
            return dir;
        }
        if (!File.Exists(path))
        {
            Core.ThemeManager.WriteError($"file: {args[0]}: No existe");
            return dir;
        }

        var ext = Path.GetExtension(path).ToLower();
        var type = ext switch
        {
            ".txt" or ".md" or ".log" => "ASCII text",
            ".json" => "JSON text data",
            ".xml" => "XML document",
            ".html" or ".htm" => "HTML document",
            ".css" => "CSS stylesheet",
            ".js" => "JavaScript source",
            ".cs" => "C# source code",
            ".py" => "Python script",
            ".exe" => "PE32+ executable (console)",
            ".dll" => "PE32+ DLL",
            ".zip" => "Zip archive data",
            ".png" => "PNG image data",
            ".jpg" or ".jpeg" => "JPEG image data",
            ".gif" => "GIF image data",
            ".pdf" => "PDF document",
            _ => "data"
        };

        Console.WriteLine($"{args[0]}: {type}");
        return dir;
    }

    // === which: Buscar ejecutable en PATH ===
    private static string Which(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: which <comando>");
            return "";
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var paths = pathEnv.Split(Path.PathSeparator);
        var extensions = new[] { "", ".exe", ".cmd", ".bat", ".com" };

        foreach (var cmd in args)
        {
            bool found = false;
            foreach (var dir in paths)
            {
                foreach (var ext in extensions)
                {
                    var full = Path.Combine(dir, cmd + ext);
                    if (File.Exists(full))
                    {
                        Console.WriteLine(full);
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
            if (!found)
                Core.ThemeManager.WriteError($"which: {cmd}: not found");
        }
        return "";
    }

    // === env: Mostrar variables de entorno ===
    private static string Env(string[] args)
    {
        foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
        {
            Console.WriteLine($"{env.Key}={env.Value}");
        }
        return "";
    }

    // === export: Establecer variable de entorno ===
    private static string Export(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: export VARIABLE=valor");
            return "";
        }

        foreach (var arg in args)
        {
            var parts = arg.Split('=', 2);
            if (parts.Length == 2)
            {
                Core.Configuration.SetEnvironmentVar(parts[0], parts[1]);
                Core.ThemeManager.WriteSuccess($"export {parts[0]}={parts[1]}");
            }
        }
        return "";
    }

    // === unset: Eliminar variable de entorno ===
    private static string Unset(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: unset VARIABLE");
            return "";
        }

        foreach (var name in args)
        {
            Core.Configuration.RemoveEnvironmentVar(name);
            Core.ThemeManager.WriteSuccess($"unset {name}");
        }
        return "";
    }

    // === printenv: Mostrar variable de entorno ===
    private static string PrintEnv(string[] args)
    {
        if (args.Length == 0)
        {
            Env(args);
            return "";
        }

        foreach (var name in args)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (value != null)
                Console.WriteLine(value);
        }
        return "";
    }

    // === Helper ===
    private static string GetFullPath(string path, string dir)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);
        return Path.GetFullPath(Path.Combine(dir, path));
    }
}
