using System.Text.RegularExpressions;

namespace CrateAgent.Core.Services;

public static class FileNameParser
{
    // hífen, meia-risca (–) ou travessão (—), com espaços em volta
    private static readonly Regex Separator = new(@"\s+[-–—]\s+", RegexOptions.Compiled);

    // marcador de duplicado do Windows no fim do nome: " (1)"
    private static readonly Regex DuplicateMarker = new(@"\s*\(\d+\)\s*$", RegexOptions.Compiled);

    // "BCCO Premiere Mathys Lenne" -> pega só o que vem depois de "Premiere"
    private static readonly Regex ChannelBeforePremiere =
        new(@"^.*\S\s+Premiere\s+(?<rest>\S.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // lixo no começo do artista: posição de vinil, código de lançamento, "Digital Bonus 01"
    private static readonly Regex[] LeadingNoise =
    {
        new(@"^[A-D]\d{1,2}\s+(?=\S)", RegexOptions.Compiled),
        new(@"^\p{Lu}{2,4}\d{2,4}\s+(?=\S)", RegexOptions.Compiled),
        new(@"^Digital\s+Bonus\s+\d+\s+(?=\S)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public static (string Artist, string Title) Parse(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath).Replace('_', ' ').Trim();
        name = DuplicateMarker.Replace(name, string.Empty);

        var parts = Separator.Split(name, 2);

        return parts.Length == 2
            ? (CleanArtist(parts[0].Trim()), parts[1].Trim())
            : (string.Empty, name);
    }

    private static string CleanArtist(string artist)
    {
        var match = ChannelBeforePremiere.Match(artist);
        if (match.Success)
            artist = match.Groups["rest"].Value;

        bool changed;
        do
        {
            changed = false;
            foreach (var rule in LeadingNoise)
            {
                var cleaned = rule.Replace(artist, string.Empty, 1);
                if (cleaned != artist)
                {
                    artist = cleaned;
                    changed = true;
                }
            }
        } while (changed);

        return artist.Trim();
    }
}