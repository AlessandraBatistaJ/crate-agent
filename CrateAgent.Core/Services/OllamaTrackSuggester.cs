using System.Net.Http.Json;
using System.Text.Json;
using CrateAgent.Core.Models;

namespace CrateAgent.Core.Services;

public class OllamaTrackSuggester
{
    private const string SystemPrompt = """
        Você extrai artista e título de nomes de arquivos de música.
        Responda SOMENTE com JSON no formato: {"artist": "", "title": "", "reason": ""}
        Regras:
        - Use apenas informações presentes no nome do arquivo.
        - Nunca invente nem use conhecimento externo para descobrir o artista.
        - Se o nome não contém o artista, deixe "artist" vazio.
        - Remova códigos de catálogo (ex.: MR039, ONI-Y002), a palavra Premiere e posições de vinil (ex.: A1).
        - Se uma sigla curta parecer prefixo de canal ou selo (ex.: DS, VD), remova e explique em "reason".
        - "reason" deve ter uma frase curta explicando o que você fez.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaTrackSuggester(HttpClient http, string model = "llama3.2")
    {
        _http = http;
        _model = model;
    }

    public async Task<TrackSuggestion?> SuggestAsync(string fileName, CancellationToken ct = default)
    {
        var request = new
        {
            model = _model,
            stream = false,
            format = "json",
            options = new { temperature = 0 },
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Nome do arquivo: {fileName}" }
            }
        };

        using var response = await _http.PostAsJsonAsync("/api/chat", request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
        return ParseSuggestion(body?.Message?.Content);
    }

    public static TrackSuggestion? ParseSuggestion(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var raw = JsonSerializer.Deserialize<RawSuggestion>(json, JsonOptions);
            if (raw is null || string.IsNullOrWhiteSpace(raw.Title))
                return null;

            return new TrackSuggestion(
                NameCleaner.Clean(raw.Artist ?? string.Empty),
                NameCleaner.Clean(raw.Title),
                raw.Reason?.Trim() ?? string.Empty);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record RawSuggestion(string? Artist, string? Title, string? Reason);
    private sealed record OllamaMessage(string? Content);
    private sealed record OllamaChatResponse(OllamaMessage? Message);
}