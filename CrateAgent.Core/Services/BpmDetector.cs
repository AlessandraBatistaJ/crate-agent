namespace CrateAgent.Core.Services;

public enum BpmVerdict { Ok, Double, Half, Other }

public static class BpmDetector
{
    private const double StartSeconds = 30;   // pula a introdução, se a faixa for longa
    private const double TailSeconds = 30;
    private const double MaxSeconds = 90;
    private const double CutoffHz = 150;      // só os graves (bumbo)
    private const int MinFrames = 500;        // menos de ~5 s não dá para medir
    private const double Step = 0.1;

    public static double? Detect(float[] samples, int sampleRate, double minBpm = 90, double maxBpm = 180)
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
        var bestBpm = minBpm;
        var bestScore = double.MinValue;

        for (var s = 0; s < steps; s++)
        {
            var bpm = minBpm + s * Step;
            var period = 60.0 * fps / bpm;
            double score = 0;

            for (var k = 1; k <= 4; k++)
                score += Correlation(k * period);

            if (score > bestScore)
            {
                bestScore = score;
                bestBpm = bpm;
            }
        }

        return Math.Round(bestBpm, 1);
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
}