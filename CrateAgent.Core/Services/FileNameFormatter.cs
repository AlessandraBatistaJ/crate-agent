namespace CrateAgent.Core.Services;

public static class FileNameFormatter
{
    public static string Format(string artist, string title, string extension)
    {
        var a = Underscore(artist);
        var t = Underscore(title);
        var ext = extension.ToLowerInvariant();

        return a.Length == 0 ? $"{t}{ext}" : $"{a} - {t}{ext}";
    }

    private static string Underscore(string text) =>
        string.Join('_', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
