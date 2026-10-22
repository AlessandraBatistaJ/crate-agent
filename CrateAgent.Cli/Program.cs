using System.Text;
using CrateAgent.Core.Models;
using CrateAgent.Core.Services;

Console.OutputEncoding = Encoding.UTF8;

// ===== Menu =====
Console.Write("1 = Organizar | 2 = Desfazer | 3 = Avaliar gabarito | 4 = Importar BPM | 5 = Montar set | 6 = Detectar BPM: ");
var opcao = Console.ReadLine()?.Trim();

if (opcao == "2")
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

// ===== Avaliar gabarito =====
if (opcao == "3")
{
    Console.Write("Caminho do gabarito.csv: ");
    var csv = Console.ReadLine()?.Trim('"', ' ');

    if (string.IsNullOrWhiteSpace(csv) || !File.Exists(csv))
    {
        Console.WriteLine("Gabarito não encontrado.");
        return;
    }

    var (rows, notFound) = GabaritoEvaluator.Evaluate(tracks, csv);
    var certos = rows.Count(linha => linha.Correct);

    Console.WriteLine();

    foreach (var erro in rows.Where(linha => !linha.Correct))
        Console.WriteLine($"[{erro.Original}]\n   esperado: {erro.Expected}\n   obtido:   {erro.Actual}\n");

    foreach (var ausente in notFound)
        Console.WriteLine($"Não encontrado na pasta: {ausente}");

    if (rows.Count == 0)
    {
        Console.WriteLine("Nenhuma faixa da pasta bateu com o gabarito. Confira a pasta e o CSV.");
        return;
    }

    Console.WriteLine($"Acertos: {certos}/{rows.Count} ({(double)certos / rows.Count:P0})");
    return;
}

// ===== Importar BPM e tom de um CSV =====
if (opcao == "4")
{
    Console.Write("Caminho do bpm.csv: ");
    var bpmCsv = Console.ReadLine()?.Trim('"', ' ');

    if (string.IsNullOrWhiteSpace(bpmCsv) || !File.Exists(bpmCsv))
    {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var aplicadas = BpmCsvImporter.Apply(tracks, bpmCsv);
    DuplicateDetector.ShareMetadata(DuplicateDetector.Find(tracks));

    Console.WriteLine($"\n{aplicadas} faixa(s) receberam BPM ou tom do CSV.\n");

    foreach (var faixaBpm in tracks.Where(x => x.Bpm is not null || !string.IsNullOrWhiteSpace(x.Key)))
        Console.WriteLine($"{Path.GetFileName(faixaBpm.FilePath)}  |  BPM: {faixaBpm.Bpm?.ToString() ?? "?"}  |  Tom: {faixaBpm.Key ?? "?"}");

    return;
}

// ===== Montar set =====
if (opcao == "5")
{
    Console.Write("Caminho do bpm.csv: ");
    var csvSet = Console.ReadLine()?.Trim('"', ' ');

    if (string.IsNullOrWhiteSpace(csvSet) || !File.Exists(csvSet))
    {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    BpmCsvImporter.Apply(tracks, csvSet);

    var gruposSet = DuplicateDetector.Find(tracks);
    DuplicateDetector.ShareMetadata(gruposSet);

    var copias = gruposSet
        .SelectMany(g => g.Duplicates)
        .Select(d => d.FilePath)
        .ToHashSet();
    var candidatas = tracks.Where(x => !copias.Contains(x.FilePath)).ToList();

    Console.WriteLine($"\n{candidatas.Count(x => x.Bpm is not null)} faixa(s) com BPM disponíveis (de {candidatas.Count}).");

    Console.Write("BPM inicial: ");
    var okInicio = int.TryParse(Console.ReadLine(), out var bpmInicio);
    Console.Write("BPM final: ");
    var okFim = int.TryParse(Console.ReadLine(), out var bpmFim);
    Console.Write("Número máximo de faixas: ");
    var okQtd = int.TryParse(Console.ReadLine(), out var maxFaixas);

    if (!okInicio || !okFim || !okQtd || bpmFim < bpmInicio)
    {
        Console.WriteLine("Valores inválidos.");
        return;
    }

    var pedido = new SetRequest(bpmInicio, bpmFim, maxFaixas);
    var sequencia = SetCurator.Build(candidatas, pedido);

    if (sequencia.Count == 0)
    {
        Console.WriteLine("Nenhuma faixa com BPM nessa faixa de valores.");
        return;
    }

    Console.WriteLine($"\n=== SET ({sequencia.Count} faixa(s)) ===\n");

    for (var i = 0; i < sequencia.Count; i++)
    {
        var item = sequencia[i];
        var anterior = i > 0 ? sequencia[i - 1] : null;
        var detalhe = "";

        if (anterior is not null)
        {
            var ranking = SetCurator.HarmonicRank(anterior.Key, item.Key);
            var harmonia = ranking == 0 ? "tom compatível" : ranking == 1 ? "tom desconhecido" : "tom incompatível";
            detalhe = $"   (+{item.Bpm - anterior.Bpm} BPM, {harmonia})";
        }

        Console.WriteLine($"{i + 1,2}. {item.Bpm} BPM | {item.Key ?? "?"} | {Path.GetFileName(item.FilePath)}{detalhe}");
    }

    if (sequencia.Count < maxFaixas)
        Console.WriteLine($"\nO set parou aqui: não há mais faixas dentro do salto de {pedido.MaxBpmJump} BPM.");

    return;
}

// ===== Detectar BPM e comparar com o CSV =====
if (opcao == "6")
{
    Console.Write("Caminho do bpm.csv (valores de referência): ");
    var csvDetect = Console.ReadLine()?.Trim('"', ' ');

    if (string.IsNullOrWhiteSpace(csvDetect) || !File.Exists(csvDetect))
    {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    BpmCsvImporter.Apply(tracks, csvDetect);

    var gruposDetect = DuplicateDetector.Find(tracks);
    DuplicateDetector.ShareMetadata(gruposDetect);

    var copiasDetect = gruposDetect
        .SelectMany(g => g.Duplicates)
        .Select(d => d.FilePath)
        .ToHashSet();
    var alvo = tracks.Where(x => x.Bpm is not null && !copiasDetect.Contains(x.FilePath)).ToList();

    Console.WriteLine($"\nDetectando o BPM de {alvo.Count} faixa(s)...\n");

    var contagem = new Dictionary<BpmVerdict, int>();
    var falhas = 0;

    foreach (var faixaAlvo in alvo)
    {
        var nome = Path.GetFileName(faixaAlvo.FilePath);

        if (!nome.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"PULOU   {nome} (só MP3 por enquanto)");
            falhas++;
            continue;
        }

        try
        {
            var cronometro = System.Diagnostics.Stopwatch.StartNew();
            var (amostras, taxa) = Mp3Decoder.ReadMono(faixaAlvo.FilePath);
            var detectado = BpmDetector.Detect(amostras, taxa);
            cronometro.Stop();

            if (detectado is null)
            {
                Console.WriteLine($"SEM RESULTADO   {nome}");
                falhas++;
                continue;
            }

            var esperado = faixaAlvo.Bpm!.Value;
            var valor = detectado.Value;
            var veredito = BpmDetector.Classify(valor, esperado);
            contagem[veredito] = contagem.GetValueOrDefault(veredito) + 1;

            Console.WriteLine($"{veredito,-6}  esperado {esperado,3}  |  detectado {valor,6:F1}  |  {cronometro.Elapsed.TotalSeconds:F1}s  |  {nome}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FALHOU  {nome}: {ex.Message}");
            falhas++;
        }
    }

    Console.WriteLine($"\nDentro de ±1 BPM: {contagem.GetValueOrDefault(BpmVerdict.Ok)}/{alvo.Count}");
    Console.WriteLine($"Dobro: {contagem.GetValueOrDefault(BpmVerdict.Double)} | Metade: {contagem.GetValueOrDefault(BpmVerdict.Half)} | " +
                      $"Outro: {contagem.GetValueOrDefault(BpmVerdict.Other)} | Sem resultado ou falha: {falhas}");
    return;
}

// ===== Listar faixas =====
Console.WriteLine($"\n{tracks.Count} faixas encontradas.");
Console.WriteLine($"{tracks.Count(x => !x.NeedsReview)} ok, {tracks.Count(x => x.NeedsReview)} para revisar.\n");

foreach (var t in tracks)
{
    var arquivo = Path.GetFileName(t.FilePath);
    var marca = t.NeedsReview
        ? $"  [REVISAR: {t.ReviewReasons}]"
        : t.ReviewReasons.HasFlag(ReviewReason.CatalogCode) ? "  (código de catálogo mantido)" : "";
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

// ===== Revisão manual =====
var pendentes = tracks.Where(x => x.NeedsReview).ToList();

if (pendentes.Count > 0)
{
    Console.Write($"\nRevisar agora as {pendentes.Count} faixas em revisão? (S/N): ");

    if (Console.ReadLine()?.Trim().ToUpper() == "S")
    {
        Console.WriteLine("Enter = pular | s = aprovar como está | 'Artista - Título' = corrigir | q = parar\n");

        var parar = false;

        foreach (var pendente in pendentes)
        {
            if (parar)
                break;

            Console.WriteLine($"[{Path.GetFileName(pendente.FilePath)}]");
            Console.WriteLine($"   atual: {pendente.Artist} - {pendente.Title}  ({pendente.ReviewReasons})");

            while (true)
            {
                Console.Write("   > ");
                var entrada = Console.ReadLine();

                if (entrada?.Trim().ToLower() == "q")
                {
                    parar = true;
                    break;
                }

                var decisao = ManualReview.Parse(entrada, pendente);

                if (decisao.Action == ReviewAction.Invalid)
                {
                    Console.WriteLine("   Entrada inválida. Sem artista, digite 'Artista - Título'.");
                    continue;
                }

                ManualReview.Apply(pendente, decisao);
                Console.WriteLine(decisao.Action == ReviewAction.Skip
                    ? "   (pulada)"
                    : $"   aprovada: {pendente.Artist} - {pendente.Title}");
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

var grupos = DuplicateDetector.Find(tracks);
var duplicatas = grupos.SelectMany(g => g.Duplicates).Select(d => d.FilePath).ToHashSet();

if (grupos.Count > 0)
{
    Console.WriteLine($"\n{grupos.Count} grupo(s) de duplicatas encontrado(s):");
    foreach (var g in grupos)
    {
        Console.WriteLine($"   fica:  {Path.GetFileName(g.Keep.FilePath)}");
        foreach (var d in g.Duplicates)
            Console.WriteLine($"   copia: {Path.GetFileName(d.FilePath)}");
    }
}

var plano = LibraryOrganizer.Plan(tracks, destino, duplicatas);

Console.WriteLine("\n=== SIMULAÇÃO (nada será movido) ===\n");

foreach (var move in plano)
{
    var origem = Path.GetFileName(move.Source);
    var relativo = Path.GetRelativePath(destino, move.Destination);
    Console.WriteLine($"{origem}\n   → {relativo}\n");
}

Console.WriteLine($"{plano.Count(m => !m.ToReview && !m.IsDuplicate)} seriam organizadas, " +
                  $"{plano.Count(m => m.ToReview)} iriam para _Revisar, " +
                  $"{plano.Count(m => m.IsDuplicate)} para _Duplicadas.");

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