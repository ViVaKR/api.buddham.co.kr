namespace Buddham.Core.Common.Results;

public record VideoProcessResult
{
    public string RawPath { get; init; } = ""; // 원본 저장 경로
    public string JobId { get; init; } = ""; // 백그라운드 잡 ID
}