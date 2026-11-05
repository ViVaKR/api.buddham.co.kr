using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Scripture;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 삼장 또는 부, 경장/율장/논장, 아함부/반야부/법화부 등
/// </summary>
public class CanonDivision : EntityBase, ICodedName
{
    public long CanonId { get; set; }
    public Canon Canon { get; set; } = null!;

    public long? ParentId { get; set; }
    public CanonDivision? Parent { get; set; }

    public DivisionKind Kind { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public string? NameOriginal { get; set; }
    public string? NameRoman { get; set; }
    public int SortOrder { get; set; }

    public ICollection<CanonDivision> Children { get; set; } = [];
    public ICollection<CanonCollection> Collections { get; set; } = [];
}
