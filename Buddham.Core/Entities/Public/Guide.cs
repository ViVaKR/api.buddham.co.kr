using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Common;

namespace Buddham.Core.Entities.Public;

public class Guide : EntityBase, IOwnedEntity, IEmbeddable
{
    public required GuideCategory GuideCategory { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public Guid UserId { get; set; }
    public float[]? Embedding { get; set; }
    public string? SearchVector { get; set; }
}
