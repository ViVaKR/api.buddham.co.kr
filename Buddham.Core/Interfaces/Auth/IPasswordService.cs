using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Auth;

/// <summary>
/// 비밀번호 서비스 (변경 / 재설정 / 확인)
/// </summary>
public interface IPasswordService
{
    // ══════════════════════════════════════════
    // 🔑 비밀번호 재설정 (비회원)
    // ══════════════════════════════════════════

    /// <summary>비밀번호 분실 - 재설정 메일 발송</summary>
    Task<Result<EmailSchema.Identity>> ForgotPasswordAsync(EmailSchema.ForgotPassword dto);

    /// <summary>비밀번호 재설정 (임시토큰 포함)</summary>
    Task<Result> ResetPasswordAsync(AuthSchema.ResetPassword dto);

    /// <summary>비밀번호 강제 재설정 (관리자)</summary>
    Task<Result> ForceResetPasswordAsync(AuthSchema.ResetPassword dto);

    // ══════════════════════════════════════════
    // 🔒 비밀번호 관리 (회원)
    // ══════════════════════════════════════════

    /// <summary>비밀번호 확인 (중요 작업 전 본인 확인)</summary>
    Task<Result> CheckPasswordAsync(AuthSchema.CheckPassword dto, ApplicationUser user);

    /// <summary>비밀번호 변경 (회원)</summary>
    Task<Result> ChangePasswordAsync(AuthSchema.ChangePassword dto, ApplicationUser user); 
}
