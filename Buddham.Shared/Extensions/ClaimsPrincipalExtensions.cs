using System.Security.Claims;
using Buddham.Core.Entities.Auth;
using Microsoft.AspNetCore.Identity;

namespace Buddham.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Guid UserId 추출
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim)) return null;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// 사용자 ID 가져오기 null 이면 예외
    /// </summary>
    public static Guid GetUserIdOrThrow(this ClaimsPrincipal user)
    {
        return user.GetUserId()
               ?? throw new InvalidOperationException("인증되지 않은 사용자입니다.");
    }

    /// <summary>
    /// 사용자 정보 가져오기
    /// </summary>
    public static async Task<ApplicationUser?> GetUserAsync(
        this ClaimsPrincipal user,
        UserManager<ApplicationUser> userManager)
    {
        var userId = user.GetUserId();
        return userId.HasValue ? await userManager.FindByIdAsync(userId.Value.ToString()) : null;
    }
}
