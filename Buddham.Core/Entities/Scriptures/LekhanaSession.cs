using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 사경 한 회차
/// 한 판본을 처음 부터 끝까지 또는 일부 구간을 쓰는 단위
/// </summary>
public class LekhanaSession : EntityBase
{
    // 사용자 키(Guide.UserId와 동일한 Guid)
    public Guid UserId { get; set; }

    // 사경 대상 판본.
    // 원문 복사하지 않고 구절을 참조 함.
    public long EditionId { get; set; }
    public CanonEdition Edition { get; set; } = null!;
    public string? Title { get; set; }

    // 발원문
    public string? Vow { get; set; }
    public LekhanaStatus Status { get; set; } = LekhanaStatus.InProgress;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<LekhanaEntry> Entries { get; set; } = [];
}
