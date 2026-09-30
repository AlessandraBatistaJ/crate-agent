using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class FileNameParserTests
{
    [Theory]
    [InlineData("Blenk_-_Vibration.mp3", "Blenk", "Vibration")]
    [InlineData("Premiere_Temudo_—_Cohorus_MR039.mp3", "Premiere Temudo", "Cohorus MR039")]
    [InlineData("Pressurefunk_Premiere_Sveric_–_Razorsharp_HMN001.mp3", "Pressurefunk Premiere Sveric", "Razorsharp HMN001")]
    [InlineData("Night_Train.mp3", "", "Night Train")]
    public void Parse_SeparaArtistaETitulo(string arquivo, string artistaEsperado, string tituloEsperado)
    {
        var (artista, titulo) = FileNameParser.Parse(arquivo);

        Assert.Equal(artistaEsperado, artista);
        Assert.Equal(tituloEsperado, titulo);
    }
}
