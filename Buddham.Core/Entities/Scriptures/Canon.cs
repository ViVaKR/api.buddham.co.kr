using Buddham.Core.Common.Primitives;
using Buddham.Core.Interfaces.Scripture;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 경전 집합
/// 팔리 띠삐따까, 대정신수대장경, 고려대장경, 칸큐르 등
/// 단일 경전을 가정하지 않음
/// </summary>
public class Canon : EntityBase, ICodedName
{
    // 예: pali-tipitaka, taisho, koryo, kangyur
    public string Code { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public string? NameOriginal { get; set; }
    public string? NameRoman { get; set; }
    // 주 언어(BCP-47 계열: pi, lzh, sa, bo ...)
    public string? PrimaryLanguage { get; set; }
    // 단일 전통에 속할 때만 지정(팔이 띠삐따까 -> 상좌부)
    // 대정장처럼 여러 부파가 섞이면 null
    public long? TraditionId { get; set; }
    public CanonTradition? Tradition { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public ICollection<CanonDivision> Divisions { get; set; } = [];
}
