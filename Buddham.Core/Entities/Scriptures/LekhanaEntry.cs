using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 사경 한 구절의 기록
/// </summary>
public class LekhanaEntry : EntityBase
{
    public long SessionId { get; set; }
    public LekhanaSession Session { get; set; } = null!;

    public long SegmentId { get; set; }
    public CanonSegment Segment { get; set; } = null!;

    public int SortOrder { get; set; }
    public DateTime? CopiedAt { get; set; }

    // 사용자가 직접 입력한 텍스트 (원문 대조용)
    public string? TypedText { get; set; }

    // 손글씨 사진 등 첨부 경로
    public string? ImageUrl { get; set; }
}
