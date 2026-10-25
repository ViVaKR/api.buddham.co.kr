namespace Buddham.Core.Interfaces.Comment;

public interface ICommentTarget
{
    long TargetId { get; set; }
    static abstract string TargetColumnPropertyName { get; } 
}
