using Buddham.Core.EnumTypes;

namespace Buddham.Core.Interfaces.Comment;

public interface IReaction
{
    void SetTarget(long commentId, Guid userId, ReactionType reactionType);
}
