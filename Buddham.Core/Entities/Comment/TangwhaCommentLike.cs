using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Comment;

public class TangwhaCommentLike : ReactionBase
{
    public long CommentId { get; set; }
    public virtual TangwhaComment Comment { get; set; } = null!;
    public override void SetTarget(long commentId, Guid userId, ReactionType reactionType)
    {
        CommentId = commentId;
        UserId = userId;
        ReactionType = reactionType;
    }
}
