using System.Text;

namespace DaniShell.Core;

public static class CommandLine
{
    public static string[] SplitPipeline(string input)
    {
        var stages = new List<string>();
        char? quote = null;
        int start = 0;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\\' && i + 1 < input.Length && quote == '"' && input[i + 1] == '"') { i++; continue; }
            if (quote != null) { if (c == quote) quote = null; }
            else if (c is '\'' or '"') quote = c;
            else if (c == '|')
            {
                stages.Add(input[start..i].Trim());
                start = i + 1;
            }
        }
        if (quote != null) throw new ArgumentException("Comillas sin cerrar.");
        stages.Add(input[start..].Trim());
        if (stages.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("La tubería contiene un comando vacío.");
        return stages.ToArray();
    }

    public static string[] ParseArguments(string input)
    {
        var args = new List<string>();
        var current = new StringBuilder();
        char? quote = null;
        bool started = false;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\\' && quote == '"' && i + 1 < input.Length && input[i + 1] == '"')
            { current.Append(input[++i]); started = true; continue; }
            if (quote != null) { if (c == quote) quote = null; else current.Append(c); }
            else if (c is '\'' or '"') { quote = c; started = true; }
            else if (char.IsWhiteSpace(c))
            {
                if (started) { args.Add(current.ToString()); current.Clear(); started = false; }
            }
            else { current.Append(c); started = true; }
        }
        if (quote != null) throw new ArgumentException("Comillas sin cerrar.");
        if (started) args.Add(current.ToString());
        return args.ToArray();
    }
}
