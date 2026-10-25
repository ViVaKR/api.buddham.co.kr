using Buddham.Core.Common.Options;

namespace Buddham.Core.Entities.Public;

public record VideoJob
{
    public string JobId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string RawPath { get; init; } = string.Empty;
    public string BasePath { get; init; } = string.Empty;
    public VideoProcessOptions Options { get; init; } = new();
}
