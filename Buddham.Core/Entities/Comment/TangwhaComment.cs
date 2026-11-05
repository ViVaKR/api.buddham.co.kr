using Buddham.Core.Common.Primitives;
using Buddham.Core.Entities.Public;
using Buddham.Core.Interfaces.Comment;

namespace Buddham.Core.Entities.Comment;

public class TangwhaComment : CommentBase, ICommentTarget
{
    public long GuideId { get; set; }
    public long TargetId
    {
        get => GuideId;
        set => GuideId = value;
    }

    public static string TargetColumnPropertyName => nameof(GuideId);

    public virtual Tangwha Tangwha { get; set; } = null!;
    public virtual ICollection<TangwhaComment> Replies { get; set; } = [];
    public virtual ICollection<TangwhaCommentLike> Likes { get; set; } = [];
}
