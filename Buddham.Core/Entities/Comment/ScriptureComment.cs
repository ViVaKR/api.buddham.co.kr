using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Comment;

namespace Buddham.Core.Entities.Comment;

public class ScriptureComment : CommentBase, ICommentTarget
{
    public long ScriptureId { get; set; }

    public long TargetId
    {
        get => ScriptureId;
        set => ScriptureId = value;
    }

    public static string TargetColumnPropertyName => nameof(ScriptureId);

    // public virtual Scripture Scripture { get; set; } = null!;

    public virtual ICollection<ScriptureComment> Replies { get; set; } = [];
    public virtual ICollection<ScriptureCommentLike> Likes { get; set; } = [];
}

public class ScriptureCommentLike : ReactionBase
{
    public long CommentId { get; set; }

    public virtual ScriptureComment Comment { get; set; } = null!;

    public override void SetTarget(long commentId, Guid userId, ReactionType reactionType)
    {
        CommentId = commentId;
        UserId = userId;
        ReactionType = reactionType;
    }
}
