using System.Net;
using System.Text;
using System.Text.Json;
using CrateAgent.Core.Services;

namespace CrateAgent.Tests;

public class OllamaTrackSuggesterTests
{
    private class FakeHandler : HttpMessageHandler
    {
        private readonly string _body;
        public FakeHandler(string body) => _body = body;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public void ParseSuggestion_JsonValido_RetornaSugestao()
    {
        var json = """{"artist":"Danny Wabbit","title":"Suave","reason":"Removi sigla e código"}""";

        var s = OllamaTrackSuggester.ParseSuggestion(json);

        Assert.NotNull(s);
        Assert.Equal("Danny Wabbit", s!.Artist);
        Assert.Equal("Suave", s.Title);
    }

    [Theory]
    [InlineData("isto não é json")]
    [InlineData("""{"artist":"X","title":""}""")]
    [InlineData("")]
    public void ParseSuggestion_RespostaRuim_RetornaNull(string json)
    {
        Assert.Null(OllamaTrackSuggester.ParseSuggestion(json));
    }

    [Fact]
    public void ParseSuggestion_PassaPeloNameCleaner()
    {
        var json = """{"artist":"Premiere Setaoc Mass","title":"Sundogs"}""";

        var s = OllamaTrackSuggester.ParseSuggestion(json);

        Assert.Equal("Setaoc Mass", s!.Artist);
    }

    [Fact]
    public async Task SuggestAsync_LeARespostaDoOllama()
    {
        var inner = """{"artist":"Blenk","title":"Vibration","reason":"ok"}""";
        var body = JsonSerializer.Serialize(new { message = new { role = "assistant", content = inner } });
        var http = new HttpClient(new FakeHandler(body)) { BaseAddress = new Uri("http://localhost:11434") };
        var suggester = new OllamaTrackSuggester(http);

        var s = await suggester.SuggestAsync("Blenk_-_Vibration.mp3");

        Assert.Equal("Blenk", s!.Artist);
        Assert.Equal("Vibration", s.Title);
    }
}