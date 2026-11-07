using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Extensions.Persistence;

public static class DbContextPersistenceExtensions
{
    /// <summary>
    /// SaveChanges의 공통 예외 변환.
    /// 성공은 null, 실패는 Result를 반환한다.
    /// </summary>
    public static async Task<Result?> TrySaveChangeAsync(
        this DbContext context,
        ILogger logger,
        string entityDisplayName,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);
        try
        {
            await context.SaveChangesAsync(ct);
            return null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "{Entity} concurrency conflict.", entityDisplayName);
            return Result.Conflict("다른 사용자가 먼저 수정하거나 삭제했습니다. 새로 고침 후 다시 시도해주세요.");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "{Entity} database update failed.", entityDisplayName);
            return Result.Failure(RsCode.Error, "데이터 저장 중 오류가 발생했습니다.");
        }
    }
}
