using System.Text.Json;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public static class LibraryMover
{
    public static MoveResult Execute(IEnumerable<PlannedMove> moves, string logPath)
    {
        var done = new List<MoveLogEntry>();
        var skipped = new List<string>();

        try
        {
            foreach (var move in moves)
            {
                if (!File.Exists(move.Source) || File.Exists(move.Destination))
                {
                    skipped.Add(move.Source);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(move.Destination)!);
                File.Move(move.Source, move.Destination);
                done.Add(new MoveLogEntry(move.Source, move.Destination));
            }
        }
        finally
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logPath))!);
            var json = JsonSerializer.Serialize(done, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(logPath, json);
        }

        return new MoveResult(done.Count, skipped);
    }

    public static MoveResult Undo(string logPath)
    {
        var entries = JsonSerializer.Deserialize<List<MoveLogEntry>>(File.ReadAllText(logPath))
                      ?? new List<MoveLogEntry>();
        var restored = 0;
        var skipped = new List<string>();

        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];

            if (!File.Exists(entry.Destination) || File.Exists(entry.Source))
            {
                skipped.Add(entry.Destination);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(entry.Source)!);
            File.Move(entry.Destination, entry.Source);
            restored++;
        }

        return new MoveResult(restored, skipped);
    }
}