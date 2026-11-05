using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 작품의 실제 텍스트 판본 (언어•출전별).
/// 원문과 번역을 같은 구조로 담되 Kind 로 구분.
/// </summary>
public class CanonEdition : EntityBase
{
    public long WorkId { get; set; }
    public CanonWork Work { get; set; } = null!;

    public long? SourceId { get; set; }
    public CanonSource? Source { get; set; }

    public EditionKind Kind { get; set; }
    public EditionStatus Status { get; set; } = EditionStatus.Draft;

    // BCP-47 계열: pi, lzh, sa, bo, ko, en
    public string LanguageCode { get; set; } = string.Empty;

    // 문자: Latn, Deva, Hant, Hang 등
    public string? Script { get; set; }

    public string? TranslatorName { get; set; }

    // 번역일 때 어느 원문 판본을 저본으로 했는지
    public long? BasedOnEditionId { get; set; }
    public CanonEdition? BasedOn { get; set; }

    public string? Revision { get; set; }

    // 구절 전체를 순서대로 이은 SHA-256. 원문 무결성 검증용.
    public string? ContentHash { get; set; }
    public int SegmentCount { get; set; }

    public ICollection<CanonEdition> Translations { get; set; } = [];
    public ICollection<CanonSegment> Segments { get; set; } = [];
}
