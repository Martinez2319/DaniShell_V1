using System.Diagnostics;
using System.Text;
using DaniShell.Core;
using DaniShell.Plugins;

public static class AdvancedTests
{
    public static void Run(string directory, Action<string, Action> test, Func<string, string?, string> run)
    {
        void Equal(string expected, string actual) { if (expected != actual) throw new Exception($"Expected [{expected}], got [{actual}]"); }
        string Execute(string command, string? input = null) => run(command, input);
        test("redirection truncate, append and stdin with quoted paths", () =>
        {
            Execute("echo uno > 'redireccion con espacios.txt'"); Execute("echo dos >> 'redireccion con espacios.txt'");
            Equal("uno\ndos\n", Execute("cat < 'redireccion con espacios.txt'"));
            Execute("echo nuevo > 'redireccion con espacios.txt'"); Equal("nuevo\n", Execute("cat < 'redireccion con espacios.txt'"));
        });
        test("conditional chaining uses the exit code of the last command", () =>
        {
            Equal("uno\ndos\n", Execute("echo uno && echo dos || echo error"));
            Equal("recuperado\n", Execute("cat __missing__ && echo incorrecto || echo recuperado"));
            Equal("0", Shell.LastExitCode.ToString());
        });
        test("stderr can be redirected and merged without entering stdout", () =>
        {
            Equal("", Execute("cat __missing__ 2> error.txt"));
            if (!File.ReadAllText(Path.Combine(directory, "error.txt")).Contains("No existe")) throw new Exception("Missing stderr");
            if (!Execute("cat __missing__ 2>&1").Contains("No existe")) throw new Exception("Missing merged error");
        });
        test("literal operators inside quotes are preserved", () => Equal("a > b && c || d\n", Execute("echo 'a > b && c || d'")));
        test("stderr descriptor order is preserved", () =>
        {
            if (!Execute("cat __missing__ 2>&1 > only-output.txt").Contains("No existe")) throw new Exception("stderr was redirected after the descriptor copy");
            Equal("", File.ReadAllText(Path.Combine(directory, "only-output.txt")));
            Equal("", Execute("cat __missing__ > both.txt 2>&1"));
            if (!File.ReadAllText(Path.Combine(directory, "both.txt")).Contains("No existe")) throw new Exception("Missing redirected stderr");
        });
        test("missing input redirection participates in conditional execution", () =>
            Equal("recovered\n", Execute("cat < __missing_input__ 2> ignored.txt || echo recovered")));
        test("native sessions keep state across successive input lines", () =>
        {
            foreach (var engine in new[] { "cmd", "powershell" })
            {
                var before = Console.Out; using var output = new StringWriter();
                Process? active = null;
                using var timer = new Timer(_ => { try { active?.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } }, null, 15000, Timeout.Infinite);
                try
                {
                    Console.SetOut(output);
                    string input = engine == "cmd" ? "set DS_STATE=42\r\necho DS_SESSION_VALUE=%DS_STATE%\r\nexit 0\r\n" : "$DS_STATE=42\r\nWrite-Output ('DS_SESSION_VALUE=' + $DS_STATE)\r\nexit 0\r\n";
                    Equal("0", ProcessRunner.Run(NativeShell.Session(engine, directory), input, p => active = p).ToString());
                }
                finally { Console.SetOut(before); }
                if (!output.ToString().Contains("DS_SESSION_VALUE=42")) throw new Exception(engine + " lost session state");
            }
        });
        test("native CMD executes environment, FOR and native syntax", () =>
        {
            var before = Console.Out; using var output = new StringWriter();
            try
            {
                Console.SetOut(output);
                var code = ProcessRunner.Run(NativeShell.Command("cmd", "set DS_TEST=hola&call echo %DS_TEST%&for %i in (1 2) do @echo %i", directory));
                Equal("0", code.ToString());
            }
            finally { Console.SetOut(before); }
            Equal("hola\n1\n2\n", output.ToString().Replace("\r\n", "\n"));
        });
        test("native PowerShell runs object pipelines and keeps variables within a script", () =>
            Equal("42\n", Execute("ps '$n = 41; [pscustomobject]@{Value=$n+1} | Select-Object -ExpandProperty Value'")));
        test("native exit codes propagate", () => { Execute("cmd /c exit 7"); Equal("7", Shell.LastExitCode.ToString()); });
        test("native PowerShell forwards errors with a failure exit code", () =>
        {
            var before = Console.Error; using var error = new StringWriter();
            try { Console.SetError(error); Execute("ps 'throw \"expected-test-error\"'"); }
            finally { Console.SetError(before); }
            if (Shell.LastExitCode == 0 || !error.ToString().Contains("expected-test-error")) throw new Exception("PowerShell error lost");
        });
        test("CLI uses the selected native engine and exit code", () =>
        {
            var info = PipelineRunner.StageProcess("ignored", directory);
            // StageProcess supplies a correctly resolved host; replace only the program arguments.
            if (info.ArgumentList.Contains("--stage"))
            {
                int pos = info.ArgumentList.IndexOf("--stage"); info.ArgumentList.RemoveAt(pos + 1); info.ArgumentList.RemoveAt(pos);
                info.ArgumentList.Add("--shell"); info.ArgumentList.Add("cmd"); info.ArgumentList.Add("-c"); info.ArgumentList.Add("exit 9");
            }
            Equal("9", ProcessRunner.Run(info).ToString());
        });
        test("streaming pipeline exits when an early consumer finishes", () =>
        {
            string command = "\"" + Environment.ProcessPath + "\" --slow";
            var before = Console.Out; using var output = new StringWriter(); var clock = Stopwatch.StartNew();
            try { Console.SetOut(output); Equal("0", PipelineRunner.Run(new[] { new CommandStage(command), new CommandStage("head -n 1") }, directory).ToString()); }
            finally { Console.SetOut(before); }
            Equal("first\n", output.ToString().Replace("\r\n", "\n"));
            if (clock.Elapsed > TimeSpan.FromSeconds(7)) throw new Exception("Pipeline waited for the entire producer");
        });
        test("cancellation terminates the pipeline and returns 130", () =>
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(900));
            var before = Console.Out; using var output = new StringWriter(); var clock = Stopwatch.StartNew();
            try { Console.SetOut(output); Equal("130", PipelineRunner.Run(new[] { new CommandStage("\"" + Environment.ProcessPath + "\" --slow") }, directory, cancellationToken: cancellation.Token).ToString()); }
            finally { Console.SetOut(before); }
            if (clock.Elapsed > TimeSpan.FromSeconds(7)) throw new Exception("Cancellation did not stop producer");
        });
        test("large file pipeline counts incrementally", () =>
        {
            var path = Path.Combine(directory, "large.txt");
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(false))) for (int i = 0; i < 100_000; i++) writer.WriteLine("dato " + i);
            Equal("100000", Execute("cat large.txt | wc -l").Trim());
        });
        test("external merge sort handles more than one chunk", () =>
        {
            var path = Path.Combine(directory, "sort-large.txt");
            using (var writer = new StreamWriter(path)) for (int i = 20_000; i > 0; i--) writer.WriteLine(i);
            Equal("1\n2\n3\n", Execute("sort -n sort-large.txt | head -n 3"));
        });
        test("history removes earlier duplicates and supports reverse search", () =>
        {
            InputHandler.ClearHistory(); InputHandler.AddToHistory("echo uno"); InputHandler.AddToHistory("git status"); InputHandler.AddToHistory("echo uno");
            Equal("git status|echo uno", string.Join("|", InputHandler.GetHistory()));
            Equal("echo uno", InputHandler.SearchHistory("UNO").Single());
            InputHandler.AddToHistory(" hidden-command"); Equal("2", InputHandler.GetHistory().Count.ToString());
        });
        test("completion handles options and quoted paths with spaces", () =>
        {
            Directory.CreateDirectory(Path.Combine(directory, "carpeta con espacios"));
            if (!Completion.Candidates("grep -", directory).Contains("grep -i")) throw new Exception("Missing option completion");
            if (!Completion.Candidates("cat \"carp", directory).Contains("cat \"carpeta con espacios\"")) throw new Exception("Missing quoted path completion");
            var cycle = new CompletionCycle(); string first = cycle.Next("wc -", directory), second = cycle.Next(first, directory);
            if (first == second) throw new Exception("Completion did not cycle");
        });
        test("plugin template loads, runs in a pipeline and rejects incompatible API", () =>
        {
            string destination = Configuration.PluginsDir;
            foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "fixture-plugin"))) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            PluginLoader.LoadPlugins(quiet: true);
            Equal("Hola, DaniShell!\n", Execute("hello DaniShell | cat"));
            File.WriteAllText(Path.Combine(destination, "HelloPlugin.plugin.json"), "{\"apiVersion\":99}");
            PluginLoader.LoadPlugins(quiet: true);
            if (PluginLoader.GetPlugin("hello") != null) throw new Exception("Incompatible plugin accepted");
        });
    }
}
