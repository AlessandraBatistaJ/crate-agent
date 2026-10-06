using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class SuggestionGuardTests
{
    [Theory]
    [InlineData("Side To Side.mp3", "Ariana Grande", "Side To Side", false)]
    [InlineData("Meet_The_People.mp3", "Meet The People", "Meet The People", false)]
    [InlineData("BCCO_Premiere_Mathys_Lenne_-_Mutant_MR039.mp3", "BCCO", "Mathys Lenne - Mutant", false)]
    [InlineData("Night_Train.mp3", "", "Night Train", true)]
    [InlineData("DS_Premiere_Danny_Wabbit_-_Suave_ONI-Y002.mp3", "Danny Wabbit", "Suave", true)]
    [InlineData("DJ_CRÈME_BRÛLÉE_-_MIDFIELD_SYNVA01_SYNTHESIZEE.mp3", "DJ CRÈME BRÛLÉE", "MIDFIELD", true)]
    public void Validate_RejeitaInvencaoEAceitaSugestoesFieis(
        string arquivo, string artista, string titulo, bool esperado)
    {
        var sugestao = new TrackSuggestion(artista, titulo, "");

        var resultado = SuggestionGuard.Validate(arquivo, sugestao);

        Assert.Equal(esperado, resultado.Accepted);
    }
}