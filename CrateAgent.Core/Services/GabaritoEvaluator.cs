using System.Text;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public record EvalRow(string Original, string Expected, string Actual)
{
    public bool Correct => Expected == Actual;
}

public static class GabaritoEvaluator
{
    public static (List<EvalRow> Rows, List<string> NotFound) Evaluate(
        IEnumerable<Track> tracks, string csvPath)
    {
        var expected = new Dictionary<string, string>();

        foreach (var line in File.ReadLines(csvPath).Skip(1))
        {
            var parts = line.Split(';');
            if (parts.Length < 2) continue;

            var original = Norm(parts[0]);
            var wanted = Norm(parts[1]);
            if (original.Length == 0 || wanted.Length == 0 || wanted == "?") continue;

            expected.TryAdd(original, wanted);
        }

        var rows = new List<EvalRow>();
        var seen = new HashSet<string>();

        foreach (var track in tracks)
        {
            var original = Norm(Path.GetFileName(track.FilePath));
            if (!expected.TryGetValue(original, out var wanted)) continue;

            seen.Add(original);
            var actual = Norm(FileNameFormatter.Format(
                track.Artist, track.Title, Path.GetExtension(track.FilePath)));
            rows.Add(new EvalRow(original, wanted, actual));
        }

        var notFound = expected.Keys.Where(k => !seen.Contains(k)).ToList();
        return (rows, notFound);
    }

    private static string Norm(string text) =>
        text.Trim().Normalize(NormalizationForm.FormC);
}