namespace Buddham.Core.Interfaces.Ollama;

public interface IOllamaService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct);
}
