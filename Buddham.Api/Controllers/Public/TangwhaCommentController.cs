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
public class TangwhaCommentController(
    GenericCommentService<TangwhaComment, TangwhaCommentLike> service,
    UserManager<ApplicationUser> userManager
) : CommentControllerBase<TangwhaComment, TangwhaCommentLike>(service, userManager)
{
    protected override Task<bool> IsAdminAsync(ApplicationUser user)
    {
        return UserManager.IsInRoleAsync(user, "Admin");
    }

    protected override void SetDomainKey(TangwhaComment comment, CommentSchema.Request request)
    {
        // 대댓글일 때는 request.TargetId 가 null 일 수 있으므로
        // 값이 있을 때만 할당하고 없으면, GenericCommentService 에서 부모를 통해 처리 하도록 넘김
        if (request.TargetId.HasValue)
        {
            comment.TangwhaId = request.TargetId.Value;
        }
        else if (!request.ParentId.HasValue)
        {
            // 원댓글인데 TargetId 가 없는 경우는 에러!
            throw new ArgumentException("GuidId 는필수 입니다.");
        }

        // 대댓글(ParentId가 있는 경우) 이고 TargetId 가 없다면
        // GenericCommentService 에서 부모 댓글의 TargetId를 상속받아 채워주게 됩니다.
    }
}
