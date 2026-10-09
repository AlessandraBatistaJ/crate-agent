using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class DuplicateDetectorTests : IDisposable
{
    private readonly string _temp =
        Path.Combine(Path.GetTempPath(), "crateagent-dup-" + Guid.NewGuid());

    public DuplicateDetectorTests()
    {
        Directory.CreateDirectory(_temp);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temp))
            Directory.Delete(_temp, true);
    }

    private Track Criar(string nome, string conteudo)
    {
        var caminho = Path.Combine(_temp, nome);
        File.WriteAllText(caminho, conteudo);
        return new Track { FilePath = caminho };
    }

    [Fact]
    public void Find_ArquivosIdenticos_MantemOQueNaoTemMarcador()
    {
        var comMarcador = Criar("Musica (1).mp3", "conteudo igual");
        var original = Criar("Musica.mp3", "conteudo igual");

        var grupos = DuplicateDetector.Find(new[] { comMarcador, original });

        Assert.Single(grupos);
        Assert.Equal(original.FilePath, grupos[0].Keep.FilePath);
        Assert.Equal(comMarcador.FilePath, Assert.Single(grupos[0].Duplicates).FilePath);
    }

    [Fact]
    public void Find_MesmoTamanhoMasConteudoDiferente_NaoEDuplicata()
    {
        var a = Criar("a.mp3", "aaaa");
        var b = Criar("b.mp3", "bbbb");

        Assert.Empty(DuplicateDetector.Find(new[] { a, b }));
    }

    [Fact]
    public void Find_ArquivosDiferentes_RetornaVazio()
    {
        var a = Criar("a.mp3", "um conteudo");
        var b = Criar("b.mp3", "outro conteudo bem maior");

        Assert.Empty(DuplicateDetector.Find(new[] { a, b }));
    }

    [Fact]
    public void Find_TresCopias_MantemUmEMarcaDuas()
    {
        var a = Criar("Faixa.mp3", "x");
        var b = Criar("Faixa (1).mp3", "x");
        var c = Criar("Faixa (2).mp3", "x");

        var grupos = DuplicateDetector.Find(new[] { c, b, a });

        Assert.Single(grupos);
        Assert.Equal(a.FilePath, grupos[0].Keep.FilePath);
        Assert.Equal(2, grupos[0].Duplicates.Count);
    }
}