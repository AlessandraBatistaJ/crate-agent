namespace CrateAgent.Core.Services;

public enum BpmVerdict { Ok, Double, Half, Other }

public record BpmCandidate(double Bpm, double Score);

public static class BpmDetector
{
    private const double StartSeconds = 30;   // pula a introdução, se a faixa for longa
    private const double TailSeconds = 30;
    private const double MaxSeconds = 90;
    private const double CutoffHz = 150;      // só os graves (bumbo)
    private const int MinFrames = 500;        // menos de ~5 s não dá para medir
    private const double Step = 0.1;
    private const double MinPeakSeparation = 3.0;

    public static double? Detect(float[] samples, int sampleRate, double minBpm = 90, double maxBpm = 180)
    {
        var curve = ScoreCurve(samples, sampleRate, minBpm, maxBpm);

        if (curve is null)
            return null;

        var best = 0;

        for (var i = 1; i < curve.Length; i++)
        {
            if (curve[i].Score > curve[best].Score)
                best = i;
        }

        return Math.Round(curve[best].Bpm, 1);
    }

    // os melhores picos da curva de pontuação, separados por pelo menos 3 BPM
    public static IReadOnlyList<BpmCandidate> TopCandidates(
        float[] samples, int sampleRate, int count = 5, double minBpm = 90, double maxBpm = 180)
    {
        var curve = ScoreCurve(samples, sampleRate, minBpm, maxBpm);

        if (curve is null || count <= 0)
            return Array.Empty<BpmCandidate>();

        var peaks = new List<BpmCandidate>();

        for (var i = 0; i < curve.Length; i++)
        {
            var left = i == 0 ? double.MinValue : curve[i - 1].Score;
            var right = i == curve.Length - 1 ? double.MinValue : curve[i + 1].Score;

            if (curve[i].Score > left && curve[i].Score >= right)
                peaks.Add(curve[i]);
        }

        var selected = new List<BpmCandidate>();

        foreach (var peak in peaks.OrderByDescending(p => p.Score))
        {
            if (selected.All(s => Math.Abs(s.Bpm - peak.Bpm) >= MinPeakSeparation))
                selected.Add(peak);

            if (selected.Count == count)
                break;
        }

        return selected.Select(c => c with { Bpm = Math.Round(c.Bpm, 1) }).ToList();
    }

    public static BpmVerdict Classify(double detected, double expected)
    {
        if (Math.Abs(detected - expected) <= 1.0)
            return BpmVerdict.Ok;

        if (Math.Abs(detected - expected * 2) <= 2.0)
            return BpmVerdict.Double;

        if (Math.Abs(detected - expected / 2) <= 1.0)
            return BpmVerdict.Half;

        return BpmVerdict.Other;
    }

    private static BpmCandidate[]? ScoreCurve(float[] samples, int sampleRate, double minBpm, double maxBpm)
    {
        if (samples.Length == 0 || sampleRate <= 0 || maxBpm <= minBpm)
            return null;

        var total = samples.Length;
        var start = total > (StartSeconds + TailSeconds) * sampleRate ? (int)(StartSeconds * sampleRate) : 0;
        var length = Math.Min(total - start, (int)(MaxSeconds * sampleRate));

        var hop = Math.Max(1, sampleRate / 100);
        var frames = length / hop;

        if (frames < MinFrames)
            return null;

        var fps = (double)sampleRate / hop;

        // 1. graves -> energia por quadro (cerca de 100 quadros por segundo)
        var alpha = 1.0 - Math.Exp(-2.0 * Math.PI * CutoffHz / sampleRate);
        var rms = new double[frames];
        double low = 0;

        for (var f = 0; f < frames; f++)
        {
            var offset = start + f * hop;
            double sum = 0;

            for (var i = 0; i < hop; i++)
            {
                low += alpha * (samples[offset + i] - low);
                sum += low * low;
            }

            rms[f] = Math.Sqrt(sum / hop);
        }

        // 2. início das batidas: só os aumentos de energia
        var onset = new double[frames];
        double mean = 0;

        for (var f = 1; f < frames; f++)
        {
            onset[f] = Math.Max(0, rms[f] - rms[f - 1]);
            mean += onset[f];
        }

        mean /= frames;

        double energy = 0;

        for (var f = 0; f < frames; f++)
        {
            onset[f] -= mean;
            energy += onset[f] * onset[f];
        }

        if (energy <= 1e-12)
            return null;

        // 3. autocorrelação com defasagem fracionária (interpolação linear)
        double Correlation(double lag)
        {
            var whole = (int)Math.Floor(lag);
            var frac = lag - whole;

            if (whole + 1 >= frames)
                return 0;

            double sum = 0;

            for (var n = 0; n < frames - whole - 1; n++)
                sum += onset[n] * ((1 - frac) * onset[n + whole] + frac * onset[n + whole + 1]);

            return sum / energy;
        }

        // 4. para cada BPM candidato, soma a correlação em 1, 2, 3 e 4 tempos
        var steps = (int)Math.Round((maxBpm - minBpm) / Step);

        if (steps <= 0)
            return null;

        var curve = new BpmCandidate[steps];

        for (var s = 0; s < steps; s++)
        {
            var bpm = minBpm + s * Step;
            var period = 60.0 * fps / bpm;
            double score = 0;

            for (var k = 1; k <= 4; k++)
                score += Correlation(k * period);

            curve[s] = new BpmCandidate(bpm, score);
        }

        return curve;
    }
}