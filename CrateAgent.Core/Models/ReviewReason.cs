namespace CrateAgent.Core.Models;

[Flags]
public enum ReviewReason
{
    None = 0,
    MissingArtist = 1,
    CatalogCode = 2,
    SuspiciousTitle = 4,
    PrefixRemoved = 8
}