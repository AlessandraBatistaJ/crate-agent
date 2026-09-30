using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class TrackReviewerTests
{
    [Theory]
    [InlineData("Danny Wabbit", "Cash Or Credit Original Mix", ReviewReason.None)]
    [InlineData("SAN", "Se ese", ReviewReason.None)]
    [InlineData("", "Night Train", ReviewReason.MissingArtist)]
    [InlineData("Temudo", "Cohorus MR039", ReviewReason.CatalogCode)]
    [InlineData("DS Danny Wabbit", "Suave ONI-Y002", ReviewReason.CatalogCode)]
    [InlineData("Saroc", "Bog GRÜV02", ReviewReason.CatalogCode)]
    [InlineData("Jon10", "All My Girls", ReviewReason.None)]
    [InlineData("Yrsen", "Yrsen - The Adversary", ReviewReason.SuspiciousTitle)]
    [InlineData("", "Shattered Dreams MR039", ReviewReason.MissingArtist | ReviewReason.CatalogCode)]
    public void Evaluate_IdentificaMotivosDeRevisao(string artista, string titulo, ReviewReason esperado)
    {
        var track = new Track { Artist = artista, Title = titulo };

        Assert.Equal(esperado, TrackReviewer.Evaluate(track));
    }
}