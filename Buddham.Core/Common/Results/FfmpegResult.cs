namespace Buddham.Core.Common.Results;

public record FfmpegResult
{
    public string? Mp4RelPath { get; init; }
    public string? ThumbnailRelPath { get; init; }
    public string? HlsRelPath { get; init; }
}