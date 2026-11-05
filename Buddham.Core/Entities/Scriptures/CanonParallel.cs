using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 병행경 관계(상윳따 - 잡아함 등)
/// 구절 지정이 없으면 작품단위 관계
/// </summary>
public class CanonParallel : EntityBase
{
    public long FromWorkId { get; set; }
    public CanonWork FromWork { get; set; } = null!;

    public long ToWorkId { get; set; }
    public CanonWork ToWork { get; set; } = null!;

    public long? FromSegmentId { get; set; }
    public CanonSegment? FromSegment { get; set; }

    public long? ToSegmentId { get; set; }
    public CanonSegment? ToSegment { get; set; }

    public ParallelKind Kind { get; set; } = ParallelKind.Full;
    public string? Note { get; set; }
}
