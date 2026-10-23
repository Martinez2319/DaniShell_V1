using System.Text;
using DaniShell.Core;

// Child modes exercise real OS pipes without SSH, nmap or network dependencies.
if (args.FirstOrDefault() == "--flood")
{
    Console.Error.Write(new string('e', 300_000));
    Console.Out.Write("done");
    return 0;
}
if (args.FirstOrDefault() == "--copy") { Console.Write(Console.In.ReadToEnd()); return 0; }
if (args.FirstOrDefault() == "--arguments") { Console.Write(string.Join("|", args.Skip(1))); return 0; }
if (args.FirstOrDefault() == "--exit") return 7;

var scratch = Path.Combine(Path.GetTempPath(), "DaniShell-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(scratch);
var initialDirectory = Directory.GetCurrentDirectory();
int passed = 0;
Configuration.Config.SoundEnabled = false;

void Equal(string expected, string actual)
{
    if (expected != actual) throw new Exception($"Expected [{expected}], got [{actual}]");
}
void Test(string name, Action test)
{
    test();
    passed++;
    Console.WriteLine("PASS " + name);
}
string Run(string command, string? input = null)
{
    var previous = Console.Out;
    using var output = new StringWriter();
    try { Console.SetOut(output); Shell.ExecuteLine(command, scratch, input); }
    finally { Console.SetOut(previous); }
    return output.ToString().Replace("\r\n", "\n");
}
string Count(string flags, string text) => Run("wc " + flags, text).Trim();

try
{
    File.WriteAllText(Path.Combine(scratch, "datos con espacios.txt"), "zeta\nalfa\nalfa\nbeta\n", new UTF8Encoding(false));
    Test("quoted arguments, empty arguments and Windows paths", () =>
        Equal("echo|a b||C:\\Users\\demo", string.Join("|", CommandLine.ParseArguments("echo \"a b\" '' C:\\Users\\demo"))));
    Test("escaped quotation mark", () => Equal("a\"b", CommandLine.ParseArguments("\"a\\\"b\"")[0]));
    Test("quoted pipe is literal", () => Equal("a|b\n", Run("echo 'a|b'")));
    Test("unterminated quote is rejected", () =>
    {
        try { CommandLine.SplitPipeline("echo 'open"); }
        catch (ArgumentException) { return; }
        throw new Exception("Invalid input accepted");
    });
    Test("empty pipeline stage is rejected", () =>
    {
        try { CommandLine.SplitPipeline("echo a || sort"); }
        catch (ArgumentException) { return; }
        throw new Exception("Empty stage accepted");
    });
    Test("echo escapes and no trailing newline", () => Equal("a\nb", Run(@"echo -n -e 'a\nb'")));
    Test("interactive SSH is rejected inside a buffered pipeline", () =>
    {
        try { Shell.ExecuteLine("echo data | ssh example.invalid", scratch); }
        catch (ArgumentException) { return; }
        throw new Exception("Interactive SSH accepted in a pipeline");
    });
    Test("wc newline count with unterminated last line", () => Equal("2", Count("-l", "uno dos\ntré\nfinal")));
    Test("wc counts whitespace-separated words", () => Equal("4", Count("-w", "uno dos\ntré\nfinal")));
    Test("wc counts UTF-8 bytes", () => Equal("3", Count("-c", "é\n")));
    Test("wc defaults on stdin", () => Equal("1,2,4", string.Join(",", Run("wc", "a b\n").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))));
    Test("native cat grep sort uniq pipeline", () => Equal("alfa\nbeta\n", Run("cat 'datos con espacios.txt' | grep a | sort | uniq | head -n 2")));
    Test("empty pipeline input does not scan the directory", () => Equal("0", Run("echo z | grep no-match | wc -l").Trim()));
    Test("tail consumes standard input", () => Equal("beta\n", Run("cat 'datos con espacios.txt' | tail -n 1")));
    Test("awk consumes standard input after quoted expression", () => Equal("dos\n", Run("echo 'uno dos' | awk '{print $2}'")));
    Test("sed non-global replacement", () => Equal("Xoo\n", Run("echo ooo | sed 's/o/X/'")));
    Test("tee forwards output and writes file", () =>
    {
        Equal("hola\n", Run("echo hola | tee copia.txt | cat"));
        Equal("hola\n", File.ReadAllText(Path.Combine(scratch, "copia.txt")).Replace("\r\n", "\n"));
    });
    Test("external filter receives native stdout", () => Equal("hola\n", Run("echo hola | findstr hola")));
    Test("published auxiliary resources are copied during build", () =>
    {
        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "commands_cmd.json")) ||
            !Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "banners")).Any()) throw new Exception("Missing resources");
    });
    Test("process drains large stderr concurrently", () =>
    {
        var outBefore = Console.Out; var errBefore = Console.Error;
        using var output = new StringWriter(); using var error = new StringWriter();
        try
        {
            Console.SetOut(output); Console.SetError(error);
            var info = ProcessRunner.Create(Environment.ProcessPath!, new[] { "--flood" }, scratch);
            if (ProcessRunner.Run(info) != 0) throw new Exception("Child failed");
        }
        finally { Console.SetOut(outBefore); Console.SetError(errBefore); }
        Equal("done", output.ToString());
        Equal("300000", error.ToString().Length.ToString());
    });
    Test("process preserves stdin and arguments", () =>
    {
        var before = Console.Out; using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            ProcessRunner.Run(ProcessRunner.Create(Environment.ProcessPath!, new[] { "--arguments", "a b", "c\"d" }, scratch));
            Equal("a b|c\"d", output.ToString());
            output.GetStringBuilder().Clear();
            ProcessRunner.Run(ProcessRunner.Create(Environment.ProcessPath!, new[] { "--copy" }, scratch), new string('x', 200_000));
            Equal("200000", output.ToString().Length.ToString());
        }
        finally { Console.SetOut(before); }
    });
    Test("interactive runner inherits console and exit code", () =>
    {
        var info = ProcessRunner.Create(Environment.ProcessPath!, new[] { "--exit" }, scratch);
        Equal("7", ProcessRunner.RunInteractive(info).ToString());
    });
    Console.WriteLine($"{passed} regression tests passed.");
    return 0;
}
catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
finally
{
    Directory.SetCurrentDirectory(initialDirectory);
    Directory.Delete(scratch, recursive: true);
}
