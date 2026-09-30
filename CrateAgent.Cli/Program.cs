using System.Text;
using CrateAgent.Core.Services;

Console.OutputEncoding = Encoding.UTF8;

Console.Write("Digite o caminho da pasta de músicas: ");
var folder = Console.ReadLine()?.Trim('"', ' ');

if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
{
    Console.WriteLine("Pasta não encontrada.");
    return;
}

var scanner = new LibraryScanner();
var tracks = scanner.Scan(folder).ToList();

Console.WriteLine($"\n{tracks.Count} faixas encontradas:\n");

foreach (var t in tracks)
{
    var bpm = t.Bpm?.ToString() ?? "?";
    var arquivo = Path.GetFileName(t.FilePath);
    Console.WriteLine($"[{arquivo}] {t.Artist} - {t.Title} | {t.Album} | BPM: {bpm}");
}