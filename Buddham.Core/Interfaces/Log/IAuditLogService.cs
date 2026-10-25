namespace Buddham.Core.Interfaces.Log;

public interface IAuditLogService
{
    /// <summary>
    /// 감사로그
    /// </summary>
    Task LogAsync(
        Guid userId,
        string userEmail,
        string actionType,
        string category,
        string? message = null,
        object? oldValue = null,
        object? newValue = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestPath = null,
        string? httpMethod = null,
        bool isSuccess = true // ✅ 추가! 기본값 true라 기존 호출부 수정 불필요
    );

    /// <summary>
    /// 실패 로그를 기록 (isSuccess = false 고정)
    /// </summary>
    Task LogFailureAsync(
        Guid userId,
        string userEmail,
        string actionType,
        string category,
        string message,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestPath = null,
        string? httpMethod = null
    );
}
