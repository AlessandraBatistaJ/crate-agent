namespace CrateAgent.Core.Models;

public record MoveLogEntry(string Source, string Destination);

public record MoveResult(int Count, List<string> Skipped);