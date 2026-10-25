using Buddham.Core.Common.Results;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Auth;

/// <summary>
/// 관리자 서비스 (회원목록 / 권한 / 통계)
/// </summary>
public interface IAdminService
{
    // ══════════════════════════════════════════
    // 👥 회원 관리
    // ══════════════════════════════════════════

    /// <summary>회원 목록 조회</summary>
    Task<Result<IEnumerable<AuthSchema.UserDetail>>> GetUserListAsync();

    /// <summary>회원 권한 수정</summary>
    Task<Result> UpdateUserRolesAsync(string userId, AuthSchema.UpdateUserRoles dto);

    // ══════════════════════════════════════════
    // 📊 통계
    // ══════════════════════════════════════════

    /// <summary>총 회원수 (캐시 5분)</summary>
    Task<Result<int>> GetTotalAccountAsync(); 
}
