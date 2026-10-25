using Buddham.Core.Entities.Auth;
using Buddham.Core.Entities.Comment;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Comment;
using Buddham.Core.Schemas;
using Microsoft.Extensions.DependencyInjection;

namespace Buddham.Shared.Services.Comment;

public class CommentService(IServiceProvider provider) : ICommentService
{
    // 문자열을 분석하여 알맞은 분리 싀마 서비스(GenericCommentService) 를 찾아 주는 지혜의 눈
    private ICommentBridge GetActualService(string targetType) => targetType.ToLower() switch
    {
        "scripture" => provider.GetRequiredService<GenericCommentService<ScriptureComment, ScriptureCommentLike>>(),

        // 프론트엔드가 보낸 'GuideComments'를 소문자로 받아서 처리하는 노선 추가!
        "guidecomments" => provider.GetRequiredService<GenericCommentService<GuideComment, GuideCommentLike>>(),

        // ☀️ 향후 추가 스키마가 추가
        _ => throw new ArgumentException($"'{targetType}'은(는) 존재하지 않는 청정 스키마 경로입니다.")
    };

    public async Task<List<CommentSchema.Response>> GetRepliesAsync(string targetType, long rootId, Guid userId)
        => await GetActualService(targetType).GetRepliesAsync(rootId, userId);

    public async Task<CommentSchema.Response> CreateAsync(string targetType, CommentSchema.Request dto, ApplicationUser
        user)
        => await GetActualService(targetType).CreateAsync(dto, user);

    public async Task<ReactionSchema.Response> ToggleReactionAsync(string targetType, long commentId, Guid userId,
        ReactionType reactionType)
        => await GetActualService(targetType).ToggleReactionAsync(commentId, userId, reactionType);

    public async Task DeleteAsync(string targetType, long id, Guid userId, bool isAdmin)
        => await GetActualService(targetType).DeleteAsync(id, userId, isAdmin);
}
