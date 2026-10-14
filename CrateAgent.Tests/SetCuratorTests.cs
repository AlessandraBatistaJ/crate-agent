using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class SetCuratorTests
{
    private static Track T(string nome, int? bpm, string? tom) =>
        new() { FilePath = nome, Bpm = bpm, Key = tom };

    [Theory]
    [InlineData("8A", "8A", 0)]
    [InlineData("8A", "9A", 0)]
    [InlineData("8A", "7A", 0)]
    [InlineData("12A", "1A", 0)]
    [InlineData("1A", "12A", 0)]
    [InlineData("8A", "8B", 0)]
    [InlineData("8A", "9B", 2)]
    [InlineData("8A", "10A", 2)]
    [InlineData("8A", null, 1)]
    [InlineData(null, "8A", 1)]
    [InlineData("8A", "?", 1)]
    public void HarmonicRank_SegueARodaCamelot(string? de, string? para, int esperado)
    {
        Assert.Equal(esperado, SetCurator.HarmonicRank(de, para));
    }

    [Fact]
    public void Build_OrdenaPorBpmCrescente()
    {
        var a = T("a.mp3", 140, "8A");
        var b = T("b.mp3", 138, "8A");
        var c = T("c.mp3", 142, "8A");

        var set = SetCurator.Build(new[] { a, b, c }, new SetRequest(138, 146, 10));

        Assert.Equal(new[] { b, a, c }, set);
    }

    [Fact]
    public void Build_IgnoraFaixasSemBpmOuForaDaFaixa()
    {
        var dentro = T("dentro.mp3", 140, "8A");
        var semBpm = T("sem.mp3", null, "8A");
        var baixo = T("baixo.mp3", 120, "8A");
        var alto = T("alto.mp3", 150, "8A");

        var set = SetCurator.Build(new[] { dentro, semBpm, baixo, alto }, new SetRequest(138, 146, 10));

        Assert.Equal(dentro, Assert.Single(set));
    }

    [Fact]
    public void Build_PrefereTomCompativelAoMenorSalto()
    {
        var inicio = T("inicio.mp3", 138, "8A");
        var incompativel = T("incompativel.mp3", 139, "3B");
        var compativel = T("compativel.mp3", 141, "9A");

        var set = SetCurator.Build(new[] { inicio, incompativel, compativel }, new SetRequest(138, 146, 10));

        Assert.Equal(compativel, set[1]);
    }

    [Fact]
    public void Build_ParaQuandoNaoHaFaixaDentroDoSalto()
    {
        var inicio = T("inicio.mp3", 138, "8A");
        var longe = T("longe.mp3", 150, "8A");

        var set = SetCurator.Build(new[] { inicio, longe }, new SetRequest(138, 160, 10));

        Assert.Equal(inicio, Assert.Single(set));
    }

    [Fact]
    public void Build_RespeitaONumeroMaximoDeFaixas()
    {
        var faixas = Enumerable.Range(138, 5).Select(b => T($"f{b}.mp3", b, "8A")).ToList();

        var set = SetCurator.Build(faixas, new SetRequest(138, 146, 2));

        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void Build_SemFaixasComBpm_RetornaVazio()
    {
        var set = SetCurator.Build(new[] { T("a.mp3", null, "8A") }, new SetRequest(138, 146, 10));

        Assert.Empty(set);
    }
}