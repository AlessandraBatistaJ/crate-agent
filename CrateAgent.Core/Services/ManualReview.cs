using System.Text.RegularExpressions;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public enum ReviewAction { Skip, Approve, Edit, Invalid }

public record ReviewDecision(ReviewAction Action, string Artist = "", string Title = "");

public static class ManualReview
{
    // hífen, meia-risca (–) ou travessão (—), com espaços em volta
    private static readonly Regex Separator = new(@"\s+[-–—]\s+", RegexOptions.Compiled);

    public static ReviewDecision Parse(string? input, Track track)
    {
        var text = input?.Trim() ?? string.Empty;

        if (text.Length == 0)
            return new ReviewDecision(ReviewAction.Skip);

        if (text.Equals("s", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(track.Artist)
                ? new ReviewDecision(ReviewAction.Invalid)
                : new ReviewDecision(ReviewAction.Approve);

        var parts = Separator.Split(text, 2);

        if (parts.Length != 2)
            return new ReviewDecision(ReviewAction.Invalid);

        var artist = NameCleaner.Clean(parts[0]);
        var title = NameCleaner.Clean(parts[1]);

        return artist.Length == 0 || title.Length == 0
            ? new ReviewDecision(ReviewAction.Invalid)
            : new ReviewDecision(ReviewAction.Edit, artist, title);
    }

    public static void Apply(Track track, ReviewDecision decision)
    {
        if (decision.Action == ReviewAction.Edit)
        {
            track.Artist = decision.Artist;
            track.Title = decision.Title;
        }

        if (decision.Action is ReviewAction.Approve or ReviewAction.Edit)
            track.ReviewReasons = ReviewReason.None;
    }
}