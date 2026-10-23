namespace DaniShell.Commands;
public interface IStreamingCommand : ICommand
{
    int ExecuteStream(string[] args, string directory, TextReader input, TextWriter output, TextWriter error);
}
