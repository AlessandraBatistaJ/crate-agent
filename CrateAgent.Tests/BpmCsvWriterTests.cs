using System.Globalization;
using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class BpmCsvWriterTests : IDisposable
{
    private readonly string _temp =
        Path.Combine(Path.GetTempPath(), "crateagent-writer-" + Guid.NewGuid());

    public BpmCsvWriterTests() => Directory.CreateDirectory(_temp);

    public void Dispose()
    {
        if (Directory.Exists(_temp))
            Directory.Delete(_temp, true);
    }

    private string Caminho => Path.Combine(_temp, "bpm_detectado.csv");

    [Fact]
    public void Format_UsaPontoDecimalMesmoEmCulturaBrasileira()
    {
        var anterior = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

            Assert.Equal("a.mp3;139.6;;detectado", BpmCsvWriter.Format("a.mp3", 139.6));
        }
        finally
        {
            CultureInfo.CurrentCulture = anterior;
        }
    }

    [Fact]
    public void Format_NomeComPontoEVirgula_RetornaNull()
    {
        Assert.Null(BpmCsvWriter.Format("faixa; remix.mp3", 128));
    }

    [Fact]
    public void Append_CriaOArquivoComCabecalho()
    {
        BpmCsvWriter.Append(Caminho, "a.mp3", 128);

        var linhas = File.ReadAllLines(Caminho);

        Assert.Equal(2, linhas.Length);
        Assert.Equal("arquivo;bpm;tom;obs", linhas[0]);
        Assert.Equal("a.mp3;128.0;;detectado", linhas[1]);
    }

    [Fact]
    public void Append_ContinuaOArquivoSemRepetirOCabecalho()
    {
        BpmCsvWriter.Append(Caminho, "a.mp3", 128);
        BpmCsvWriter.Append(Caminho, "b.mp3", 140);

        var linhas = File.ReadAllLines(Caminho);

        Assert.Equal(3, linhas.Length);
        Assert.Equal("b.mp3;140.0;;detectado", linhas[2]);
    }

    [Fact]
    public void Append_ArquivoSemQuebraDeLinhaNoFim_NaoGrudaAsLinhas()
    {
        File.WriteAllText(Caminho, "arquivo;bpm;tom;obs\nx.mp3;120;;");

        BpmCsvWriter.Append(Caminho, "y.mp3", 130);

        var linhas = File.ReadAllLines(Caminho);

        Assert.Equal(3, linhas.Length);
        Assert.Equal("x.mp3;120;;", linhas[1]);
        Assert.Equal("y.mp3;130.0;;detectado", linhas[2]);
    }

    [Fact]
    public void Append_O_Importador_LeOQueOWriterEscreveu()
    {
        BpmCsvWriter.Append(Caminho, "Blenk.mp3", 139.6);
        var track = new Track { FilePath = Path.Combine("x", "Blenk.mp3") };

        BpmCsvImporter.Apply(new[] { track }, Caminho);

        Assert.Equal(140, track.Bpm);
    }
}