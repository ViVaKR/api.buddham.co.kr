using Buddham.Core.Interfaces;

namespace Buddham.Core.Common.Primitives;

public abstract class EntityBase : IAuditable, ISoftDeletable
{
    public long Id { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
