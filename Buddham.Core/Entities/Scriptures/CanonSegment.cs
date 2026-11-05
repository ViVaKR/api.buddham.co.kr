using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 주소 가능한 최소 텍스트 단위(문단•게송•행)
/// </summary>
public class CanonSegment : EntityBase
{
    public long EditionId { get; set; }
    public CanonEdition Edition { get; set; } = null!;

    // 안정 주소(예: sn56.11:3.2). 같은 원전을 번역한 판본끼리는 같은 값을 써서 대역을 맞춥니다.
    // 언어 간 구절 분할이 다른 병행경은 CanonParallel로 연결합니다.
    public string SegmentRef { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public SegmentKind Kind { get; set; } = SegmentKind.Prose;

    // 원문 그대로의 텍스트. 원문 판본에서는 수정하지 않는 불변 층.
    public string Text { get; set; } = string.Empty;

    // 검색용 정규화 텍스트(발음부호 제거 등)
    // 원문 보존과 분리
    public string? TextNormalized { get; set; }

    // 원전 쪽수, 행 표기.
    // 예: PTS M i 55, T01n0001_p0001a01
    public string? PageRef { get; set; }

    // Text의 SHA-256. 변경감지와 재임베딩 판단용
    public string? TextHash { get; set; }

    // bge-m3 임베딩(1024차원)
    public float[]? Embedding { get; set; }
    public string? EmbeddingModel { get; set; }
    public DateTime? EmbeddedAt { get; set; }

    public ICollection<CanonSegmentTopic> Topics { get; set; } = [];
    public ICollection<LekhanaEntry> LekhanaEntries { get; set; } = [];

}
