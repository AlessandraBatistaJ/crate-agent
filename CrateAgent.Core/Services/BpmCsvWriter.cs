using System.Text;

namespace CrateAgent.Core.Services;

public static class BpmCsvWriter
{
    private const string Header = "arquivo;bpm;tom;obs";

    // null quando o nome não cabe no formato (ponto e vírgula ou quebra de linha quebrariam o CSV)
    public static string? Format(string fileName, double bpm)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Contains(';') || fileName.Contains('\n') || fileName.Contains('\r'))
            return null;

        return FormattableString.Invariant($"{fileName};{bpm:F1};;detectado");
    }

    public static bool Append(string csvPath, string fileName, double bpm)
    {
        var row = Format(fileName, bpm);

        if (row is null)
            return false;

        var directory = Path.GetDirectoryName(Path.GetFullPath(csvPath));

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var isNew = !File.Exists(csvPath) || new FileInfo(csvPath).Length == 0;
        var needsLeadingNewline = false;

        if (!isNew)
        {
            using var stream = File.OpenRead(csvPath);
            stream.Seek(-1, SeekOrigin.End);
            needsLeadingNewline = stream.ReadByte() != '\n';
        }

        var text = new StringBuilder();

        if (isNew)
            text.AppendLine(Header);
        else if (needsLeadingNewline)
            text.AppendLine();

        text.AppendLine(row);

        File.AppendAllText(csvPath, text.ToString(), new UTF8Encoding(false));
        return true;
    }
}