using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Scripture;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 집성 아래 묶음(품, 상윳따, 니빠따, 권)
/// 가변 깊이의 트리.
/// </summary>
public class CanonGroup : EntityBase, ICodedName
{
    public long CollectionId { get; set; }
    public CanonCollection Collection { get; set; } = null!;
    public long? ParentId { get; set; }
    public CanonGroup? Parent { get; set; }
    public GroupKind? Kind { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public string? NameOriginal { get; set; }
    public string? NameRoman { get; set; }
    public int SortOrder { get; set; }
    public ICollection<CanonGroup> Children { get; set; } = [];
    public ICollection<CanonWork> Works { get; set; } = [];
}
