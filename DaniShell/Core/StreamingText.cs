using System.Text;

namespace DaniShell.Core;

public static class StreamingText
{
    public static (long Lines, long Words, long Bytes) Count(TextReader reader)
    {
        var buffer = new char[4096]; var encoder = Encoding.UTF8.GetEncoder();
        long lines = 0, words = 0, bytes = 0; bool inWord = false; int count;
        while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            bytes += encoder.GetByteCount(buffer, 0, count, false);
            for (int i = 0; i < count; i++)
            {
                char c = buffer[i]; if (c == '\n') lines++;
                if (char.IsWhiteSpace(c)) inWord = false;
                else if (!inWord) { words++; inWord = true; }
            }
        }
        bytes += encoder.GetByteCount(Array.Empty<char>(), 0, 0, true);
        return (lines, words, bytes);
    }

    public static IEnumerable<string> Sort(IEnumerable<string> input, bool reverse, bool numeric, bool unique)
    {
        var comparer = Comparer<string>.Create((a, b) =>
        {
            int value;
            if (numeric)
            {
                double.TryParse(a.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), out double x);
                double.TryParse(b.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), out double y);
                value = x.CompareTo(y);
            }
            else value = StringComparer.OrdinalIgnoreCase.Compare(a, b);
            return reverse ? -value : value;
        });
        string directory = Path.Combine(Environment.GetEnvironmentVariable("DANISHELL_PIPELINE_TEMP") ?? Path.GetTempPath(), "DaniShell-sort-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var files = new List<string>(); var chunk = new List<string>(8192); int serial = 0;
        string Save(IEnumerable<string> lines)
        {
            string path = Path.Combine(directory, (serial++).ToString() + ".txt");
            File.WriteAllLines(path, lines, new UTF8Encoding(false)); return path;
        }
        try
        {
            foreach (var line in input)
            {
                chunk.Add(line);
                if (chunk.Count == 8192) { chunk.Sort(comparer); files.Add(Save(chunk)); chunk.Clear(); }
            }
            if (chunk.Count > 0) { chunk.Sort(comparer); files.Add(Save(chunk)); }
            // Merge at most 64 files at a time, bounding both memory and open handles.
            while (files.Count > 64)
            {
                var merged = new List<string>();
                foreach (var group in files.Chunk(64)) { merged.Add(Save(Merge(group, comparer))); foreach (var path in group) File.Delete(path); }
                files = merged;
            }
            string? previous = null;
            foreach (var line in Merge(files, comparer))
            { if (!unique || previous != line) yield return line; previous = line; }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static IEnumerable<string> Merge(IEnumerable<string> paths, IComparer<string> comparer)
    {
        var readers = paths.Select(File.OpenText).ToArray();
        var queue = new PriorityQueue<(string Line, int Source), string>(comparer);
        try
        {
            for (int i = 0; i < readers.Length; i++) if (readers[i].ReadLine() is string line) queue.Enqueue((line, i), line);
            while (queue.TryDequeue(out var item, out _))
            {
                yield return item.Line;
                if (readers[item.Source].ReadLine() is string line) queue.Enqueue((line, item.Source), line);
            }
        }
        finally { foreach (var reader in readers) reader.Dispose(); }
    }
}
