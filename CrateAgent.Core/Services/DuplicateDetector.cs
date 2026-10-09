using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public record DuplicateGroup(Track Keep, List<Track> Duplicates);

public static class DuplicateDetector
{
    // marcador de cópia do Windows no fim do nome: "(1)", "(2)"...
    private static readonly Regex CopyMarker = new(@"\(\d+\)\s*$", RegexOptions.Compiled);

    public static List<DuplicateGroup> Find(IEnumerable<Track> tracks)
    {
        var groups = new List<DuplicateGroup>();

        var sameSize = tracks
            .Where(t => File.Exists(t.FilePath))
            .GroupBy(t => new FileInfo(t.FilePath).Length)
            .Where(g => g.Count() > 1);

        foreach (var sizeGroup in sameSize)
        {
            foreach (var sameContent in sizeGroup.GroupBy(t => Hash(t.FilePath)).Where(g => g.Count() > 1))
            {
                var ordered = sameContent
                    .OrderBy(t => HasCopyMarker(t.FilePath))
                    .ThenBy(t => t.FilePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                groups.Add(new DuplicateGroup(ordered[0], ordered.Skip(1).ToList()));
            }
        }

        return groups;
    }

    private static bool HasCopyMarker(string path) =>
        CopyMarker.IsMatch(Path.GetFileNameWithoutExtension(path));

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
