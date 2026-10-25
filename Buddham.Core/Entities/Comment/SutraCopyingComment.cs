using Buddham.Core.Common.Primitives;
using Buddham.Core.Entities.Public;
using Buddham.Core.Interfaces.Comment;

namespace Buddham.Core.Entities.Comment;

public class SutraCopyingComment : CommentBase, ICommentTarget
{
    public long SutraCopyingId { get; set; }

    public long TargetId
    {
        get => SutraCopyingId;
        set => SutraCopyingId = value;
    }

    public static string TargetColumnPropertyName => nameof(SutraCopyingId);
    public virtual SutraCopying SutraCopying { get; set; } = null!;
    public virtual ICollection<SutraCopyingComment> Replies { get; set; } = [];
    public virtual ICollection<SutraCopyingCommentLike> Likes { get; set; } = [];
}
