using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class FileNameParserTests
{
    [Theory]
    [InlineData("Blenk_-_Vibration.mp3", "Blenk", "Vibration")]
    [InlineData("Premiere_Temudo_—_Cohorus_MR039.mp3", "Premiere Temudo", "Cohorus MR039")]
    [InlineData("Pressurefunk_Premiere_Sveric_–_Razorsharp_HMN001.mp3", "Sveric", "Razorsharp HMN001")]
    [InlineData("Night_Train.mp3", "", "Night Train")]
    public void Parse_SeparaArtistaETitulo(string arquivo, string artistaEsperado, string tituloEsperado)
    {
        var (artista, titulo) = FileNameParser.Parse(arquivo);

        Assert.Equal(artistaEsperado, artista);
        Assert.Equal(tituloEsperado, titulo);
    }

    [Theory]
    [InlineData("BCCO_Premiere_Mathys_Lenne_-_Mutant_MR039.mp3", "Mathys Lenne", "Mutant MR039")]
    [InlineData("DS_Premiere_RMK_-_Marine_MR039.mp3", "RMK", "Marine MR039")]
    [InlineData("A1_Benales_-_Cryo_Clergy.mp3", "Benales", "Cryo Clergy")]
    [InlineData("MR021_B3_Raffaele_Attanasio_-_South_Signatures.mp3", "Raffaele Attanasio", "South Signatures")]
    [InlineData("MR036_Digital_Bonus_01_Human_Safari_-_Dorian.mp3", "Human Safari", "Dorian")]
    [InlineData("PREMIERE_Setaoc_Mass_-_Sundogs_SK11013.mp3", "PREMIERE Setaoc Mass", "Sundogs SK11013")]
    [InlineData("Blenk_-_Vibration (1).mp3", "Blenk", "Vibration")]
    public void Parse_RemoveRuidoNoComecoDoArtista(string arquivo, string artistaEsperado, string tituloEsperado)
    {
        var (artista, titulo) = FileNameParser.Parse(arquivo);

        Assert.Equal(artistaEsperado, artista);
        Assert.Equal(tituloEsperado, titulo);
    }

    [Theory]
    [InlineData("PYLOT_-_Howl_RV006.mp3", "PYLOT", "Howl RV006")]
    [InlineData("Panteros666 MCYL - Pow.mp3", "Panteros666 MCYL", "Pow")]
    [InlineData("SAN_-_Se_ese_CBRTX004.mp3", "SAN", "Se ese CBRTX004")]
    [InlineData("GIO - I Wanna Dance.mp3", "GIO", "I Wanna Dance")]
    public void Parse_NaoApagaNomesLegitimos(string arquivo, string artistaEsperado, string tituloEsperado)
    {
        var (artista, titulo) = FileNameParser.Parse(arquivo);

        Assert.Equal(artistaEsperado, artista);
        Assert.Equal(tituloEsperado, titulo);
    }
}