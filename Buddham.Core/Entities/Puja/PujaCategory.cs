using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Puja;

public class PujaCategory : EntityBase
{
    public required string NameKo { get; set; } // 예경, 재의식, 신행생활 등
    public string? Description { get; set; }
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }

    public virtual PujaCategory? Parent { get; set; }
    public virtual ICollection<PujaCategory> Children { get; set; } = [];
}