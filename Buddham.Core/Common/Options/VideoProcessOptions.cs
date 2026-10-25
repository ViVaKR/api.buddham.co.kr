using Buddham.Core.EnumTypes;

namespace Buddham.Core.Common.Options;

public record VideoProcessOptions
{
    public bool GenerateThumbnail { get; init; } = true;
    public bool TranscodeToHls { get; init; } = true; // HLS 스트리밍용
    public bool TranscodeToMp4 { get; init; } = true; // 호환성 MP4
    public int ThumbnailAtSecond { get; init; } = 3; // 썸네일 추출 시점
    public VideoQuality Quality { get; init; } = VideoQuality.High;
}
