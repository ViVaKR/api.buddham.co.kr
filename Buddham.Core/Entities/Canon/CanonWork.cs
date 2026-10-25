using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Canon;

/// <summary>
/// 원전(原典) — 대장경 총서(Collection) 내 개별 경전의 마스터 레코드.
/// 번역/해설 등 실제 콘텐츠(Pitaka 등)가 참조하는 근본 뿌리.
/// </summary>
public class CanonWork : EntityBase
{
    public long CollectionId { get; set; }
    public string? CanonicalNumber { get; set; } // K-20, T-251 등 표준 코드
    public required string TitleKo { get; set; }
    public string? TitleOriginal { get; set; } // 한자 / 산스크리트 / 빠알리
    public string? TitleRomanized { get; set; }
    public string? TranslatorOrAuthor { get; set; } // 역자 / 찬자 (구마라집, 현장 등)
    public string? Era { get; set; } // 시대 (요진, 당 등)
    public string? Description { get; set; }

    public long? ParentId { get; set; } // 다권본 자기참조 (화엄경 80권본 등)
    public int? VolumeNumber { get; set; } // 권 번호 (권1, 권2 ...)
    public int SortOrder { get; set; }

    public virtual CanonCollection Collection { get; set; } = null!;
    public virtual CanonWork? Parent { get; set; }
    public virtual ICollection<CanonWork> Children { get; set; } = [];
}


/*
 * Canon Work 의미
 * 원전(原典) — "오리지널 원전 등"이라고 직접 썼던 표현이라, 지금 설계의 정신과도 정확히 맞아떨어짐
 * "이 사경이 어느 원전에서 나왔는가" 같은 문장도 자연스러움
 */