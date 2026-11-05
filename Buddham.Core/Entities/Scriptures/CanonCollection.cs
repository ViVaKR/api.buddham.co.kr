using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Scripture;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 집성, 팔리의 Nikaya, 한역의 Agama 가 같은 층위 (Kind로 구분)
/// </summary>
public class CanonCollection : EntityBase, ICodedName
{
    public long DivisionId { get; set; }
    public CanonDivision Division { get; set; } = null!;

    // 귀속 부파, 확정되지 않았다면 null + TraditionCertainty로 표현
    public long? TraditionId { get; set; }
    public CanonTradition? Tradition { get; set; }
    public CertaintyLevel TraditionCertainty { get; set; } = CertaintyLevel.Unknown;
    public string? TraditionNote { get; set; }

    public CollectionKind? Kind { get; set; }

    // 예: DN, MN, SN, AN, KN / T01-DA, SA 등 내부키
    public string Code { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public string? NameOriginal { get; set; }
    public string? NameRoman { get; set; }
    public int SortOrder { get; set; }

    public ICollection<CanonGroup> Groups { get; set; } = [];
    public ICollection<CanonWork> Works { get; set; } = [];
}
