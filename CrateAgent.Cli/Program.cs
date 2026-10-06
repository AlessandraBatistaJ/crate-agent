using System.Text;
using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

Console.OutputEncoding = Encoding.UTF8;

// ===== Menu: organizar ou desfazer =====
Console.Write("1 = Organizar uma pasta | 2 = Desfazer usando um log: ");

if (Console.ReadLine()?.Trim() == "2")
{
    Console.Write("Caminho do arquivo de log: ");
    var logDesfazer = Console.ReadLine()?.Trim('"', ' ');

    if (string.IsNullOrWhiteSpace(logDesfazer) || !File.Exists(logDesfazer))
    {
        Console.WriteLine("Log não encontrado.");
        return;
    }

    var desfeito = LibraryMover.Undo(logDesfazer);
    Console.WriteLine($"{desfeito.Count} arquivos restaurados, {desfeito.Skipped.Count} ignorados.");
    return;
}

// ===== Escanear a pasta de músicas =====
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
Console.WriteLine($"{tracks.Count(x => !x.NeedsReview)} ok, {tracks.Count(x => x.NeedsReview)} para revisar.\n");

foreach (var t in tracks)
{
    var arquivo = Path.GetFileName(t.FilePath);
    var marca = t.NeedsReview ? $"  [REVISAR: {t.ReviewReasons}]" : "";
    Console.WriteLine($"[{arquivo}] {t.Artist} - {t.Title}{marca}");
}

// ===== Sugestões da IA (só para faixas sem artista) =====
var semArtista = tracks
    .Where(x => x.ReviewReasons.HasFlag(ReviewReason.MissingArtist))
    .ToList();

if (semArtista.Count > 0)
{
    Console.Write($"\nPedir sugestões da IA para as {semArtista.Count} faixas sem artista? (S/N): ");

    if (Console.ReadLine()?.Trim().ToUpper() == "S")
    {
        using var http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = TimeSpan.FromMinutes(2)
        };
        var suggester = new OllamaTrackSuggester(http);

        foreach (var faixa in semArtista)
        {
            var nomeArquivo = Path.GetFileName(faixa.FilePath);
            try
            {
                var s = await suggester.SuggestAsync(nomeArquivo);
                Console.WriteLine($"\n{nomeArquivo}");

                if (s is null)
                {
                    Console.WriteLine("   → (sem sugestão válida)");
                }
                else
                {
                    var veredito = SuggestionGuard.Validate(nomeArquivo, s);
                    var status = veredito.Accepted ? "ACEITA" : $"REJEITADA: {veredito.Reason}";
                    Console.WriteLine($"   → Artista: \"{s.Artist}\" | Título: \"{s.Title}\"");
                    Console.WriteLine($"   [{status}]");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Não consegui falar com o Ollama. Ele está aberto?");
                break;
            }
        }
    }
}

// ===== Simulação (dry-run) =====
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

// ===== Mover de verdade (com confirmação) =====
Console.Write("\nMover os arquivos de verdade? Digite SIM para confirmar: ");

if (Console.ReadLine()?.Trim() != "SIM")
{
    Console.WriteLine("Nada foi movido.");
    return;
}

var logPath = Path.Combine(destino, $"crateagent-log-{DateTime.Now:yyyyMMdd-HHmmss}.json");
var resultado = LibraryMover.Execute(plano, logPath);

Console.WriteLine($"\n{resultado.Count} arquivos movidos, {resultado.Skipped.Count} ignorados.");
Console.WriteLine($"Log salvo em: {logPath}");