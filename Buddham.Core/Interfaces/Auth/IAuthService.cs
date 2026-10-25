using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Auth;

/// <summary>
/// 인증 서비스 (로그인 / 로그아웃 / 토큰 / 가입 / 탈퇴)
/// </summary>
public interface IAuthService
{
    // ══════════════════════════════════════════
    // 🔐 로그인 / 로그아웃
    // ══════════════════════════════════════════

    /// <summary>로그인</summary>
    Task<Result<AuthSchema.Identity>> SignInAsync(AuthSchema.SignIn dto);

    /// <summary>로그아웃</summary>
    Task<Result> SignOutAsync(string userId);

    /// <summary>토큰 갱신</summary>
    Task<Result<AuthSchema.Identity>> RefreshTokenAsync(AuthSchema.TokenRefresh dto);

    // ══════════════════════════════════════════
    // 📝 회원가입 / 탈퇴
    // ══════════════════════════════════════════

    /// <summary>회원가입</summary>
    Task<Result> SignUpAsync(AuthSchema.SignUp request);

    /// <summary>회원탈퇴 (본인)</summary>
    Task<Result> WithdrawAsync(AuthSchema.Withdraw request, string userId);

    /// <summary>회원삭제 (관리자)</summary>
    Task<Result> DeleteAccountByAdminAsync(AuthSchema.DeleteAccountByAdmin request, string adminId);


    /// <summary> 회원정보 </summary>
    Task<Result<AuthSchema.UserDetail>> GetUserDetailAsync(string userId);


    //==============//

    // * 본인정보 수정
    Task<Result<AuthSchema.UserDetail>> UpdateAccountBySelfAsync(string userId, AuthSchema.UpdateUser request);

    // * 전체 회원목록 (관리자 용)
    Task<Result<IEnumerable<AuthSchema.UserDetail>>> GetUserListAsync();

    // * 사용자 목록 (오프셋 방식)
    Task<OffsetPagedResult<AuthSchema.UserDetail>> GetUserPagedListAsync(OffsetPagedQuery request);

    // * 사용자 목록 (커서 방식)
    Task<CursorPagedResult<AuthSchema.UserDetail>> GetUserCursorListAsync(CursorPagedQuery request);

    // * 사용자 권한 변경 (관리자 용)
    Task<Result> UpdateUserRolesAsync(string userId, AuthSchema.UpdateUserRoles request);

    // * 필명 중복 체크 (회원 가입용)
    Task<bool> CheckPseudonymAsync(AuthSchema.CheckPseudonym request);

    // * 필명 중복 체크 (회원 필명 수정용)
    Task<bool> CheckPseudonymAuthAsync(AuthSchema.CheckPseudonym request, string userId);

    // * 필명 수정 (회원 및 관리자 용)
    Task<bool> UpdatePseudonymAuthAsync(string id, AuthSchema.UpdatePseudonym request);

    // * 법명 수정 (회원 및 관리자 용)
    Task<bool> UpdateFullNameAuthAsync(string id, AuthSchema.UpdateFullName request);

    // * 이메일
    Task<bool> CheckEmailAsync(AuthSchema.CheckEmail request);

    // * 비밀번호 분실시 재설정
    Task<Result> ResetPasswordAsync(AuthSchema.ResetPassword reuest);

    // * 관리자 강제 비밀번호 변경
    Task<Result> ForceUpdatePasswordAsync(ApplicationUser user, string newPassword);

    // * 비밀번호 확인
    Task<Result> CheckPasswordAsync(AuthSchema.CheckPassword request, ApplicationUser user);

    // * 비밀번호 변경 (회원전용)
    Task<Result> ChangePasswordAsync(AuthSchema.ChangePassword request, ApplicationUser user);
}
