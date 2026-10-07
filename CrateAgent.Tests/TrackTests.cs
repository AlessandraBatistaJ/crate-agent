using CrateAgent.Core.Models;

namespace CrateAgent.Tests;

public class TrackTests
{
    [Theory]
    [InlineData(ReviewReason.None, false)]
    [InlineData(ReviewReason.CatalogCode, false)]
    [InlineData(ReviewReason.MissingArtist, true)]
    [InlineData(ReviewReason.SuspiciousTitle, true)]
    [InlineData(ReviewReason.CatalogCode | ReviewReason.MissingArtist, true)]
    [InlineData(ReviewReason.PrefixRemoved, true)]
    public void NeedsReview_CodigoDeCatalogoSozinhoNaoPedeRevisao(ReviewReason motivos, bool esperado)
    {
        var track = new Track { ReviewReasons = motivos };

        Assert.Equal(esperado, track.NeedsReview);
    }
}
