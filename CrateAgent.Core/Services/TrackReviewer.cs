using System.Text.RegularExpressions;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public static class TrackReviewer
{
    // 2+ letras maiúsculas, até 4 letras, hífen+letra opcional, e números: MR039, ONI-Y002, GRÜV02
    private static readonly Regex CatalogCode =
        new(@"\b\p{Lu}{2}\p{L}{0,4}(-\p{L})?\d+\b", RegexOptions.Compiled);

    public static ReviewReason Evaluate(Track track)
    {
        var reasons = ReviewReason.None;

        if (string.IsNullOrWhiteSpace(track.Artist))
            reasons |= ReviewReason.MissingArtist;

        if (CatalogCode.IsMatch(track.Artist) || CatalogCode.IsMatch(track.Title))
            reasons |= ReviewReason.CatalogCode;

        if (track.Title.Contains(" - "))
            reasons |= ReviewReason.SuspiciousTitle;

        return reasons;
    }
}