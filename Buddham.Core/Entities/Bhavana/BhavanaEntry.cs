using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Bhavana;

public class BhavanaEntry : EntityBase
{
    public Guid UserId { get; set; }
    public string? AuthorPseudonym { get; set; }

    // 스칼라 참조만 (인드라망 원칙 — Pitaka를 소유하지 않음, FK 제약 없음)
    public long? PitakaScriptureId { get; set; }

    // 원문 스냅샷 — 사경 시작 시점에 고정, 이후 원문이 바뀌어도 이 기록은 불변
    public required string SourceTitle { get; set; }
    public required string SourceContent { get; set; }

    // 실제 타이핑한 내용 (누적, 덮어쓰기 저장)
    public string TypedContent { get; set; } = string.Empty;

    public int TotalLength { get; set; } // 원문 총 글자수 (시작 시점 계산)
    public int TypedLength { get; set; } // 현재까지 입력한 글자수

    public BhavanaStatus Status { get; set; } = BhavanaStatus.InProgress;

    public bool IsPublic { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? LastPracticedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}