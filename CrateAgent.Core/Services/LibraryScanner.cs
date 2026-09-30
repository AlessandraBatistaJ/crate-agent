using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public class LibraryScanner
{
    private static readonly string[] SupportedExtensions =
        { ".mp3", ".flac", ".wav", ".m4a", ".aiff" };

    public IEnumerable<Track> Scan(string folderPath)
    {
        var files = Directory
            .EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(f => SupportedExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant()));

        foreach (var file in files)
        {
            var track = ReadTrack(file);
            if (track is not null)
                yield return track;
        }
    }

    private static Track? ReadTrack(string filePath)
    {
        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            var tag = tagFile.Tag;

            var (fileArtist, fileTitle) = ParseFileName(filePath);

            return new Track
            {
                FilePath = filePath,
                Title = string.IsNullOrWhiteSpace(tag.Title) ? fileTitle : tag.Title,
                Artist = string.IsNullOrWhiteSpace(tag.FirstPerformer) ? fileArtist : tag.FirstPerformer,
                Album = tag.Album ?? string.Empty,
                Year = tag.Year == 0 ? null : tag.Year,
                Genre = tag.FirstGenre,
                Bpm = tag.BeatsPerMinute == 0 ? null : (int)tag.BeatsPerMinute,
                Duration = tagFile.Properties.Duration
            };
        }
        catch (Exception)
        {
            return null; // arquivo corrompido ou ilegível: ignora
        }
    }

    private static (string Artist, string Title) ParseFileName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath).Replace('_', ' ');

        var parts = name.Split(" - ", 2, StringSplitOptions.TrimEntries);

        return parts.Length == 2
            ? (parts[0], parts[1])
            : (string.Empty, name.Trim());
    }
}