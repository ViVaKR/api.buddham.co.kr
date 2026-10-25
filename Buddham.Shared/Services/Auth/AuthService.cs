using System.Security.Claims;
using System.Text;
using Buddham.Core.Common;
using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Schemas;
using Buddham.Shared.Constants;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Buddham.Shared.Services.Auth;

public class AuthService(
    BuddhamDbContext buddhamDbContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    IOptions<BuddhamAppSettings> buddhamAppSettingsOptions) : IAuthService
{
    private readonly BuddhamAppSettings _buddhamAppSettings = buddhamAppSettingsOptions.Value;

    // ══════════════════════════════════════════
    // 🔐 로그인 / 로그아웃
    // ══════════════════════════════════════════

    /// <summary>
    ///     SignIn
    /// </summary>
    public async Task<Result<AuthSchema.Identity>> SignInAsync(AuthSchema.SignIn dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);

        if (user is { IsActive: false } or null)
            return Result<AuthSchema.Identity>.Failure(RsCode.BadRequest, "현재 비 회원 도반이십니다.");

        // * 비밀번호 체크 (Lockout 포함)
        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, true);

        if (result.IsLockedOut)
            return Result<AuthSchema.Identity>.Failure(RsCode.Error, "계정이 잠겼습니다. 잠시후 다시 시도하여 주세요.");

        if (!result.Succeeded)
            return Result<AuthSchema.Identity>.Failure(RsCode.Error, "이메일 혹은 비밀번호가 일치하지 않습니다.");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 신뢰 기기 검증/
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        var trusted =
            user.TrustedDeviceKey == dto.DeviceFingerprint &&
            user.TrustedDeviceExpiry.HasValue &&
            user.TrustedDeviceExpiry > DateTime.UtcNow;

        // * 2FA 로직 처리
        if (user.TwoFactorEnabled && !trusted)
        {
            var status = new AuthSchema.Identity
            {
                UserId = user.Id,
                Token = null,
                RefreshToken = null,
                RequiresTwoFactor = true
            };
            return Result<AuthSchema.Identity>.Success(status, "2FA_REQIRED");
        }

        var isEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);

        var twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);

        // * ITokenService 를 사용하여 토큰 생성
        var accessToken = await tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        // * 사용자 정보 업데이트 (Refresh Token 저장)
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = dto.IpAddress;
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        var userIdentity = user.ToIdentity(
            accessToken,
            refreshToken,
            user.RefreshTokenExpiryTime,
            isEmailConfirmed,
            twoFactorEnabled
        );

        return Result<AuthSchema.Identity>.Success(userIdentity, new
        {
            user.Pseudonym,
            user.Avatar,
            user.AccessFailedCount,
            user.CreatedAt,
            user.FullName
        }, "로그인 성공");
    }

    /// <summary>
    ///     SignOut
    /// </summary>
    public async Task<Result> SignOutAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Success("성공적으로 로그아웃 되었습니다.");

        // 3. 보안 토큰 무효화
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1);

        // 4. DB 반영
        var updateResult = await userManager.UpdateAsync(user);

        // 5. 결과 반환
        if (!updateResult.Succeeded)
        {
            var errorMsg = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(RsCode.Error, $"로그아웃 처리 중 알수없는 오류 발생:\n{string.Join('\n', errorMsg)}");
        }

        // 6. 성공 보고 (데이터가 없으니 null 전달)
        return Result.Success("성공적으로 로그아웃 되었습니다.");
    }

    /// <summary>
    ///     토큰갱신
    /// </summary>
    public async Task<Result<AuthSchema.Identity>> RefreshTokenAsync(AuthSchema.TokenRefresh dto)
    {
        // 1. 널 체크
        if (string.IsNullOrEmpty(dto.RefreshToken) || string.IsNullOrEmpty(dto.AccessToken))
            return Result<AuthSchema.Identity>.Failure(RsCode.BadRequest, "토큰 정보가 누락되었습니다.");

        // 2. 만료된 토큰에서 정보 추출
        var principal = tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
        if (principal is null)
        {
            return Result<AuthSchema.Identity>.Failure(RsCode.Unauthorized, "유효하지 않은 액세스 토큰입니다.");
        }

        // 3. 토큰 안의 SecurityStamp 추출 (CONTS.SecurityStamp 클레임)
        var tokenStamp = principal.FindFirstValue(AppInfrastructure.SecurityStamp);

        // 4. 유저 식별
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await userManager.FindByIdAsync(userId ?? "");
        if (user is null)
        {
            return Result<AuthSchema.Identity>.Failure(RsCode.Unauthorized, "존재하지 않는 사용자입니다.");
        }

        // 🔥 [핵심] DB의 스탬프와 토큰의 스탬프 비교!
        // 만약 2FA 성공 후 스탬프를 업데이트했다면, 이전 액세스 토큰은 여기서 걸러져야 합니다.
        if (user.SecurityStamp != tokenStamp)
        {
            return Result<AuthSchema.Identity>.Failure(RsCode.Unauthorized, "토큰이 더 이상 유효하지 않습니다. 다시 로그인 해주세요.");
        }

        // 4. 리프레시 토큰 값 및 만료 시간 검증
        if (user.RefreshToken != dto.RefreshToken)
        {
            return Result<AuthSchema.Identity>.Failure(RsCode.Unauthorized, "리프레시 토큰이 유효하지 않습니다.");
        }

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Result<AuthSchema.Identity>.Failure(RsCode.Unauthorized, "리프레시 토큰이 만료되었습니다.");
        }

        // 4. 새로운 토큰 생성
        var newAccessToken = await tokenService.GenerateAccessTokenAsync(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        // 5. DB 저장
        user.RefreshToken = newRefreshToken;
        // 권장 - 만료시간도 같이 갱신
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 슬라이딩 갱신
        // * 즉시 로그아웃 효과 (모든 기기에서 튕겨나가게 함)
        await userManager.UpdateAsync(user);

        // 6. ✅ 순수 데이터 DTO 만 반환 (성공/실패 판단은 Result 가 담당)
        var identity = user.ToIdentity(
            newAccessToken,
            newRefreshToken,
            user.RefreshTokenExpiryTime,
            user.EmailConfirmed,
            user.TwoFactorEnabled
        );
        return Result<AuthSchema.Identity>.Success(identity, "토큰이 성공적으로 갱신되었습니다.");
    }


    /// <summary>
    /// 회원가입
    /// </summary>
    public async Task<Result> SignUpAsync(AuthSchema.SignUp request)
    {
        // * (1) 이메일 중복 확인
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Result.Failure(RsCode.BadRequest, "이미 가입된 메일입니다.");

        // * (2) 필명 중복 확인
        var existingPsedonym = await userManager.Users.AnyAsync(x => x.Pseudonym.Equals(request.Pseudonym));

        if (existingPsedonym)
            return Result.Failure(RsCode.BadRequest, "이미 사용중이 필명입니다.");

        var user = request.ToEntity();

        var createResult = await userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(x => x.Description).ToArray();
            return Result.Failure(RsCode.Error, $"회원가입에 실패하였습니다. {string.Join(Environment.NewLine, errors)}");
        }

        // * (4) 기본역할 (User) 부여
        await userManager.AddToRoleAsync(user, UserRoles.User);

        // 이메일 전송에 필요한 '최소한의 데이터'만 변수에 복사
        var userId = user.Id;
        var userEmail = user.Email!;
        var userFullName = user.FullName;
        var userPseudonym = user.Pseudonym;

        // 인증 토큰 생성
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // * (5) 안전한 백그라운드 이메일 전송 (Fire-and-forget)
        _ = Task.Run(async () =>
        {
            // 새로운 Scope를 생성하여 DbContext 소멸 문제 해결
            using var scope = scopeFactory.CreateScope();

            var bgUserManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var bgLogger = scope.ServiceProvider.GetRequiredService<ILogger<AuthService>>();

            try
            {
                var bgUser = await bgUserManager.FindByIdAsync(userId.ToString());
                if (bgUser is null) throw new Exception("백그라운드 작업에서 사용자를 찾을 수 없습니다.");

                var confirmationLink = $"{_buddhamAppSettings.FrontendUrl}/MemberShip/ConfirmEmailReply" +
                                       $"?email={Uri.EscapeDataString(user.Email!)}" +
                                       $"&token={encodedToken}";

                await emailService.SendEmailAsync(user.Email!, "회원가입을 축하 및 이메일 인증 안내",
                    emailTemplateService.GetWelcomeEmailHtml(userFullName, userPseudonym, confirmationLink));
            }
            catch (Exception ex)
            {
                bgLogger.LogError(ex, "백그라운드 이메일 전송 중 오류 발생: {Email}", userEmail);
            }
        });

        return Result.Success("회원가입 완료! 메일함을 확인하여 주세요");
    }

    /// <summary>
    /// 회원정보 (관리자 및 일반회원용)
    /// 관리자는 전체 정보, 일반회원은 자기 정보만 조회
    /// </summary>
    public async Task<Result<AuthSchema.UserDetail>> GetUserDetailAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var guidUserId))
            return Result<AuthSchema.UserDetail>.Failure(RsCode.BadRequest, "유효하지 않은 식별자입니다.");

        var user = await userManager.Users.AsNoTracking()
            .Where(x => x.Id == guidUserId && x.DeletedAt == null)
            .ToDTO()
            .FirstOrDefaultAsync();

        return user is null
            ? Result<AuthSchema.UserDetail>.NotFound("해당 사용자를 찾을 수 없습니다.")
            : Result<AuthSchema.UserDetail>.Success(user, "회원상세 정보 전송 완료");
    }

    /// <summary>
    /// 회원목록 (관리자 용)
    /// 일반
    /// </summary>
    public async Task<Result<IEnumerable<AuthSchema.UserDetail>>> GetUserListAsync()
    {
        // 1. 데이터 가져오기 (Projection 활용)
        var users = await userManager.Users.AsNoTracking()
            .AsNoTracking()
            .Where(x => x.IsActive && x.DeletedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ToDTO().ToListAsync();

        return Result<IEnumerable<AuthSchema.UserDetail>>.Success(users, "전체 회원목록 전송완료");
    }

    /// <summary>
    ///     회원탈퇴 (회원용)
    /// </summary>
    public async Task<Result> WithdrawAsync([FromBody] AuthSchema.Withdraw request, string userId)
    {
        // * 현재 로그인한 사용자 정보 가져오기  (인증된 컨텍스트 기반)
        // 컨트롤러에게 넘겨 받거나, 서비스 내에서 ClaimsPrincipal 을 통해 식별해야 합니다.
        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
            return Result.Failure(RsCode.NotFound, "사용자를 찾을 수 없습니다.");

        // * 본인 확인 절차 (보안상 매우 중요)
        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
            return Result.Failure(RsCode.BadRequest, "비밀번호가 일치하지 않아 탈퇴할 수 없습니다.");

        // * 소프트 삭제 수행
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        // * (선택사항)
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
            return Result.Failure(RsCode.Error, "탈퇴 처리 중 오류가 발생했습니다.");

        // * 즉시 로그아웃 효과 (모든 기기에서 튕겨나가게 함)
        await userManager.UpdateSecurityStampAsync(user);

        // * 리스페시 토큰 및 기기 정보 초기화
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await userManager.UpdateAsync(user);
        return Result.Success("그동안 이용해 주셔서 감사합니다. 회원 탈퇴가 안전하게 처리되었습니다.");
    }


    /// <summary>
    ///     회원정보 수정 (회원용)
    /// </summary>
    public async Task<Result<AuthSchema.UserDetail>> UpdateAccountBySelfAsync(string userId, AuthSchema.UpdateUser request)
    {
        // 1. 추적 가능한 상태로 사용자 조회
        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
            return Result<AuthSchema.UserDetail>.Failure(RsCode.NotFound, "가입된 회원이 아닙니다.");

        user.ToPatch(request);

        // 3. 결과 확인 (중요!)
        var identityResult = await userManager.UpdateAsync(user);

        if (!identityResult.Succeeded)
        {
            var errorMsg = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            return Result<AuthSchema.UserDetail>.Failure(RsCode.BadRequest, $"수정 실패: {errorMsg}");
        }

        var userDetail = user.ToDetail();
        return Result<AuthSchema.UserDetail>.Success(userDetail, $"{user.Pseudonym}님의 정보가 수정되었습니다.");
    }

    /// <summary>
    ///     회원삭제 (관리자)
    /// </summary>
    public async Task<Result> DeleteAccountByAdminAsync([FromBody] AuthSchema.DeleteAccountByAdmin request, string adminId)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
            return Result.Failure(RsCode.NotFound, "존재하지 않는 중생(사용자)입니다.");

        // * 관리자 본인 셀프 차단 방지 (매우 중요!)
        if (user.Id.ToString().Equals(adminId))
            return Result.Failure(RsCode.Error, "자신을 비활성화할 수는 없습니다.");

        // 마지막 Admin 보호
        var adminCount = await userManager.GetUsersInRoleAsync("Admin");
        if (adminCount.Count(u => u.IsActive) <= 1)
            return Result.Failure(RsCode.Error, "마지막 관리자는 비활성화할 수 없습니다.");

        user.IsActive = false;
        // user.Pseudonym = $"{user.Pseudonym}";
        // 1. 핵심: 타인이 이 필명을 재사용할 수 있도록 고유 식별자 조합으로 변경
        // 예: 기존 필명에 _del_사용자ID를 붙여서 UNIQUE 충돌을 피하고 기존 필명을 해방시킴
        user.Pseudonym = $"{user.Pseudonym}_del_{user.Id}";
        user.UpdatedAt = DateTime.UtcNow;

        // await userManager.UpdateAsync(user);
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result.Failure(RsCode.Error, "계정 비활성화 중 필명 처리 실패");

        // 3. ASP.NET Core Identity 의 Lockout 기능을 활용
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        // 4. 보안 스탬프 갱신 (중요!)
        // 현재 로그인되어 있는 불량 회원의 쿠키/토큰이 무료화 되어 즉시 튕겨 나감.
        await userManager.UpdateSecurityStampAsync(user);
        return Result.Success($"{user.Pseudonym} 님의 계정을 성공적으로 비활성화 필명을 해제했습니다.");
    }

    /// <summary>
    ///     데이터 목록 (오프셋)
    /// </summary>
    public async Task<OffsetPagedResult<AuthSchema.UserDetail>> GetUserPagedListAsync(OffsetPagedQuery request)
    {
        var query = userManager.Users.AsNoTracking().Where(u => u.IsActive && u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
        {
            var s = request.SearchKeyword.ToLower();
            query = query.Where(u => u.Pseudonym.Contains(s) || u.Email!.Contains(s));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToDTO()
            .ToListAsync();

        // ── 표준화된 PagedResult 사용 ──
        return OffsetPagedResult<AuthSchema.UserDetail>.Success(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize,
            "전체 회원 목록을 성공적으로 가져왔습니다."
        );
    }

    /// <summary>
    ///     데이터 목록 (무한스크롤)
    /// </summary>
    public async Task<CursorPagedResult<AuthSchema.UserDetail>> GetUserCursorListAsync(CursorPagedQuery request)
    {
        request.Size = Math.Clamp(request.Size, 1, 100);
        var query = userManager.Users.AsNoTracking().Where(u => u.IsActive && u.DeletedAt == null);

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            var (cursorId, cursorDate) = DecodeUserCursor(request.Cursor);
            query = query.Where(u => u.CreatedAt < cursorDate ||
                                     (u.CreatedAt == cursorDate && u.Id.CompareTo(cursorId) < 0));
        }

        // Size + 1을 가져와서 HasMore 판단
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Take(request.Size + 1)
            .ToDTO()
            .ToListAsync();

        var hasMore = items.Count > request.Size;
        if (hasMore) items.RemoveAt(request.Size);

        var nextCursor = hasMore ? EncodeUserCursor(items.Last()) : null;
        // var totalCount = await query.CountAsync();
        return CursorPagedResult<AuthSchema.UserDetail>.Success(
            items,
            nextCursor,
            hasMore,
            0,
            $"무한 스크롤 회원 목록을 가져왔습니다. {request.Size}"
        );
    }

    /// <summary>
    ///     회원권한 변경 (관리자, Admin)
    /// </summary>
    public async Task<Result> UpdateUserRolesAsync(string userId, AuthSchema.UpdateUserRoles request)
    {
        if (!Guid.TryParse(userId, out var guidId) || guidId != request.Id)
            return Result.Failure(RsCode.BadRequest, "잘못된 요청입니다.");

        // [추가] 2. 셀프 수정 방어 (관리자가 자기 권한을 바꾸려 할 때)
        // 현재 요청을 보낸 유저의 ID를 가져오는 로직이 필요 (예: IHttpContextAccessor 사용)
        var currentOperatorId = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == currentOperatorId)
            return Result.Failure(RsCode.BadRequest, "관리자 본인의 권한은 직접 수정할 수 없습니다. 다른 관리자에게 요청하세요.");

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
            return Result.Failure(RsCode.NotFound, "해당 사용자가 존재하지 않습니다.");

        await using var trn = await buddhamDbContext.Database.BeginTransactionAsync();

        // 1. 현재 권한 조회 및 삭제
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) throw new Exception("권한 삭제 실패");
        }

        // 2. 새로운 권한 부여
        var addResult = await userManager.AddToRolesAsync(user, request.Roles);
        if (!addResult.Succeeded) throw new Exception("권한 부여 실패");

        // 3. 모든 작업이 성공하면 확정(Commit)
        await trn.CommitAsync();
        return Result.Success("회원권한이 변경되었습니다.");
    }

    /// <summary>
    ///     필명 중복 체크 (회원 가입용)
    /// </summary>
    public async Task<bool> CheckPseudonymAsync(AuthSchema.CheckPseudonym request)
    {
        return await userManager.Users.AnyAsync(x => x.Pseudonym.Equals(request.Pseudonym));
    }

    /// <summary>
    ///     필명 중복 체크 (회원 필명 수정용)
    /// </summary>
    public async Task<bool> CheckPseudonymAuthAsync(AuthSchema.CheckPseudonym request, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return true;
        if (user.Pseudonym.Equals(request.Pseudonym)) return false;
        return await userManager.Users.AnyAsync(x => x.Pseudonym.Equals(request.Pseudonym));
    }

    /// <summary>
    /// 필명 수정 (회원 및 관리자 용)
    /// </summary>
    public async Task<bool> UpdatePseudonymAuthAsync(string userId, AuthSchema.UpdatePseudonym request)
    {
        if (!userId.Equals(request.Id)) return false;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        if (user.Pseudonym.Equals(request.Pseudonym)) return false;
        user.Pseudonym = request.Pseudonym;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return true;
    }

    /// <summary>
    /// 법명/불명 수정
    /// </summary>
    public async Task<bool> UpdateFullNameAuthAsync(string id, AuthSchema.UpdateFullName request)
    {
        if (!id.Equals(request.Id)) return false;
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return false;
        if (user.FullName.Equals(request.FullName)) return false;
        user.FullName = request.FullName;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return true;
    }

    /// <summary>
    /// 이메일 중복체크 (회원 가입용)
    /// </summary>
    public async Task<bool> CheckEmailAsync(AuthSchema.CheckEmail request)
    {
        return await userManager.Users.AnyAsync(x => x.Email == request.Email);
    }

    /// <summary>
    /// 비밀번호 분실시 재설정
    /// </summary>
    public async Task<Result> ResetPasswordAsync(AuthSchema.ResetPassword reuest)
    {
        var user = await userManager.FindByEmailAsync(reuest.Email);
        if (user is null)
            return Result.NotFound("가입되지 않은 이메일입니다. 다시 한번 확인해주시겠어요? 🙏"); // 조금 더 따뜻하게!
        try
        {
            // 토큰 정화 작업
            var cleanToken = reuest.Token.Trim();
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(cleanToken));

            var result = await userManager.ResetPasswordAsync(user, decodedToken, reuest.NewPassword);

            if (!result.Succeeded)
            {
                // 시스템 에러를 사람이 읽기 편한 말로 순화
                var errorMessage = result.Errors.Any()
                    ? "입력하신 비밀번호가 규칙에 맞지 않는 것 같아요."
                    : "재설정 시간이 만료되었습니다. 다시 시도해주세요.";
                return Result.Failure(RsCode.Error, errorMessage);
            }

            // 비밀번호가 바뀌었으니 보안 스탬프를 업데이트하여 이전 세션을 정리할지 말지 결정!
            // await userManager.UpdateSecurityStampAsync(user);
            return Result.Success("새로운 비밀번호로 무사히 변경되었습니다. 이제 편안히 수행하세요.");
        }
        catch (Exception ex)
        {
            return Result.Failure(RsCode.Error, $"처리 중에 잠시 문제가 생겼습니다. 잠시 후 다시 시도해주세요.{ex.Message}");
        }
    }

    /// <summary>
    ///     비밀번호 강제 변경 (관리자용)
    ///     관리자가 회원의 비밀번호를 초기화하거나 강제로 변경할 때 사용합니다.
    /// </summary>
    public async Task<Result> ForceUpdatePasswordAsync(ApplicationUser user, string newPassword)
    {
        // 1. 기존 비번 제거
        await userManager.RemovePasswordAsync(user);

        // 2. 새 비번 추가
        var result = await userManager.AddPasswordAsync(user, newPassword);

        if (!result.Succeeded) return Result.Failure(RsCode.BadRequest, "변경 실패!");
        return Result.Success("새로운 비밀번호로 성불하소서!");
    }

    /// <summary>
    ///     비밀번호 확인 (회원전용)
    /// </summary>
    public async Task<Result> CheckPasswordAsync(AuthSchema.CheckPassword request, ApplicationUser user)
    {
        // 진짜 비밀번호 체크
        var isMatch = await userManager.CheckPasswordAsync(user, request.Password);

        if (!isMatch)
        {
            // 실패 횟수 증가 (내부적으로 AccessFailedCount 가 올라감)
            await userManager.AccessFailedAsync(user);
            if (await userManager.IsLockedOutAsync(user))
            {
                return Result.Failure(RsCode.Forbidden, "비밀번호 5회 오류로 계정이 일시적으로 잠겼습니다.");
            }

            // 4. 보안 팁: 너무 자주 틀리면 계정을 잠그거나 지연을 주는 로직을 고려할 수 있음
            var failedCount = await userManager.GetAccessFailedCountAsync(user);
            return Result.Failure(RsCode.BadRequest, $"비밀번호가 일치하지 않습니다. (누적 {failedCount}회 실패)");
        }

        // 5. 성공 시 실패 횟수 초기화 (중요! 자비로운 지배자의 용서)
        await userManager.ResetAccessFailedCountAsync(user);

        // 성공시 다음 단계를 위한 '일회성 확인 토큰) 등을 반환하면 더 있어 보임
        return Result.Success("본인 확인이 완료 되었습니다.");
    }


    /// <summary>
    ///     비밀번호 변경 (회원전용)
    /// </summary>
    public async Task<Result> ChangePasswordAsync(AuthSchema.ChangePassword request, ApplicationUser user)
    {
        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(RsCode.BadRequest, "비밀번호 변경에 실패하였습니다.");
        return Result.Success("비밀번호 변경외 완료되었습니다.");
    }

    public async Task<Result<List<AuthSchema.UserDetail>>> GetusersByRoleAsync(string? roleName)
    {
        if (roleName is null)
            return Result<List<AuthSchema.UserDetail>>
                .NotFound("역할 이름이 유효하지 않습니다.");

        var usersInRole = await userManager.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.DeletedAt == null)
            // 이 사용자의 중간다리 (UserRoles) 중
            // 해당 역할과 일치하는 항목이 하나라도 있는가?.
            .Where(u => u.UserRoles
                .Any(ur => ur.Role.Name == roleName)).ToDTO()
            .ToListAsync();
        return Result<List<AuthSchema.UserDetail>>.Success(usersInRole, $"'{roleName}'");
    }

    // ── Cursor 인코딩/디코딩 (Base64) ────────────────────────

    private static string EncodeUserCursor(AuthSchema.UserDetail last)
    {
        var raw = $"{last.Id}|{last.CreatedAt:O}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static (long Id, DateTime CreateAt) DecodeUserCursor(string cursor)
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|');
        return (long.Parse(parts[0]), DateTime.Parse(parts[1]));
    }
}


/// <summary>
///     ClaimsPrincipal - 신분증 꾸러미
///     Claims - 신분증에 적힌 항목들(이름, 이메일, 역할등)
///     Identity - 신분증 그 자체 (운저면허증, 주민등록증)
///     Principal = 이 신분증릉 들고 있는 주체(사람)
/// </summary>
// public static async Task<Result<object>> GetClaimsAsync(ClaimsPrincipal user)
// {
//     var allClaims = user.Claims.Select(x => new { x.Type, x.Value }).ToList();
//     var methods = new
//     {
//         Sub = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
//         NameId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
//         Email = user.FindFirst(ClaimTypes.Email)?.Value,
//         Name = user.FindFirst(ClaimTypes.Name)?.Value,
//         Upn = user.FindFirst(ClaimTypes.Upn)?.Value,
//         Role = user.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList(),
//         IdentityName = user.Identity?.Name,
//         user.Identity?.IsAuthenticated
//     };
//
//     return Result<object>.Success(new { AllClaims = allClaims, Method = methods }, null, "");
// }
