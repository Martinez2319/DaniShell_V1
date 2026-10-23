namespace DaniShell.Core;

public static class Completion
{
    private static readonly Dictionary<string, string[]> Options = new(StringComparer.OrdinalIgnoreCase)
    {
        ["grep"] = new[] { "-i", "-n", "-r", "-v" }, ["head"] = new[] { "-n" }, ["tail"] = new[] { "-n" },
        ["sort"] = new[] { "-n", "-r", "-u" }, ["wc"] = new[] { "-l", "-w", "-c" }, ["tee"] = new[] { "-a" },
        ["theme"] = new[] { "dark", "light", "hacker", "cyberpunk", "ocean", "fire", "neon" },
        ["shell"] = new[] { "cmd", "powershell" }, ["plugins"] = new[] { "list", "reload" }
    };
    private static readonly string[] Commands = ("help exit quit clear cls cd dir ls lsd whoami theme runas sysinfo about sound history alias unalias config plugins reload banner prompt version status shell cmd ps powershell cat touch rm cp mv grep pwd head tail mkdir rmdir chmod find wc sort uniq diff tar curl wget awk sed tee basename dirname realpath ln stat file which env export unset printenv ssh nmap").Split(' ');

    public static IReadOnlyList<string> Candidates(string input, string directory)
    {
        char? quote = null; int start = 0; int stageStart = 0;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c is '\'' or '"') { if (quote == c) quote = null; else if (quote == null) quote = c; }
            else if (quote == null && char.IsWhiteSpace(c)) start = i + 1;
            else if (quote == null && c is '|' or '&') { start = i + 1; stageStart = i + 1; }
        }
        string prefix = input[start..].TrimStart('\'', '"');
        string command = input[stageStart..start].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (command.Length == 0)
        {
            foreach (var name in Commands.Concat(Configuration.Aliases.Keys).Concat(Plugins.PluginLoader.Plugins.Select(p => p.Name))) values.Add(name);
            foreach (var path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                try { foreach (var file in Directory.EnumerateFiles(path.Trim('"'))) if (Path.GetExtension(file).ToLowerInvariant() is ".exe" or ".cmd" or ".bat") values.Add(Path.GetFileName(file)); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        else if (Options.TryGetValue(command, out var options)) foreach (var option in options) values.Add(option);
        if (!prefix.StartsWith("-"))
        {
            try
            {
                string parent = Path.GetDirectoryName(prefix) ?? "";
                string baseDirectory = Path.GetFullPath(parent.Length == 0 ? "." : parent, directory);
                foreach (var path in Directory.EnumerateFileSystemEntries(baseDirectory))
                    values.Add(Path.Combine(parent, Path.GetFileName(path)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
        }
        return values.Where(v => v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Order(StringComparer.OrdinalIgnoreCase).Take(100)
            .Select(value => input[..start] + (value.Any(char.IsWhiteSpace) || input[start..].StartsWith('"') ? "\"" + value.Replace("\"", "\\\"") + "\"" : value)).ToArray();
    }
}

public sealed class CompletionCycle
{
    private IReadOnlyList<string> _candidates = Array.Empty<string>(); private string? _last; private int _index = -1;
    public string Next(string input, string directory, bool backwards = false)
    {
        if (_last != input) { _candidates = Completion.Candidates(input, directory); _index = backwards ? 0 : -1; }
        if (_candidates.Count == 0) return input;
        _index = (_index + (backwards ? -1 : 1) + _candidates.Count) % _candidates.Count;
        return _last = _candidates[_index];
    }
}
