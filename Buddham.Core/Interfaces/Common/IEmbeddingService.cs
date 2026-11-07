using Buddham.Core.Common.Results;

namespace Buddham.Core.Interfaces.Common;

public interface IEmbeddingService
{
    Task<(float[]? Vector, Result? Failure)> GenerateAsync(string text, CancellationToken ct);
}
