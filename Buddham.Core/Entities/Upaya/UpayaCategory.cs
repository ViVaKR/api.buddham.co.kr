using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Upaya;

public class UpayaCategory : EntityBase
{
    /// <summary>
    /// 법문영상, 탱화갤러리, 독경음원 등
    /// </summary>
    public required string NameKo { get; set; }

    public string? Description { get; set; }
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }

    public virtual UpayaCategory? Parent { get; set; }
    public virtual ICollection<UpayaCategory> Children { get; set; } = [];
}
