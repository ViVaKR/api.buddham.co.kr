using Buddham.Core.Interfaces;

namespace Buddham.Core.Common.Primitives;

public abstract class GuidEntityBase : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}