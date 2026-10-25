using Buddham.Api.Controllers.BaseControllers;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Entities.Comment;
using Buddham.Core.Schemas;
using Buddham.Shared.Services.Comment;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Public;

[ApiController]
[Route("[controller]")]
public class GuideCommentController(
    GenericCommentService<GuideComment, GuideCommentLike> service,
    UserManager<ApplicationUser> userManager
) : CommentControllerBase<GuideComment, GuideCommentLike>(service, userManager)
{
    protected override void SetDomainKey(GuideComment comment, CommentSchema.Request request)
    {
        // ★ 대댓글일 때는 request.TargetId가 null일 수 있으므로
        // 값이 있을 때만 할당하고, 없으면 서비스(GenericCommentService)에서 부모를 통해 처리하도록 넘깁니다.
        if (request.TargetId.HasValue)
        {
            comment.GuideId = request.TargetId.Value;
        }
        else if (!request.ParentId.HasValue)
        {
            // 원댓글인데 TargetId가 없는 경우는 에러!
            throw new ArgumentException("GuideId 는 필수입니다.");
        }
        // 대댓글(ParentId가 있는 경우)이고 TargetId가 없다면,
        // GenericCommentService 내부에서 부모 댓글의 TargetId를 상속받아 채워주게 됩니다.
    }
    protected override async Task<bool> IsAdminAsync(ApplicationUser user) =>
        await UserManager.IsInRoleAsync(user, "Admin");
}
