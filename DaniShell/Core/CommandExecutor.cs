using System.Text;
namespace DaniShell.Core;

public static class CommandExecutor
{
    public static string Execute(string input, string directory, string? standardInput = null)
    {
        if (string.IsNullOrWhiteSpace(input)) return directory;
        if (NativeShell.TryShortcut(input.Trim(), directory, out int code)) { Shell.SetExitCode(code); return directory; }
        foreach (var group in CommandSyntax.Parse(input))
        {
            if (group.Condition == "success" && Shell.LastExitCode != 0 || group.Condition == "failure" && Shell.LastExitCode == 0) continue;
            if (group.Stages.Count > 1) Shell.SetExitCode(PipelineRunner.Run(group.Stages, directory, standardInput));
            else directory = Single(group.Stages[0], directory, standardInput);
        }
        return directory;
    }
    private static string Single(CommandStage stage, string directory, string? input)
    {
        var previousIn = Console.In; var previousOut = Console.Out; var previousError = Console.Error;
        var resources = new List<IDisposable>();
        try
        {
            if (stage.Output != null) { var writer = new StreamWriter(Path.GetFullPath(stage.Output, directory), stage.Append, new UTF8Encoding(false)); resources.Add(writer); Console.SetOut(writer); }
            if (stage.MergeError) Console.SetError(stage.MergeOriginalOutput ? previousOut : Console.Out);
            else if (stage.Error != null) { var writer = new StreamWriter(Path.GetFullPath(stage.Error, directory), stage.AppendError, new UTF8Encoding(false)); resources.Add(writer); Console.SetError(writer); }
            if (stage.Input != null) { var reader = File.OpenText(Path.GetFullPath(stage.Input, directory)); resources.Add(reader); Console.SetIn(reader); }
            else if (input != null) Console.SetIn(new StringReader(input));
            return Shell.DispatchStage(stage.Command, directory, input != null || stage.Input != null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Shell.SetExitCode(1); Console.Error.WriteLine(ex.Message); return directory; }
        finally { Console.SetIn(previousIn); Console.SetOut(previousOut); Console.SetError(previousError); foreach (var resource in resources) resource.Dispose(); }
    }
}
