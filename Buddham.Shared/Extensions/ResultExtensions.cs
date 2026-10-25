using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Shared.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult<TResult>(
        this TResult result,
        ControllerBase controller
    ) where TResult : ResultBase
    {
        return result.Code switch
        {
            RsCode.Ok => controller.Ok(result),
            RsCode.Created => controller.StatusCode(201, result),
            RsCode.Accepted => controller.StatusCode(202, result),
            RsCode.NoContent => controller.NoContent(), // 204는 보통 본문을 보내지 않음

            RsCode.BadRequest => controller.BadRequest(result),
            RsCode.Unauthorized => controller.Unauthorized(result),
            RsCode.Forbidden => controller.Forbid(), // 또는 controller.StatusCode(403, result)
            RsCode.NotFound => controller.NotFound(result),
            RsCode.Conflict => controller.Conflict(result),

            RsCode.Error => controller.StatusCode(500, result),
            _ => controller.StatusCode((int)result.Code, result)
        };
    }

    public static Result ToResult<T>(this Result<T> result)
    {
        return new Result
        {
            IsSuccess = result.IsSuccess,
            Message = result.Message,
            Code = result.Code,
            ValidationErrors = result.ValidationErrors,
            TraceId = result.TraceId
        };
    }

    /*
    // >> [실제 활용] <<
    public async Task<Result> UpdateProfileAsync(UpdateDto dto)
    {
        // 상세 정보를 반환하는 내부 메서드를 호출한 뒤...
        Result<User> updateDetail = await internalService.UpdateUserAsync(dto);

        // 유저 데이터까지 프론트에 줄 필요는 없을 때 ToResult()로 변환!
        return updateDetail.ToResult();
    }
    */
}