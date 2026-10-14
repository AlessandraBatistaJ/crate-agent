using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class BpmCsvImporterTests : IDisposable
{
    private readonly string _temp =
        Path.Combine(Path.GetTempPath(), "crateagent-bpm-" + Guid.NewGuid());

    public BpmCsvImporterTests() => Directory.CreateDirectory(_temp);

    public void Dispose()
    {
        if (Directory.Exists(_temp))
            Directory.Delete(_temp, true);
    }

    private string Csv(string conteudo)
    {
        var caminho = Path.Combine(_temp, "bpm.csv");
        File.WriteAllText(caminho, conteudo);
        return caminho;
    }

    [Theory]
    [InlineData("124", 124)]
    [InlineData("127.5", 128)]
    [InlineData("127,5", 128)]
    [InlineData("?", null)]
    [InlineData("", null)]
    [InlineData("abc", null)]
    public void ParseBpm_AceitaVirgulaEPontoERejeitaLixo(string texto, int? esperado)
    {
        Assert.Equal(esperado, BpmCsvImporter.ParseBpm(texto));
    }

    [Theory]
    [InlineData("8A", "8A")]
    [InlineData("12b", "12B")]
    [InlineData(" 11B ", "11B")]
    [InlineData("?", null)]
    [InlineData("\"", null)]
    [InlineData("A minor", null)]
    [InlineData("13A", null)]
    [InlineData("0A", null)]
    public void ParseKey_AceitaApenasNotacaoCamelot(string texto, string? esperado)
    {
        Assert.Equal(esperado, BpmCsvImporter.ParseKey(texto));
    }

    [Fact]
    public void Apply_PreencheSoOQueFalta()
    {
        var csv = Csv("arquivo;bpm;tom\nBlenk_-_Vibration.mp3;124;8A\nGIO.mp3;130;5B\n");
        var semTag = new Track { FilePath = Path.Combine("x", "Blenk_-_Vibration.mp3") };
        var comTag = new Track { FilePath = Path.Combine("x", "GIO.mp3"), Bpm = 100 };

        var aplicadas = BpmCsvImporter.Apply(new[] { semTag, comTag }, csv);

        Assert.Equal(124, semTag.Bpm);
        Assert.Equal("8A", semTag.Key);
        Assert.Equal(100, comTag.Bpm);   // a tag ganha
        Assert.Equal("5B", comTag.Key);  // mas o tom que faltava foi preenchido
        Assert.Equal(2, aplicadas);
    }

    [Fact]
    public void Apply_FaixaForaDoCsv_NaoMudaNada()
    {
        var csv = Csv("arquivo;bpm;tom\nOutra.mp3;120;1A\n");
        var track = new Track { FilePath = Path.Combine("x", "Blenk.mp3") };

        Assert.Equal(0, BpmCsvImporter.Apply(new[] { track }, csv));
        Assert.Null(track.Bpm);
    }
}