using NLayer;

namespace CrateAgent.Core.Services;

public static class Mp3Decoder
{
    // lê os primeiros segundos do MP3 e devolve o áudio em mono
    public static (float[] Samples, int SampleRate) ReadMono(string path, double maxSeconds = 120)
    {
        using var mpeg = new MpegFile(path);

        var sampleRate = mpeg.SampleRate;
        var channels = mpeg.Channels;
        var maxFrames = (int)(sampleRate * maxSeconds);

        var mono = new float[maxFrames];
        var buffer = new float[4096 * channels];
        var written = 0;

        while (written < maxFrames)
        {
            var read = mpeg.ReadSamples(buffer, 0, buffer.Length);
            if (read <= 0)
                break;

            var frames = read / channels;

            for (var f = 0; f < frames && written < maxFrames; f++)
            {
                float sum = 0;

                for (var c = 0; c < channels; c++)
                    sum += buffer[f * channels + c];

                mono[written++] = sum / channels;
            }
        }

        Array.Resize(ref mono, written);
        return (mono, sampleRate);
    }
}