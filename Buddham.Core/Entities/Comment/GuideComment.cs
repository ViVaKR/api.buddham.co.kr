using Buddham.Core.Common.Primitives;
using Buddham.Core.Entities.Public;
using Buddham.Core.Interfaces.Comment;

namespace Buddham.Core.Entities.Comment;

public class GuideComment : CommentBase, ICommentTarget
{
    public long GuideId { get; set; }

    public long TargetId
    {
        get => GuideId;
        set => GuideId = value;
    }

    public static string TargetColumnPropertyName => nameof(GuideId);
    public virtual Guide Guide { get; set; } = null!;
    public virtual ICollection<GuideComment> Replies { get; set; } = [];
    public virtual ICollection<GuideCommentLike> Likes { get; set; } = [];
}
