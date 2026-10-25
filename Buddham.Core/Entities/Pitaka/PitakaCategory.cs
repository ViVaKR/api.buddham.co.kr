using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Pitaka;

public class PitakaCategory : EntityBase
{
    public required string NameKo { get; set; }
    public string? NamePali { get; set; }
    public string? Description { get; set; }
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }

    public virtual PitakaCategory? Parent { get; set; }
    public virtual ICollection<PitakaCategory> Children { get; set; } = [];
}
