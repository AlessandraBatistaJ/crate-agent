using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class ManualReviewTests
{
    [Theory]
    [InlineData("", "Blenk", ReviewAction.Skip)]
    [InlineData("s", "Blenk", ReviewAction.Approve)]
    [InlineData("S", "Blenk", ReviewAction.Approve)]
    [InlineData("s", "", ReviewAction.Invalid)]
    [InlineData("Night Owl - Night Train", "", ReviewAction.Edit)]
    [InlineData("sem separador", "Blenk", ReviewAction.Invalid)]
    public void Parse_ClassificaAEntrada(string entrada, string artistaAtual, ReviewAction esperado)
    {
        var track = new Track { Artist = artistaAtual, Title = "Qualquer" };

        Assert.Equal(esperado, ManualReview.Parse(entrada, track).Action);
    }

    [Fact]
    public void Parse_Edicao_LimpaOsNomes()
    {
        var track = new Track();

        var decisao = ManualReview.Parse("PREMIERE Setaoc Mass — Sundogs", track);

        Assert.Equal(ReviewAction.Edit, decisao.Action);
        Assert.Equal("Setaoc Mass", decisao.Artist);
        Assert.Equal("Sundogs", decisao.Title);
    }

    [Fact]
    public void Apply_Edicao_AlteraNomesELimpaARevisao()
    {
        var track = new Track { Title = "Night Train", ReviewReasons = ReviewReason.MissingArtist };

        ManualReview.Apply(track, new ReviewDecision(ReviewAction.Edit, "Night Owl", "Night Train"));

        Assert.Equal("Night Owl", track.Artist);
        Assert.False(track.NeedsReview);
    }

    [Fact]
    public void Apply_Aprovacao_LimpaARevisaoSemMudarNomes()
    {
        var track = new Track { Artist = "Temudo", Title = "Cohorus", ReviewReasons = ReviewReason.PrefixRemoved };

        ManualReview.Apply(track, new ReviewDecision(ReviewAction.Approve));

        Assert.Equal("Temudo", track.Artist);
        Assert.False(track.NeedsReview);
    }

    [Fact]
    public void Apply_Pular_NaoMudaNada()
    {
        var track = new Track { Title = "Night Train", ReviewReasons = ReviewReason.MissingArtist };

        ManualReview.Apply(track, new ReviewDecision(ReviewAction.Skip));

        Assert.True(track.NeedsReview);
    }
}