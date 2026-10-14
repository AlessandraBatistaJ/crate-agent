using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public static class BpmCsvImporter
{
    // notação Camelot: 1A a 12B
    private static readonly Regex Camelot =
        new(@"^(1[0-2]|[1-9])[AB]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static int Apply(IEnumerable<Track> tracks, string csvPath)
    {
        var entries = Load(csvPath);
        var applied = 0;

        foreach (var track in tracks)
        {
            if (!entries.TryGetValue(Norm(Path.GetFileName(track.FilePath)), out var entry))
                continue;

            var changed = false;

            if (track.Bpm is null && entry.Bpm is not null)
            {
                track.Bpm = entry.Bpm;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(track.Key) && !string.IsNullOrWhiteSpace(entry.Key))
            {
                track.Key = entry.Key;
                changed = true;
            }

            if (changed)
                applied++;
        }

        return applied;
    }

    public static int? ParseBpm(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim() == "?")
            return null;

        var normalized = text.Trim().Replace(',', '.');

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? (int)Math.Round(value, MidpointRounding.AwayFromZero)
            : null;
    }

    public static string? ParseKey(string? text)
    {
        var trimmed = text?.Trim();
        return trimmed is not null && Camelot.IsMatch(trimmed) ? trimmed.ToUpperInvariant() : null;
    }

    private static Dictionary<string, (int? Bpm, string? Key)> Load(string csvPath)
    {
        var result = new Dictionary<string, (int? Bpm, string? Key)>();

        foreach (var line in File.ReadLines(csvPath).Skip(1))
        {
            var parts = line.Split(';');
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]))
                continue;

            var key = parts.Length > 2 ? ParseKey(parts[2]) : null;
            result.TryAdd(Norm(parts[0]), (ParseBpm(parts[1]), key));
        }

        return result;
    }

    private static string Norm(string text) =>
        text.Trim().Normalize(NormalizationForm.FormC);
}