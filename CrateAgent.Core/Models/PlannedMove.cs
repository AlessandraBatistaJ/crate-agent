namespace CrateAgent.Core.Models;

public record PlannedMove(string Source, string Destination, bool ToReview, bool IsDuplicate = false);