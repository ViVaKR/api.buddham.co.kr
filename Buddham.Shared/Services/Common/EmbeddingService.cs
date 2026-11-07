using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces.Common;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Services.Common;

public class EmbeddingService(
    IOllamaService ollamaService,
    ILogger<EmbeddingService> logger) : IEmbeddingService
{
    public async Task<(float[]? Vector, Result? Failure)> GenerateAsync(
        string text, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        try
        {
            var vector = await ollamaService.GetEmbeddingAsync(text, ct);
            if (vector is null || vector.Length != AppInfrastructure.EmbeddingDimensions)
            {
                logger.LogError(
                "Embedding dimension mismatch. Expected: {Expected}, Actual: {Actual}",
                AppInfrastructure.EmbeddingDimensions,
                vector?.Length ?? 0);

                return (null, Result.Failure(
                    RsCode.Error,
                    "임베딩 설정 오류가 발생했습니다."));
            }
            return (vector, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Embedding generation failed.");

            return (null, Result.Failure(RsCode.Error,
                "임베딩 서비스를 사용할 수 없습니다. 잠시 후 다시 시도해주세요."));
        }


    }
}
