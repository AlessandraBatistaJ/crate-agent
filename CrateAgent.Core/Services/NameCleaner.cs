using System.Text;
using System.Text.RegularExpressions;

namespace CrateAgent.Core.Services;

public static class NameCleaner
{
    private static readonly Regex PremiereWord =
        new(@"\bPremiere\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExtraSpaces =
        new(@"\s{2,}", RegexOptions.Compiled);

    public static string Clean(string text)
    {
        var result = text.Normalize(NormalizationForm.FormC);
        result = PremiereWord.Replace(result, " ");
        result = ExtraSpaces.Replace(result, " ");
        return result.Trim();
    }
}