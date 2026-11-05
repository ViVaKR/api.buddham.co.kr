using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 구절과 교리 주제의 연결
/// 사람이 단 태그와 AI가 단 태그를 구분
/// </summary>
public class CanonSegmentTopic : EntityBase
{
    public long SegmentId { get; set; }
    public CanonSegment Segment { get; set; } = null!;

    public long TopicId { get; set; }
    public DhammaTopic Topic { get; set; } = null!;

    public TagSource Source { get; set; } = TagSource.Manual;
    public double? Confidence { get; set; }
    public string? Note { get; set; }
}
