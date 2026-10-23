namespace DaniShell.Core;

public sealed record CommandStage(string Command, string? Input = null, string? Output = null,
    bool Append = false, string? Error = null, bool AppendError = false, bool MergeError = false, bool MergeOriginalOutput = false);
public sealed record CommandGroup(IReadOnlyList<CommandStage> Stages, string Condition);

public static class CommandSyntax
{
    public static IReadOnlyList<CommandGroup> Parse(string input)
    {
        var tokens = Lex(input);
        var groups = new List<CommandGroup>();
        var stages = new List<CommandStage>();
        var words = new List<string>();
        string condition = "always";
        string? source = null, output = null, error = null;
        bool append = false, appendError = false, merge = false, mergeOriginal = false;
        void Stage()
        {
            if (words.Count == 0) throw new ArgumentException("Falta un comando entre operadores.");
            stages.Add(new(string.Join(" ", words), source, output, append, error, appendError, merge, mergeOriginal));
            words.Clear(); source = output = error = null; append = appendError = merge = mergeOriginal = false;
        }
        for (int i = 0; i < tokens.Count; i++)
        {
            string token = tokens[i];
            if (token == "|") { Stage(); continue; }
            if (token is "&&" or "||")
            {
                Stage(); groups.Add(new(stages.ToArray(), condition)); stages.Clear();
                condition = token == "&&" ? "success" : "failure"; continue;
            }
            if (token == "2>&1") { merge = true; mergeOriginal = output == null; error = null; continue; }
            if (token is "<" or ">" or ">>" or "2>" or "2>>")
            {
                if (++i >= tokens.Count || IsOperator(tokens[i])) throw new ArgumentException("Falta el archivo de redirección.");
                var path = CommandLine.ParseArguments(tokens[i]);
                if (path.Length != 1 || path[0].Length == 0) throw new ArgumentException("Ruta de redirección inválida.");
                switch (token)
                {
                    case "<": source = path[0]; break;
                    case ">": case ">>": output = path[0]; append = token == ">>"; break;
                    default: error = path[0]; appendError = token == "2>>"; merge = false; break;
                }
                continue;
            }
            words.Add(token);
        }
        Stage(); groups.Add(new(stages.ToArray(), condition));
        return groups;
    }

    private static bool IsOperator(string value) => value is "|" or "&&" or "||" or "<" or ">" or ">>" or "2>" or "2>>" or "2>&1";

    private static List<string> Lex(string input)
    {
        var result = new List<string>();
        int start = -1;
        char? quote = null;
        void Word(int end) { if (start >= 0) { result.Add(input[start..end]); start = -1; } }
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\\' && quote == '"' && i + 1 < input.Length && input[i + 1] == '"') { i++; continue; }
            if (quote != null) { if (c == quote) quote = null; continue; }
            if (c is '\'' or '"') { if (start < 0) start = i; quote = c; continue; }
            if (char.IsWhiteSpace(c)) { Word(i); continue; }
            string? op = null;
            if (start < 0 && input.AsSpan(i).StartsWith("2>&1")) op = "2>&1";
            else if (start < 0 && input.AsSpan(i).StartsWith("2>>")) op = "2>>";
            else if (start < 0 && input.AsSpan(i).StartsWith("2>")) op = "2>";
            else if (input.AsSpan(i).StartsWith("&&")) op = "&&";
            else if (input.AsSpan(i).StartsWith("||")) op = "||";
            else if (input.AsSpan(i).StartsWith(">>")) op = ">>";
            else if (c is '|' or '>' or '<') op = c.ToString();
            if (op != null) { Word(i); result.Add(op); i += op.Length - 1; }
            else if (start < 0) start = i;
        }
        if (quote != null) throw new ArgumentException("Comillas sin cerrar.");
        Word(input.Length);
        return result;
    }
}
