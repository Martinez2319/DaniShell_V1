using System.Text;

namespace DaniShell.Core;

public static class InputHandler
{
    private static List<string> _history = new();
    private static int _historyIndex = -1;

    public static void LoadHistory()
    {
        try
        {
            if (File.Exists(Configuration.HistoryFile))
                _history = File.ReadAllLines(Configuration.HistoryFile).ToList();
        }
        catch { }
    }

    public static void AddToHistory(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd) || cmd.StartsWith(' ')) return;

        _history.RemoveAll(item => item == cmd);
        _history.Add(cmd);
        if (_history.Count > Configuration.Config.HistoryLimit)
            _history.RemoveAt(0);

        try { File.WriteAllLines(Configuration.HistoryFile, _history); } catch { }
    }

    public static List<string> GetHistory() => _history;

    public static IReadOnlyList<string> SearchHistory(string query) => _history.AsEnumerable().Reverse()
        .Where(item => item.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();

    public static void ClearHistory()
    {
        _history.Clear();
        try { File.WriteAllText(Configuration.HistoryFile, ""); } catch { }
    }

    public static string ReadLine(string currentDir, Func<string, string> autoComplete)
    {
        var input = new StringBuilder();
        int cursorPos = 0;
        _historyIndex = -1;
        string tempInput = "";
        var completion = new CompletionCycle();

        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.R && key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                var found = ReverseSearch(currentDir);
                if (found != null) { input.Clear(); input.Append(found); cursorPos = input.Length; }
                RedrawInput(input, cursorPos, currentDir); continue;
            }

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return input.ToString();

                case ConsoleKey.Backspace:
                    if (cursorPos > 0)
                    {
                        input.Remove(cursorPos - 1, 1);
                        cursorPos--;
                        RedrawInput(input, cursorPos, currentDir);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursorPos < input.Length)
                    {
                        input.Remove(cursorPos, 1);
                        RedrawInput(input, cursorPos, currentDir);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursorPos > 0)
                    {
                        cursorPos--;
                        MoveCursor(-1);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursorPos < input.Length)
                    {
                        cursorPos++;
                        MoveCursor(1);
                    }
                    break;

                case ConsoleKey.Home:
                    if (cursorPos > 0)
                    {
                        MoveCursor(-cursorPos);
                        cursorPos = 0;
                    }
                    break;

                case ConsoleKey.End:
                    if (cursorPos < input.Length)
                    {
                        MoveCursor(input.Length - cursorPos);
                        cursorPos = input.Length;
                    }
                    break;

                case ConsoleKey.UpArrow:
                    if (_history.Count > 0)
                    {
                        if (_historyIndex == -1)
                        {
                            tempInput = input.ToString();
                            _historyIndex = _history.Count - 1;
                        }
                        else if (_historyIndex > 0)
                        {
                            _historyIndex--;
                        }

                        input.Clear();
                        input.Append(_history[_historyIndex]);
                        cursorPos = input.Length;
                        RedrawInput(input, cursorPos, currentDir);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (_history.Count > 0 && _historyIndex != -1)
                    {
                        if (_historyIndex < _history.Count - 1)
                        {
                            _historyIndex++;
                            input.Clear();
                            input.Append(_history[_historyIndex]);
                        }
                        else
                        {
                            _historyIndex = -1;
                            input.Clear();
                            input.Append(tempInput);
                        }

                        cursorPos = input.Length;
                        RedrawInput(input, cursorPos, currentDir);
                    }
                    break;

                case ConsoleKey.Tab:
                    var completed = completion.Next(input.ToString(), currentDir, key.Modifiers.HasFlag(ConsoleModifiers.Shift));
                    if (!string.IsNullOrEmpty(completed) && completed != input.ToString())
                    {
                        input.Clear();
                        input.Append(completed);
                        cursorPos = input.Length;
                        RedrawInput(input, cursorPos, currentDir);
                    }
                    break;

                case ConsoleKey.Escape:
                    input.Clear();
                    cursorPos = 0;
                    RedrawInput(input, cursorPos, currentDir);
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        input.Insert(cursorPos, key.KeyChar);
                        cursorPos++;
                        RedrawInput(input, cursorPos, currentDir);
                    }
                    break;
            }
        }
    }

    private static string? ReverseSearch(string currentDir)
    {
        string query = ""; int index = 0;
        while (true)
        {
            var matches = SearchHistory(query);
            string? choice = matches.Count == 0 ? null : matches[index % matches.Count];
            ClearLine(); Console.Write($"(Ctrl+R: {query}) {choice}");
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) return choice;
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control)) return null;
            if (key.Key == ConsoleKey.R && key.Modifiers.HasFlag(ConsoleModifiers.Control)) { index++; continue; }
            if (key.Key == ConsoleKey.Backspace) { if (query.Length > 0) query = query[..^1]; }
            else if (!char.IsControl(key.KeyChar)) query += key.KeyChar;
            index = 0;
        }
    }

    private static void RedrawInput(StringBuilder input, int cursorPos, string currentDir)
    {
        ClearLine();
        Shell.PrintPrompt(currentDir, false);
        Console.Write(input.ToString());

        int offset = input.Length - cursorPos;
        if (offset > 0)
            MoveCursor(-offset);
    }

    private static void ClearLine()
    {
        try
        {
            int row = Console.CursorTop;
            Console.SetCursorPosition(0, row);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, row);
        }
        catch
        {
            Console.WriteLine();
        }
    }

    private static void MoveCursor(int offset)
    {
        try
        {
            int newPos = Console.CursorLeft + offset;
            if (newPos >= 0 && newPos < Console.WindowWidth)
                Console.SetCursorPosition(newPos, Console.CursorTop);
        }
        catch { }
    }
}
