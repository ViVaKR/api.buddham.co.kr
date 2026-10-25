using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Buddham.Core.Interfaces.Ollama;
using Microsoft.Extensions.Configuration;

namespace Buddham.Shared.Services.OllamaService;

public class OllamaService(HttpClient httpClient, IConfiguration config) : IOllamaService
{
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct)
    {
        var model = config["Ollama:Model"] ?? "bge-m3";
        var response = await httpClient.PostAsJsonAsync("api/embed", new
        {
            model,
            input = text
        }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken: ct);
        return result?.Embeddings[0] ?? throw new Exception("임베딩 생성 실패");
    }
}

public record OllamaEmbedResponse(
    [property: JsonPropertyName("model")]
    string Model,
    [property: JsonPropertyName("embeddings")]
    List<float[]> Embeddings);
