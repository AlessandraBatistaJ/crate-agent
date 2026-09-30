using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class NameCleanerTests
{
    [Theory]
    [InlineData("PREMIERE Ignez", "Ignez")]
    [InlineData("DS Premiere Danny Wabbit", "DS Danny Wabbit")]
    [InlineData("Silent Rage SOMA SELECTS Premiere", "Silent Rage SOMA SELECTS")]
    [InlineData("Danny Wabbit", "Danny Wabbit")]
    [InlineData("Premiered Records", "Premiered Records")]
    public void Clean_RemoveApenasAPalavraPremiere(string entrada, string esperado)
    {
        Assert.Equal(esperado, NameCleaner.Clean(entrada));
    }

    [Fact]
    public void Clean_NormalizaAcentosDecompostos()
    {
        var decomposto = "GRU\u0308V02"; // "U" + trema separado
        Assert.Equal("GR\u00DCV02", NameCleaner.Clean(decomposto));
    }
}