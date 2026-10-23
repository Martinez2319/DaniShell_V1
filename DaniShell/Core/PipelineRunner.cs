using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace DaniShell.Core;

public static class PipelineRunner
{
    private static CancellationTokenSource? _current;
    public static bool Cancel() { var current = _current; if (current == null) return false; current.Cancel(); return true; }

    public static ProcessStartInfo StageProcess(string command, string directory)
    {
        string assembly = typeof(Shell).Assembly.Location;
        ProcessStartInfo info;
        if (string.IsNullOrEmpty(assembly)) info = ProcessRunner.Create(Environment.ProcessPath!, new[] { "--stage", command }, directory);
        else
        {
            string host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ??
                Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "dotnet.exe"));
            info = ProcessRunner.Create(host, new[] { assembly, "--stage", command }, directory);
        }
        info.Environment["DANISHELL_STAGE_ALIASES"] = JsonSerializer.Serialize(Configuration.Aliases);
        info.Environment["DANISHELL_STAGE_CONFIG"] = JsonSerializer.Serialize(Configuration.Config);
        return info;
    }

    public static int Run(IReadOnlyList<CommandStage> stages, string directory, string? input = null, CancellationToken cancellationToken = default)
    {
        if (stages.Count > 32) throw new ArgumentException("La tubería admite hasta 32 etapas.");
        var processes = new List<Process>();
        var disposables = new List<IDisposable>();
        string temporary = Path.Combine(Path.GetTempPath(), "DaniShell-pipeline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _current = cancel;
        void KillAll() { lock (processes) foreach (var p in processes) TryKill(p); }
        using var registration = cancel.Token.Register(KillAll);
        try
        {
            foreach (var stage in stages)
            {
                cancel.Token.ThrowIfCancellationRequested();
                string cmd = CommandLine.ParseArguments(stage.Command).FirstOrDefault() ?? "";
                if (cmd.Equals("ssh", StringComparison.OrdinalIgnoreCase) || cmd.Equals("shell", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Las sesiones interactivas deben ejecutarse fuera de una tubería.");
                var info = StageProcess(stage.Command, directory);
                info.Environment["DANISHELL_PIPELINE_TEMP"] = temporary;
                info.RedirectStandardOutput = info.RedirectStandardError = true;
                info.RedirectStandardInput = processes.Count > 0 || input != null || stage.Input != null;
                info.StandardOutputEncoding = info.StandardErrorEncoding = new UTF8Encoding(false);
                if (info.RedirectStandardInput) info.StandardInputEncoding = new UTF8Encoding(false);
                info.CreateNoWindow = true;
                var process = new Process { StartInfo = info };
                process.Start(); lock (processes) processes.Add(process);
            }
            var tasks = new List<Task>();
            for (int i = 0; i < stages.Count; i++)
            {
                var spec = stages[i]; var process = processes[i];
                if (spec.Input != null)
                {
                    var reader = File.OpenText(Path.GetFullPath(spec.Input, directory)); disposables.Add(reader);
                    tasks.Add(Feed(reader, process));
                }
                else if (i == 0 && input != null) tasks.Add(Feed(new StringReader(input), process));

                bool defaultIsPipe = i + 1 < stages.Count && stages[i + 1].Input == null;
                TextWriter defaultDestination = defaultIsPipe ? processes[i + 1].StandardInput : i + 1 == stages.Count ? Console.Out : TextWriter.Null;
                TextWriter destination;
                if (spec.Output != null)
                {
                    var writer = new StreamWriter(Path.GetFullPath(spec.Output, directory), spec.Append, new UTF8Encoding(false));
                    disposables.Add(writer); destination = writer;
                }
                else destination = defaultDestination;
                var synchronized = TextWriter.Synchronized(destination);
                TextWriter errors = spec.MergeError
                    ? spec.MergeOriginalOutput && !ReferenceEquals(defaultDestination, destination) ? TextWriter.Synchronized(defaultDestination) : synchronized
                    : Console.Error;
                bool closesPipe = defaultIsPipe && (spec.Output == null || spec.MergeError && spec.MergeOriginalOutput);
                if (spec.Error != null)
                {
                    var writer = new StreamWriter(Path.GetFullPath(spec.Error, directory), spec.AppendError, new UTF8Encoding(false));
                    disposables.Add(writer); errors = writer;
                }
                if (defaultIsPipe && !closesPipe)
                    processes[i + 1].StandardInput.Close();
                async Task CopyStage()
                {
                    async Task SafePump(TextReader reader, TextWriter writer)
                    { try { await Pump(reader, writer); } catch (IOException) { TryKill(process); } }
                    try { await Task.WhenAll(SafePump(process.StandardOutput, synchronized), SafePump(process.StandardError, errors)); }
                    finally
                    {
                        try { await destination.FlushAsync(); if (closesPipe) defaultDestination.Close(); }
                        catch (IOException) when (closesPipe) { }
                    }
                }
                tasks.Add(CopyStage());
                tasks.Add(process.WaitForExitAsync());
                if (closesPipe)
                {
                    var producer = process; var consumer = processes[i + 1];
                    tasks.Add(consumer.WaitForExitAsync().ContinueWith(_ => TryKill(producer), TaskScheduler.Default));
                }
            }
            Task.WhenAll(tasks).GetAwaiter().GetResult();
            return cancel.IsCancellationRequested ? 130 : processes[^1].ExitCode;
        }
        catch (OperationCanceledException) { return 130; }
        finally
        {
            KillAll();
            foreach (var resource in disposables) resource.Dispose();
            foreach (var process in processes) { try { process.WaitForExit(); } catch (InvalidOperationException) { } process.Dispose(); }
            Directory.Delete(temporary, recursive: true);
            _current = null;
        }
    }

    private static async Task Pump(TextReader reader, TextWriter writer)
    {
        var buffer = new char[4096]; int count;
        while ((count = await reader.ReadAsync(buffer)) > 0) await writer.WriteAsync(buffer.AsMemory(0, count));
        await writer.FlushAsync();
    }
    private static async Task Feed(TextReader reader, Process process)
    {
        try { await Pump(reader, process.StandardInput); }
        catch (IOException) { }
        finally { try { process.StandardInput.Close(); } catch (IOException) { } }
    }
    private static void TryKill(Process process)
    { try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } }
}
