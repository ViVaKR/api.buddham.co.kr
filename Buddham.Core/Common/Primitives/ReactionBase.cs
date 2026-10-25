using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Comment;

namespace Buddham.Core.Common.Primitives;

public abstract class ReactionBase : IReaction
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ReactionType ReactionType { get; set; }
    public abstract void SetTarget(long commentId, Guid userId, ReactionType reactionType);
}
