using CrateAgent.Core.Models;
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

Console.WriteLine($"\n{tracks.Count} faixas encontradas.");
Console.WriteLine($"{tracks.Count(t => !t.NeedsReview)} ok, {tracks.Count(t => t.NeedsReview)} para revisar.\n");

foreach (var t in tracks)
{
    var arquivo = Path.GetFileName(t.FilePath);
    var marca = t.NeedsReview ? $"  [REVISAR: {t.ReviewReasons}]" : "";
    Console.WriteLine($"[{arquivo}] {t.Artist} - {t.Title}{marca}");
}

Console.Write("\nDigite a pasta de destino para simular a organização: ");
var destino = Console.ReadLine()?.Trim('"', ' ');

if (string.IsNullOrWhiteSpace(destino))
    return;

var plano = LibraryOrganizer.Plan(tracks, destino);

Console.WriteLine("\n=== SIMULAÇÃO (nada será movido) ===\n");

foreach (var move in plano)
{
    var origem = Path.GetFileName(move.Source);
    var relativo = Path.GetRelativePath(destino, move.Destination);
    Console.WriteLine($"{origem}\n   → {relativo}\n");
}

Console.WriteLine($"{plano.Count(m => !m.ToReview)} seriam organizadas, {plano.Count(m => m.ToReview)} iriam para _Revisar.");
