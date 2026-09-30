using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class LibraryOrganizerTests
{
    private const string Root = "biblioteca";

    [Fact]
    public void Plan_FaixaOk_VaiParaPastaDoArtista()
    {
        var track = new Track
        {
            FilePath = Path.Combine("downloads", "x.mp3"),
            Artist = "Blenk",
            Title = "Vibration"
        };

        var moves = LibraryOrganizer.Plan(new[] { track }, Root);

        Assert.Equal(Path.Combine(Root, "Blenk", "Blenk - Vibration.mp3"), moves[0].Destination);
        Assert.False(moves[0].ToReview);
    }

    [Fact]
    public void Plan_FaixaComRevisao_VaiParaRevisarComNomeOriginal()
    {
        var track = new Track
        {
            FilePath = Path.Combine("downloads", "Night_Train.mp3"),
            Title = "Night Train",
            ReviewReasons = ReviewReason.MissingArtist
        };

        var moves = LibraryOrganizer.Plan(new[] { track }, Root);

        Assert.Equal(Path.Combine(Root, "_Revisar", "Night_Train.mp3"), moves[0].Destination);
        Assert.True(moves[0].ToReview);
    }

    [Fact]
    public void Plan_RemoveCaracteresProibidos()
    {
        var track = new Track
        {
            FilePath = Path.Combine("downloads", "y.mp3"),
            Artist = "AC/DC",
            Title = "Back: In Black?"
        };

        var moves = LibraryOrganizer.Plan(new[] { track }, Root);

        Assert.Equal(Path.Combine(Root, "ACDC", "ACDC - Back In Black.mp3"), moves[0].Destination);
    }

    [Fact]
    public void Plan_DestinosIguais_GanhamNumeroNoSegundo()
    {
        var a = new Track { FilePath = Path.Combine("d", "1.mp3"), Artist = "Isaiah", Title = "Touch" };
        var b = new Track { FilePath = Path.Combine("d", "2.mp3"), Artist = "Isaiah", Title = "Touch" };

        var moves = LibraryOrganizer.Plan(new[] { a, b }, Root);

        Assert.Equal(Path.Combine(Root, "Isaiah", "Isaiah - Touch.mp3"), moves[0].Destination);
        Assert.Equal(Path.Combine(Root, "Isaiah", "Isaiah - Touch (2).mp3"), moves[1].Destination);
    }
}
