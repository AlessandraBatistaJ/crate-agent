using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public static class LibraryOrganizer
{
    private static readonly char[] InvalidChars =
        { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

    public static List<PlannedMove> Plan(
        IEnumerable<Track> tracks, string destinationRoot, ISet<string>? duplicates = null)
    {
        var moves = new List<PlannedMove>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var track in tracks)
        {
            if (duplicates is not null && duplicates.Contains(track.FilePath))
            {
                var dupDestination = MakeUnique(
                    Path.Combine(destinationRoot, "_Duplicadas", Path.GetFileName(track.FilePath)), used);

                moves.Add(new PlannedMove(track.FilePath, dupDestination, false, true));
                continue;
            }

            string folder, fileName;

            if (track.NeedsReview)
            {
                folder = "_Revisar";
                fileName = Path.GetFileName(track.FilePath);
            }
            else
            {
                var artist = Sanitize(track.Artist);
                var title = Sanitize(track.Title);

                folder = artist; // para a pasta também com underscore: Underscore(artist)
                fileName = FileNameFormatter.Format(artist, title, Path.GetExtension(track.FilePath));
            }

            var destination = MakeUnique(Path.Combine(destinationRoot, folder, fileName), used);
            moves.Add(new PlannedMove(track.FilePath, destination, track.NeedsReview));
        }

        return moves;
    }

    private static string Sanitize(string name)
    {
        var cleaned = string.Concat(name.Split(InvalidChars));
        return cleaned.Trim().TrimEnd('.');
    }

    private static string MakeUnique(string path, HashSet<string> used)
    {
        if (used.Add(path))
            return path;

        var folder = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        for (var i = 2; ; i++)
        {
            var candidate = Path.Combine(folder, $"{name} ({i}){extension}");
            if (used.Add(candidate))
                return candidate;
        }
    }
}