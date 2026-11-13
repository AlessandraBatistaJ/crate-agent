using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class BpmDetectorTests
{
    private const int SampleRate = 22050;

    // faixa sintética: bumbo em cada tempo, chimbal no contratempo e um pouco de ruído
    private static float[] ClickTrack(double bpm, int seconds = 40, int seed = 1)
    {
        var rng = new Random(seed);
        var samples = new float[SampleRate * seconds];
        var beat = 60.0 / bpm;
        var kickLength = (int)(0.12 * SampleRate);
        var hatLength = (int)(0.03 * SampleRate);

        for (var b = 0; ; b++)
        {
            var start = (int)Math.Round(b * beat * SampleRate);
            if (start + kickLength >= samples.Length)
                break;

            for (var i = 0; i < kickLength; i++)
            {
                var t = (double)i / SampleRate;
                samples[start + i] += (float)(Math.Sin(2 * Math.PI * (55 + 90 * Math.Exp(-t * 40)) * t) * Math.Exp(-t * 28));
            }

            var hatStart = (int)Math.Round((b + 0.5) * beat * SampleRate);
            if (hatStart + hatLength >= samples.Length)
                continue;

            for (var i = 0; i < hatLength; i++)
                samples[hatStart + i] += (float)((rng.NextDouble() * 2 - 1) * Math.Exp(-i / (0.006 * SampleRate)) * 0.4);
        }

        for (var i = 0; i < samples.Length; i++)
            samples[i] += (float)((rng.NextDouble() * 2 - 1) * 0.08);

        return samples;
    }

    // só bumbo, em posições dadas (em tempos) dentro de cada compasso, mais um pouco de ruído
    private static float[] PatternTrack(double bpm, double[] kicksInBar, double barBeats = 4, int seconds = 40, int seed = 1)
    {
        var rng = new Random(seed);
        var samples = new float[SampleRate * seconds];
        var beat = 60.0 / bpm;
        var kickLength = (int)(0.12 * SampleRate);
        var finished = false;

        for (var bar = 0; !finished; bar++)
        {
            foreach (var position in kicksInBar)
            {
                var start = (int)Math.Round((bar * barBeats + position) * beat * SampleRate);

                if (start + kickLength >= samples.Length)
                {
                    finished = true;
                    break;
                }

                for (var i = 0; i < kickLength; i++)
                {
                    var t = (double)i / SampleRate;
                    samples[start + i] += (float)(Math.Sin(2 * Math.PI * (55 + 90 * Math.Exp(-t * 40)) * t) * Math.Exp(-t * 28));
                }
            }
        }

        for (var i = 0; i < samples.Length; i++)
            samples[i] += (float)((rng.NextDouble() * 2 - 1) * 0.08);

        return samples;
    }

    [Theory]
    [InlineData(120)]
    [InlineData(128)]
    [InlineData(140)]
    [InlineData(146)]
    public void Detect_FaixaSintetica_FicaDentroDeUmBpm(double bpm)
    {
        var detectado = BpmDetector.Detect(ClickTrack(bpm), SampleRate);

        Assert.NotNull(detectado);
        Assert.InRange(detectado!.Value, bpm - 1.0, bpm + 1.0);
    }

    [Fact]
    public void Detect_TempoAbaixoDaJanela_DobraParaDentroDela()
    {
        var detectado = BpmDetector.Detect(ClickTrack(70), SampleRate);

        Assert.NotNull(detectado);
        Assert.InRange(detectado!.Value, 139.0, 141.0);
    }

    [Fact]
    public void Detect_Silencio_RetornaNull()
    {
        Assert.Null(BpmDetector.Detect(new float[SampleRate * 40], SampleRate));
    }

    [Fact]
    public void Detect_AudioCurto_RetornaNull()
    {
        Assert.Null(BpmDetector.Detect(ClickTrack(128, seconds: 1), SampleRate));
    }

    [Theory]
    [InlineData(140, 140, BpmVerdict.Ok)]
    [InlineData(139.6, 140, BpmVerdict.Ok)]
    [InlineData(141.2, 140, BpmVerdict.Other)]
    [InlineData(140, 70, BpmVerdict.Double)]
    [InlineData(70, 140, BpmVerdict.Half)]
    [InlineData(100, 140, BpmVerdict.Other)]
    public void Classify_SeparaOkDobroMetadeEOutro(double detectado, double esperado, BpmVerdict veredito)
    {
        Assert.Equal(veredito, BpmDetector.Classify(detectado, esperado));
    }

    [Fact]
    public void TopCandidates_PrimeiroEIgualAoDetect()
    {
        var faixa = ClickTrack(128);

        var candidatos = BpmDetector.TopCandidates(faixa, SampleRate);

        Assert.NotEmpty(candidatos);
        Assert.Equal(BpmDetector.Detect(faixa, SampleRate)!.Value, candidatos[0].Bpm);
    }

    [Fact]
    public void TopCandidates_SaoOrdenadosPorPontuacaoESeparados()
    {
        var candidatos = BpmDetector.TopCandidates(ClickTrack(140), SampleRate, count: 5);

        Assert.InRange(candidatos.Count, 2, 5);

        for (var i = 1; i < candidatos.Count; i++)
            Assert.True(candidatos[i].Score <= candidatos[i - 1].Score);

        for (var i = 0; i < candidatos.Count; i++)
        {
            for (var j = i + 1; j < candidatos.Count; j++)
                Assert.True(Math.Abs(candidatos[i].Bpm - candidatos[j].Bpm) >= 2.9);
        }
    }

    [Fact]
    public void TopCandidates_SemSinal_RetornaVazio()
    {
        Assert.Empty(BpmDetector.TopCandidates(new float[SampleRate * 40], SampleRate));
    }

    [Fact]
    public void Analyze_SemSinal_RetornaNull()
    {
        Assert.Null(BpmDetector.Analyze(new float[SampleRate * 40], SampleRate));
    }

    [Fact]
    public void Analyze_VencedorEIgualAoDetect()
    {
        var faixa = ClickTrack(128);

        var analise = BpmDetector.Analyze(faixa, SampleRate);

        Assert.NotNull(analise);
        Assert.Equal(BpmDetector.Detect(faixa, SampleRate)!.Value, analise!.Bpm);
    }

    [Fact]
    public void Analyze_QuatroNoChao_TemVencedorClaro()
    {
        var analise = BpmDetector.Analyze(PatternTrack(139, new[] { 0.0, 1.0, 2.0, 3.0 }), SampleRate);

        Assert.NotNull(analise);
        Assert.InRange(analise!.Bpm, 138.0, 140.0);
        Assert.NotNull(analise.AlternativeBpm);
        Assert.True(analise.AlternativeRatio < 0.7);
    }

    [Fact]
    public void Analyze_PadraoSincopado_TemAndamentoAmbiguo()
    {
        // bumbo nas posições 0 e 2,5 de cada compasso de 4 tempos (estilo 2-step)
        var analise = BpmDetector.Analyze(PatternTrack(139, new[] { 0.0, 2.5 }), SampleRate);

        Assert.NotNull(analise);
        Assert.NotNull(analise!.AlternativeBpm);
        Assert.True(analise.AlternativeRatio > 0.9);

        var par = new[] { analise.Bpm, analise.AlternativeBpm!.Value };

        Assert.Contains(par, b => Math.Abs(b - 139) <= 1.5);
        Assert.Contains(par, b => Math.Abs(b - 104.3) <= 1.5);
    }

    [Theory]
    [InlineData(0.88, true)]
    [InlineData(0.85, true)]
    [InlineData(0.84, false)]
    public void IsAmbiguous_UsaOLimiteDe85Porcento(double razao, bool esperado)
    {
        var analise = new BpmAnalysis(139, 92.7, razao);

        Assert.Equal(esperado, BpmDetector.IsAmbiguous(analise));
    }

    [Fact]
    public void IsAmbiguous_SemAlternativa_NaoEAmbiguo()
    {
        Assert.False(BpmDetector.IsAmbiguous(new BpmAnalysis(139, null, 0.99)));
    }

    [Fact]
    public void IsAmbiguous_PadraoSincopadoSim_QuatroNoChaoNao()
    {
        var sincopado = BpmDetector.Analyze(PatternTrack(139, new[] { 0.0, 2.5 }), SampleRate)!;
        var reto = BpmDetector.Analyze(PatternTrack(139, new[] { 0.0, 1.0, 2.0, 3.0 }), SampleRate)!;

        Assert.True(BpmDetector.IsAmbiguous(sincopado));
        Assert.False(BpmDetector.IsAmbiguous(reto));
    }
}