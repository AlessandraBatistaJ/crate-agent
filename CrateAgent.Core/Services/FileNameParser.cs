using System.Text.RegularExpressions;

namespace CrateAgent.Core.Services;

public static class FileNameParser
{
    // hífen, meia-risca (–) ou travessão (—), com espaços em volta
    private static readonly Regex Separator = new(@"\s+[-–—]\s+", RegexOptions.Compiled);

    public static (string Artist, string Title) Parse(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath).Replace('_', ' ').Trim();

        var parts = Separator.Split(name, 2);

        return parts.Length == 2
            ? (parts[0].Trim(), parts[1].Trim())
            : (string.Empty, name);
    }
}