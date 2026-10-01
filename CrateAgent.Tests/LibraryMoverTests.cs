using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class LibraryMoverTests : IDisposable
{
    private readonly string _temp =
        Path.Combine(Path.GetTempPath(), "crateagent-tests-" + Guid.NewGuid());

    private string Origem => Path.Combine(_temp, "origem");
    private string Destino => Path.Combine(_temp, "destino");
    private string Log => Path.Combine(_temp, "log.json");

    public LibraryMoverTests()
    {
        Directory.CreateDirectory(Origem);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temp))
            Directory.Delete(_temp, true);
    }

    private List<PlannedMove> CriarPlano()
    {
        var arquivo = Path.Combine(Origem, "Blenk_-_Vibration.mp3");
        File.WriteAllText(arquivo, "conteudo");

        var track = new Track { FilePath = arquivo, Artist = "Blenk", Title = "Vibration" };
        return LibraryOrganizer.Plan(new[] { track }, Destino);
    }

    [Fact]
    public void Execute_MoveOArquivoParaODestino()
    {
        var plano = CriarPlano();

        var resultado = LibraryMover.Execute(plano, Log);

        Assert.Equal(1, resultado.Count);
        Assert.True(File.Exists(plano[0].Destination));
        Assert.False(File.Exists(plano[0].Source));
        Assert.True(File.Exists(Log));
    }

    [Fact]
    public void Execute_NaoSobrescreveArquivoExistente()
    {
        var plano = CriarPlano();
        Directory.CreateDirectory(Path.GetDirectoryName(plano[0].Destination)!);
        File.WriteAllText(plano[0].Destination, "ja existia");

        var resultado = LibraryMover.Execute(plano, Log);

        Assert.Equal(0, resultado.Count);
        Assert.Single(resultado.Skipped);
        Assert.True(File.Exists(plano[0].Source));
        Assert.Equal("ja existia", File.ReadAllText(plano[0].Destination));
    }

    [Fact]
    public void Undo_DevolveOArquivoAoLugarOriginal()
    {
        var plano = CriarPlano();
        LibraryMover.Execute(plano, Log);

        var resultado = LibraryMover.Undo(Log);

        Assert.Equal(1, resultado.Count);
        Assert.True(File.Exists(plano[0].Source));
        Assert.False(File.Exists(plano[0].Destination));
    }
}