namespace CrateAgent.Core.Models;

public record SetRequest(int StartBpm, int EndBpm, int MaxTracks, int MaxBpmJump = 4);