using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Comment;

public class SutraCopyingCommentLike : ReactionBase
{
    public long CommentId { get; set; }
    public virtual SutraCopyingComment Comment { get; set; } = null!;
    public override void SetTarget(long commentId, Guid userId, ReactionType reactionType)
    {
        CommentId = commentId;
        UserId = userId;
        ReactionType = reactionType;
    }
}
