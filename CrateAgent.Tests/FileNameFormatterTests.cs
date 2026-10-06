using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class FileNameFormatterTests
{
    [Theory]
    [InlineData("Marko Nastic", "Her Name Was Rio", ".mp3", "Marko_Nastic - Her_Name_Was_Rio.mp3")]
    [InlineData("", "Night Train", ".mp3", "Night_Train.mp3")]
    [InlineData("Saroc", "Bog GRÜV02", ".MP3", "Saroc - Bog_GRÜV02.mp3")]
    public void Format_UsaUnderscoreEHifenComEspacos(
        string artista, string titulo, string extensao, string esperado)
    {
        Assert.Equal(esperado, FileNameFormatter.Format(artista, titulo, extensao));
    }
}