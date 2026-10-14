using System.Text.RegularExpressions;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public static class SetCurator
{
    private static readonly Regex Camelot =
        new(@"^(?<n>1[0-2]|[1-9])(?<l>[AB])$", RegexOptions.Compiled);

    public static List<Track> Build(IEnumerable<Track> tracks, SetRequest request)
    {
        var pool = tracks
            .Where(t => t.Bpm is not null && t.Bpm >= request.StartBpm && t.Bpm <= request.EndBpm)
            .OrderBy(t => t.Bpm)
            .ThenBy(t => t.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var set = new List<Track>();

        if (pool.Count == 0 || request.MaxTracks <= 0)
            return set;

        var current = pool[0];
        set.Add(current);
        pool.RemoveAt(0);

        while (set.Count < request.MaxTracks)
        {
            var next = pool
                .Where(t => t.Bpm >= current.Bpm && t.Bpm - current.Bpm <= request.MaxBpmJump)
                .OrderBy(t => HarmonicRank(current.Key, t.Key))
                .ThenBy(t => t.Bpm - current.Bpm)
                .ThenBy(t => t.FilePath, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (next is null)
                break;

            set.Add(next);
            pool.Remove(next);
            current = next;
        }

        return set;
    }

    // 0 = compatível, 1 = tom desconhecido, 2 = incompatível
    public static int HarmonicRank(string? from, string? to)
    {
        var a = ParseKey(from);
        var b = ParseKey(to);

        if (a is null || b is null)
            return 1;

        var (n1, l1) = a.Value;
        var (n2, l2) = b.Value;

        if (l1 == l2)
        {
            var diff = Math.Abs(n1 - n2);
            return diff is 0 or 1 or 11 ? 0 : 2;   // 12 e 1 são vizinhos na roda
        }

        return n1 == n2 ? 0 : 2;
    }

    private static (int Number, char Letter)? ParseKey(string? key)
    {
        if (key is null)
            return null;

        var match = Camelot.Match(key.Trim().ToUpperInvariant());

        return match.Success
            ? (int.Parse(match.Groups["n"].Value), match.Groups["l"].Value[0])
            : null;
    }
}