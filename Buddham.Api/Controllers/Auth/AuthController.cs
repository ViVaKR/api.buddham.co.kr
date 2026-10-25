using System.Security.Claims;
using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Interfaces.Cache;
using Buddham.Core.Schemas;
using Buddham.Shared.Constants;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions;
using Buddham.Shared.Filters;
using Buddham.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Api.Controllers.Auth;

[ApiController]
[Route("[controller]")]
public class AuthController(
    PublicDbContext context,
    UserManager<ApplicationUser> userManager,
    IRedisCacheService redisCacheService,
    IAuthService authService,
    IUserService userService,
    IEmailAuthService emailAuthService
) : BaseController
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 인증
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// 로그인
    /// * --> Auth/SignIn
    /// </summary>
    [AllowAnonymous]
    [HttpPost("SignIn")]
    [ValidateRequest<AuthSchema.SignIn>]
    public async Task<ActionResult<Result<AuthSchema.Identity>>> SignIn([FromBody] AuthSchema.SignIn request)
        => Ok(await authService.SignInAsync(request));

    /// <summary>
    /// 로그아웃
    ///  * --> Auth/SignOut
    /// </summary>
    [Authorize]
    [HttpPost("SignOut")]
    public async Task<ActionResult<Result>> SignOutAsync()
        => Ok(await authService.SignOutAsync(UserIdString));

    /// <summary>
    /// 토큰갱신
    /// * --> Auth/RefreshToken
    /// </summary>
    [AllowAnonymous]
    [HttpPost("RefreshToken")]
    public async Task<ActionResult<AuthSchema.Identity>> RefreshToke
        ([FromBody] AuthSchema.TokenRefresh request)
        => (await authService.RefreshTokenAsync(request)).ToActionResult(this);

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 회원 관리
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// 회원가입
    /// </summary>
    [AllowAnonymous]
    [HttpPost("SignUp")]
    public async Task<ActionResult<Result>> SignUp([FromBody] AuthSchema.SignUp request)
        => Ok(await authService.SignUpAsync(request));

    /// <summary>
    /// 회원탈퇴 (회원용)
    /// * --> Auth/Withdraw
    /// </summary>
    [Authorize]
    [HttpPost("Withdraw")]
    public async Task<ActionResult<Result>> Widraw([FromBody] AuthSchema.Withdraw request)
        => Ok(await authService.WithdrawAsync(request, UserIdString));

    /// <summary>
    /// 회원삭제 (관리자용)
    /// * --> Auth/DeleteAccountByAdmin
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("DeleteAccountByAdmin")]
    public async Task<ActionResult<Result>> DeleteAccount([FromBody] AuthSchema.DeleteAccountByAdmin request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId is null) return Result.Failure(RsCode.BadRequest, "있는 권한이 없습니다.");
        return Ok(await authService.DeleteAccountByAdminAsync(request,
            adminId));
    }

    /// <summary>
    /// 회원정보 보기(사용자용)
    /// * --> Auth/UserDetail
    /// </summary>
    [Authorize]
    [HttpGet("UserDetail")]
    public async Task<ActionResult<Result<AuthSchema.UserDetail>>> UserDetail()
        => Ok(await authService.GetUserDetailAsync(UserIdString));

    /// <summary>
    /// 회원정보 보기(관리자용)
    /// * --> Auth/UserDetailByAdmin/{id}
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("UserDetailByAdmin/{id}")]
    public async Task<ActionResult<Result<AuthSchema.UserDetail>>> GetUserDetailForAdmin(string id)
        => Ok(await authService.GetUserDetailAsync(id));

    /// <summary>
    /// 회원정보 보기 (수정용)
    /// </summary>
    [Authorize]
    [HttpGet("Read/{id}")]
    public async Task<ActionResult<UserSchema.Response>> ReadById(string id)
        => Ok(await userService.GetUserInfoByAdminAsync(id));

    /// <summary>
    /// 회원정보 수정 (회원용)
    /// --> Auth/Update
    /// </summary>
    [Authorize]
    [HttpPut("Update")]
    public async Task<ActionResult<Result<AuthSchema.UserDetail>>> UpdateUserInfo([FromBody] AuthSchema.UpdateUser dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Result<AuthSchema.UserDetail>.Failure(RsCode.Unauthorized, "인증 정보가 없습니다.");

        // DTO의 ID와 비교할 필요 없이 인증된 ID로만 처리
        return Ok(await authService.UpdateAccountBySelfAsync(userId, dto));
    }

    /// <summary>
    /// 회원정보 수정 (관리자 / 회원용)
    /// --> Auth/Update
    /// </summary>
    [Authorize]
    [HttpPut("Update/{id}")]
    public async Task<ActionResult<Result>> Update(
        string id, [FromBody] UserSchema.Request request, CancellationToken ct)
        => Ok(await userService.UpdateAsync(id, request, ct));

    /// <summary>
    /// 1. 비밀번호 분실
    /// 재설정 이메일 발송
    /// --> Auth/ForgotPassword
    /// </summary>
    [AllowAnonymous]
    [HttpPost("ForgotPassword")]
    public async Task<ActionResult<Result<EmailSchema.Identity>>> ForgotPassword(
        [FromBody] EmailSchema.ForgotPassword dto)
        => Ok(await emailAuthService.ForgotPasswordAsync(dto));

    /// <summary>
    /// 2. 비밀번호 재설정, 재설정 회신 (임시토큰 포함)
    /// * --> Auth/Resetpassword
    /// </summary>
    [AllowAnonymous]
    [HttpPost("ResetPassword")]
    public async Task<ActionResult<Result>> ResetPassword([FromBody] AuthSchema.ResetPassword dto)
        => Ok(await authService.ResetPasswordAsync(dto));

    /// <summary>
    /// 관리자용 강제 비밀번호 재설정
    /// * --> Auth/ForceUpdatePassword
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("ForceUpdatePassword")]
    public async Task<ActionResult<Result>> ForceUpdatePassword([FromBody] AuthSchema.ResetPassword dto)
        => Ok(await authService.ResetPasswordAsync(dto));

    /// <summary>
    /// 비밀번호 점검 (회원전용)
    /// 중요 작업 전 본인 확인용
    /// * --> Auth/CheckPassword
    /// </summary>
    [Authorize]
    [HttpPost("CheckPassword")]
    public async Task<ActionResult<Result>> CheckPassword([FromBody] AuthSchema.CheckPassword dto)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Result.Failure(RsCode.Unauthorized, "인증세션이 만료되었습니다.");

        return Ok(await authService.CheckPasswordAsync(dto, user));
    }

    /// <summary>
    /// 비밀번호 변경
    /// 나의 비밀번호 관리 (회원전용)
    /// * --> Auth/ChangePassword
    /// </summary>
    [Authorize]
    [HttpPut("ChangePassword")]
    public async Task<ActionResult<Result>> ChangePassword([FromBody] AuthSchema.ChangePassword dto)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Result.Failure(RsCode.Unauthorized, "인증세션이 만료되었습니다.");

        return Ok(await authService.ChangePasswordAsync(dto, user));
    }

    /// <summary>
    /// 사용자 아바타 조회
    /// * --> Auth/UserAvatars
    /// </summary>
    [Authorize]
    [HttpGet("UserAvatar")]
    public async Task<ActionResult<Result<string>>> GetAvatar()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Ok(Result<string>.NotFound());

        // ✅ file_datas에서 직접 조회 — 최신 완성 아바타
        var avatar = await context.FileDatas
            .Where(x => x.UserId == user.Id
                        && x.FileType == UploadType.Avatar
                        && x.Status == FileStatus.Completed
                        && x.WebpPath != null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.WebpPath)
            .FirstOrDefaultAsync();

        return Ok(Result<string>.Success(avatar!, "아바타 조회 성공"));
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 중복 체크
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    ///     필명 중복 체크
    ///     비회원 회원 가입용
    ///     --> Auth/CheckPsedonym
    /// </summary>
    [AllowAnonymous]
    [HttpPost("CheckPseudonym")]
    public async Task<ActionResult<bool>> CheckPseudonym([FromBody] AuthSchema.CheckPseudonym dto)
    {
        return Ok(await authService.CheckPseudonymAsync(dto));
    }

    /// <summary>
    ///     필명 중복체크
    ///     회원전용 필명 수정용
    ///     --> Auth/CheckPseudonymAuth
    /// </summary>
    [Authorize]
    [HttpPost("CheckPseudonymAuth")]
    public async Task<ActionResult<bool>> CheckPseudonymAuth([FromBody] AuthSchema.CheckPseudonym dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return BadRequest();
        return Ok(await authService.CheckPseudonymAsync(dto));
    }

    /// <summary>
    /// 필명수정 (회원 및 관리자용)
    /// </summary>
    [Authorize]
    [HttpPut("UpdatePseudonymAuth/{id}")]
    public async Task<ActionResult<bool>> UpdatePseudonymAuth(string id, [FromBody] AuthSchema.UpdatePseudonym payload)
    {
        var result = await authService.UpdatePseudonymAuthAsync(id, payload);
        return Ok(result);
    }

    /// <summary>
    /// 필명수정 (회원 및 관리자용)
    /// </summary>
    [Authorize]
    [HttpPut("UpdateFullNameAuth/{id}")]
    public async Task<ActionResult<bool>> UpdateFullNameAuth(string id, [FromBody] AuthSchema.UpdateFullName payload)
    {
        var result = await authService.UpdateFullNameAuthAsync(id, payload);
        return Ok(result);
    }

    /// <summary>
    /// 이메일 중복체크 (회원가입용)
    /// * --> Auth/CheckEmail
    /// </summary>
    [AllowAnonymous]
    [HttpPost("CheckEmail")]
    public async Task<ActionResult<bool>> CheckEmail([FromBody] AuthSchema.CheckEmail dto)
    {
        return Ok(await authService.CheckEmailAsync(dto));
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 이메일 인증
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    ///     이메일 인증 용 메일 전송
    ///     --> Auth/VerifySendEmail
    /// </summary>
    [Authorize]
    [EnableRateLimiting("email")]
    [HttpPost("VerifySendEmail")]
    public async Task<ActionResult<Result>> VerifySendEMail([FromBody] EmailSchema.VerifyEmailRequest request)
        => Ok(await emailAuthService.VerifySendEmailAsync(request));

    /// <summary>
    ///     이메일 인증용 메일 수신 및 완료하기
    ///     --> MemberShip/ComfirmEmail
    /// </summary>
    [Authorize]
    [HttpPost("ConfirmEmail")]
    public async Task<ActionResult<Result<bool>>> ConfirmEmail([FromBody] EmailSchema.ConfirmEmail dto)
        => Ok(await emailAuthService.ConfirmEmailAsync(dto));

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 관리자
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// 회원목록 (관리자 용)
    ///  --> Auth/Users
    /// </summary>
    [HttpGet("Users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Result<IEnumerable<AuthSchema.UserDetail>>>> GetUserList()
    {
        return Ok(await authService.GetUserListAsync());
    }

    /// <summary>
    ///     회원권한 수정 (관리자 용)
    ///     --> Auth/Users/{id}/Roles
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("Users/{id}/Roles")]
    public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] AuthSchema.UpdateUserRoles dto)
    {
        return Ok(await authService.UpdateUserRolesAsync(id, dto));
    }

    /// <summary>
    ///     총 회원수 (관리자 용)
    ///     --> Auth/TotalAccount
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("TotalAccount")]
    public async Task<ActionResult<Result<int>>> GetAccounts()
    {
        var (totalCount, until, isFromCache) = await redisCacheService.GetOrSetCacheAsync(RedisKeys.TotaAccount,
            async () => { return await userManager.Users.AsNoTracking().Where(x => x.IsActive).CountAsync(); },
            TimeSpan.FromMinutes(5));

        var meta = new
        {
            IsCache = isFromCache,
            ExpireAt = until?.ToString("yyyy-MM-dd HH:mm:ss"),
            Source = isFromCache ? "Redis" : "PostgreSQL"
        };
        // 캐시일 때와 아닐 때의 메시지 분기 (지배자의 안제)
        var message = isFromCache
            ? "조회된 데이터는 캐시된 정보이며, 실시간 데이터와 약 5분 정도의 오차가 있을 수 있습니다."
            : "DB에서 실시간 데이터를 성공적으로 조회하였습니다.";

        var result = Result<int>.Success(totalCount, meta, message);

        return Ok(result);
    }

    /// <summary>
    /// 메일 테스트 (전송)
    /// Auth/SendTestMail
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("SendTestMail")]
    public async Task<ActionResult<Result<bool>>> SentTestMail([FromBody] EmailSchema.SendTestMail dto)
    {
        var result = await emailAuthService.SendTestEmailAsync(dto.ToMail);
        return Result<bool>.Success(result, null, "테스트 메일요청");
    }
}
