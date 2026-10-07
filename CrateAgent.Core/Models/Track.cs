namespace CrateAgent.Core.Models;

public class Track
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public uint? Year { get; set; }
    public string? Genre { get; set; }
    public int? Bpm { get; set; }
    public string? Key { get; set; }
    public TimeSpan Duration { get; set; }
    public ReviewReason ReviewReasons { get; set; } = ReviewReason.None;
    public bool NeedsReview =>
        (ReviewReasons & (ReviewReason.MissingArtist | ReviewReason.SuspiciousTitle | ReviewReason.PrefixRemoved))
        != ReviewReason.None;

}