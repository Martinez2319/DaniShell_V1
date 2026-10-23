using DaniShell.Commands;

public sealed class HelloPlugin : CommandBase, IStreamingCommand
{
    public override string Name => "hello";
    public override string Description => "Plugin de ejemplo compatible con la API 1.";
    public override string Execute(string[] args, string currentDir)
    { ExecuteStream(args, currentDir, Console.In, Console.Out, Console.Error); return currentDir; }
    public int ExecuteStream(string[] args, string directory, TextReader input, TextWriter output, TextWriter error)
    { output.WriteLine("Hola, " + (args.Length > 0 ? string.Join(" ", args) : "DaniShell") + "!"); return 0; }
}
