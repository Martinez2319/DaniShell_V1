using System.Diagnostics;

namespace DaniShell.Core;

public static class NativeShell
{
    public static string PowerShellExecutable()
    {
        foreach (string name in new[] { "pwsh.exe", "powershell.exe" })
            foreach (string path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                string candidate = Path.Combine(path.Trim('"'), name);
                if (File.Exists(candidate)) return candidate;
            }
        throw new FileNotFoundException("No se encontró PowerShell en PATH.");
    }

    public static ProcessStartInfo Command(string engine, string script, string directory)
    {
        if (engine.Equals("cmd", StringComparison.OrdinalIgnoreCase))
        {
            if (script.StartsWith("/c ", StringComparison.OrdinalIgnoreCase)) script = script[3..];
            var info = ProcessRunner.Create(Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", Array.Empty<string>(), directory);
            info.Arguments = "/d /s /c \"" + script + "\"";
            return info;
        }
        // EncodedCommand avoids the extra quote interpretation of an outer CMD process.
        var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
        return ProcessRunner.Create(PowerShellExecutable(), new[] { "-NoLogo", "-NoProfile", "-OutputFormat", "Text", "-EncodedCommand", encoded }, directory);
    }

    public static ProcessStartInfo Session(string engine, string directory)
    {
        if (engine.Equals("cmd", StringComparison.OrdinalIgnoreCase))
            return ProcessRunner.Create(Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
                new[] { "/d", "/k", "title DaniShell - CMD & prompt [DaniShell:CMD] $P$G" }, directory);
        return ProcessRunner.Create(PowerShellExecutable(), new[] { "-NoLogo", "-NoExit", "-Command",
            "$Host.UI.RawUI.WindowTitle = 'DaniShell - PowerShell'; function global:prompt { '[DaniShell:PS] ' + (Get-Location).Path + '> ' }" }, directory);
    }

    public static int Open(string engine, string directory) => ProcessRunner.RunInteractive(Session(engine, directory), Shell.TrackProcess);

    public static bool TryShortcut(string input, string directory, out int code)
    {
        code = 0;
        int split = input.IndexOfAny(new[] { ' ', '\t' });
        string word = (split < 0 ? input : input[..split]).ToLowerInvariant();
        string rest = split < 0 ? "" : input[(split + 1)..].Trim();
        if (word == "shell")
        {
            if (rest is not ("cmd" or "powershell" or "ps")) throw new ArgumentException("Uso: shell cmd | shell powershell");
            code = Open(rest == "cmd" ? "cmd" : "powershell", directory); return true;
        }
        if (word is not ("cmd" or "ps" or "powershell" or "pwsh")) return false;
        string engine = word == "cmd" ? "cmd" : "powershell";
        if (rest.Length == 0) code = Open(engine, directory);
        else
        {
            ProcessStartInfo info;
            if (engine == "cmd" && rest.StartsWith("/k", StringComparison.OrdinalIgnoreCase))
            {
                info = ProcessRunner.Create(Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", Array.Empty<string>(), directory);
                info.Arguments = "/d " + rest;
                code = ProcessRunner.RunInteractive(info, Shell.TrackProcess); return true;
            }
            if (engine == "powershell" && rest.StartsWith("-"))
            { info = ProcessRunner.Create(PowerShellExecutable(), Array.Empty<string>(), directory); info.Arguments = rest; }
            else
            {
                var parsed = CommandLine.ParseArguments(rest);
                if (parsed.Length == 1 && rest[0] is '\'' or '"') rest = parsed[0];
                info = Command(engine, rest, directory);
            }
            code = engine == "powershell" && rest.Contains("-NoExit", StringComparison.OrdinalIgnoreCase)
                ? ProcessRunner.RunInteractive(info, Shell.TrackProcess) : ProcessRunner.Run(info, active: Shell.TrackProcess);
        }
        return true;
    }
}
