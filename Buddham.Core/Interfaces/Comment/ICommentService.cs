using Buddham.Core.Entities.Auth;
using Buddham.Core.EnumTypes;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Comment;

public interface ICommentService
{
    Task<List<CommentSchema.Response>> GetRepliesAsync(string targetType, long rootId, Guid userId);
    Task<CommentSchema.Response> CreateAsync(string targetType, CommentSchema.Request request, ApplicationUser user);
    Task<ReactionSchema.Response> ToggleReactionAsync(string targetType, long commentId, Guid userId, ReactionType newType);
    Task DeleteAsync(string targetType, long id, Guid userId, bool isAdmin);
}
