using System.Diagnostics;

namespace DaniShell.Core;

public static class ProcessRunner
{
    public static ProcessStartInfo Create(string executable, IEnumerable<string> arguments, string directory)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = directory, UseShellExecute = false };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        return info;
    }

    public static int Run(ProcessStartInfo info, string? input = null, Action<Process?>? active = null)
    {
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        info.RedirectStandardInput = input != null;
        info.CreateNoWindow = true;
        using var process = new Process { StartInfo = info };
        try
        {
            process.Start();
            active?.Invoke(process);
            // Drain both pipes concurrently, before writing stdin or waiting for exit.
            var stdout = Pump(process.StandardOutput, Console.Out);
            var stderr = Pump(process.StandardError, Console.Error);
            var stdin = input == null ? Task.CompletedTask : WriteInput(process, input);
            Task.WhenAll(stdout, stderr, stdin, process.WaitForExitAsync()).GetAwaiter().GetResult();
            return process.ExitCode;
        }
        finally { active?.Invoke(null); }
    }

    public static int RunInteractive(ProcessStartInfo info, Action<Process?>? active = null)
    {
        info.RedirectStandardOutput = false;
        info.RedirectStandardError = false;
        info.RedirectStandardInput = false;
        info.CreateNoWindow = false;
        using var process = Process.Start(info) ?? throw new InvalidOperationException("No se pudo iniciar el proceso.");
        active?.Invoke(process);
        try { process.WaitForExit(); return process.ExitCode; }
        finally { active?.Invoke(null); }
    }

    private static async Task Pump(StreamReader reader, TextWriter writer)
    {
        char[] buffer = new char[4096];
        int count;
        while ((count = await reader.ReadAsync(buffer)) != 0) await writer.WriteAsync(buffer.AsMemory(0, count));
    }

    private static async Task WriteInput(Process process, string input)
    {
        try { await process.StandardInput.WriteAsync(input); }
        catch (IOException) { /* The child may stop consuming input early. */ }
        finally { process.StandardInput.Close(); }
    }
}
