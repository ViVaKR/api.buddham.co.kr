using Buddham.Core.Entities.Auth;
using Buddham.Core.EnumTypes;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Comment;

public interface ICommentBridge
{
    Task<List<CommentSchema.Response>> GetRepliesAsync(long targetId, Guid userId);
    Task<CommentSchema.Response> CreateAsync(CommentSchema.Request request, ApplicationUser user);
    Task<ReactionSchema.Response> ToggleReactionAsync(long commentId, Guid userId, ReactionType newType);
    Task DeleteAsync(long id, Guid userId, bool isAdmin);
}
