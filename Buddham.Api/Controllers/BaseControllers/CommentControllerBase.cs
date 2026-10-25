using Buddham.Core.Common.Primitives;
using Buddham.Core.Entities.Auth;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Comment;
using Buddham.Core.Schemas;
using Buddham.Shared.Extensions;
using Buddham.Shared.Services.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.BaseControllers;

[Authorize]
public abstract class CommentControllerBase<TComment, TReaction>(
    GenericCommentService<TComment, TReaction> service,
    UserManager<ApplicationUser> userManager
) : ControllerBase
    where TComment : CommentBase, ICommentTarget, new()
    where TReaction : ReactionBase, new()
{
    protected UserManager<ApplicationUser> UserManager { get; } = userManager;
    protected abstract Task<bool> IsAdminAsync(ApplicationUser user);
    protected abstract void SetDomainKey(TComment comment, CommentSchema.Request request);

    /// <summary>
    /// GET
    /// Replies/{rootId}
    /// </summary>
    [HttpGet("Replies/{rootId:long}")]
    public async Task<IActionResult> GetReplies(long rootId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var result = await service.GetRepliesAsync(rootId, userId.Value);
        return Ok(result);
    }

    /// <summary>
    /// // POST
    /// --> CommentCreate
    /// </summary>
    [HttpPost("CommentCreate")]
    public async Task<IActionResult> CreateComment([FromBody] CommentSchema.Request dto
    )
    {
        var comment = new TComment
        {
            RootId = dto.RootId,
            ParentId = dto.ParentId,
            Content = dto.Content,
            MentionedUserName = dto.MentionedUserName
        };

        SetDomainKey(comment, dto);
        var user = await User.GetUserAsync(UserManager);
        if (user is null) return Unauthorized();
        var result = await service.CreateAsync(comment, user);
        return Ok(result);
    }

    /// <summary>
    /// POST
    /// --> Reactions/{commentId:long}?reactionType=1
    /// </summary>
    [HttpPost("Reactions/{commentId:long}")]
    public async Task<IActionResult> ToggleReaction(long commentId, [FromQuery] ReactionType reactionType)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var result = await service.ToggleReactionAsync(commentId, userId.Value, reactionType);
        return Ok(result);
    }

    /// <summary>
    /// DELETE
    /// --> CommentDelete/{id}
    /// </summary>
    [HttpDelete("CommentDelete/{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var user = await User.GetUserAsync(UserManager);
        if (user is null) return Unauthorized();
        var isAdmin = await IsAdminAsync(user);
        await service.DeleteAsync(id, user.Id, isAdmin);
        return Ok("삭제 완료");
    }
}
