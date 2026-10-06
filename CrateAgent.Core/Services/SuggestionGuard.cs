using System.Text;
using System.Text.RegularExpressions;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public record GuardResult(bool Accepted, string Reason);

public static class SuggestionGuard
{
    private static readonly Regex Word = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    public static GuardResult Validate(string fileName, TrackSuggestion suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion.Title))
            return new GuardResult(false, "Título vazio");

        if (suggestion.Title.Contains(" - "))
            return new GuardResult(false, "Título ainda contém o separador ' - '");

        if (suggestion.Artist.Length > 0 &&
            string.Equals(suggestion.Artist, suggestion.Title, StringComparison.OrdinalIgnoreCase))
            return new GuardResult(false, "Artista igual ao título");

        var fileWords = Words(Path.GetFileNameWithoutExtension(fileName));
        var invented = Words(suggestion.Artist).Concat(Words(suggestion.Title))
            .FirstOrDefault(w => !fileWords.Contains(w));

        if (invented is not null)
            return new GuardResult(false, $"A palavra '{invented}' não está no nome do arquivo");

        return new GuardResult(true, "ok");
    }

    private static HashSet<string> Words(string text) =>
        Word.Matches(text.Normalize(NormalizationForm.FormC).ToLowerInvariant())
            .Select(m => m.Value)
            .ToHashSet();
}