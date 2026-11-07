using Buddham.Core.Common.Primitives;
using Buddham.Core.Entities.Public;
using Buddham.Core.Interfaces.Comment;

namespace Buddham.Core.Entities.Comment;

public class TangwhaComment : CommentBase, ICommentTarget
{
    public long TangwhaId { get; set; }
    public long TargetId
    {
        get => TangwhaId;
        set => TangwhaId = value;
    }

    public static string TargetColumnPropertyName => nameof(TangwhaId);

    public virtual Tangwha Tangwha { get; set; } = null!;
    public virtual ICollection<TangwhaComment> Replies { get; set; } = [];
    public virtual ICollection<TangwhaCommentLike> Likes { get; set; } = [];
}
