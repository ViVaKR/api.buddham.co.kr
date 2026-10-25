using Buddham.Core.Common;
using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Schemas;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Auth;

public class UserService(
    BuddhamDbContext context,
    UserManager<ApplicationUser> userManager
) : IUserService
{
    /// <summary>
    /// Get List ALL
    /// AsSummary()
    /// </summary>
    public async Task<Result<IEnumerable<UserSchema.Response>>> GetAsync()
    {
        var users = await context.Users.AsNoTracking()
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
            .ThenInclude(Ur => Ur.Role) // 명시적 Include! 
            .AsSplitQuery()
            .AsSummary()
            .ToListAsync();
        return Result<IEnumerable<UserSchema.Response>>.Success(users, null, "OK");
    }

    /// <summary>
    /// [커서 기반 페이징] 사용자 목록 조회 서비스
    /// Cursor List
    /// AsSummary()
    /// </summary>
    /// <remarks>
    /// 1. <see cref="EF.Functions.ILike"/>와 PostgreSQL의 <c>pg_trgm</c> 인덱스를 활용한 고속 대소문자 무시 검색을 지원합니다.<br/>
    /// 2. <c>CreatedAt</c>(시간)과 <c>SequentialGuid</c>(고유키)의 결합을 통해 동일 밀리초 데이터 간의 정렬 꼬임 현상을 방지합니다.<br/>
    /// 3. <c>Take(Size + 1)</c> 기법을 사용하여 다음 페이지 존재 여부를 별도의 Count 쿼리 없이 효율적으로 판정합니다.
    /// </remarks>
    /// <param name="query">검색 키워드 및 커서(Cursor Token) 정보를 담은 쿼리 객체</param>
    /// <param name="ct">비동기 작업 취소를 위한 CancellationToken</param>
    /// <returns>매핑된 사용자 응답 스키마 리스트와 다음 커서 토큰을 포함한 페이징 결과</returns>
    public async Task<CursorPagedResult<UserSchema.Response>> GetCursorListAsync(
        CursorPagedQuery query,
        CancellationToken ct)
    {
        // [성능 최적화] Tracking 이 필요 없는 단순조회 이므로 AsNoTracking()을 명시
        // EF Core 컨텍스트 메모리 오버헤드 최소화
        var baseQuery = context.Users
            .IgnoreQueryFilters()
            .AsNoTracking();

        // ── 1단계: 🔍 검색 조건절 (Search Keyword Filter) ──────────────────────────────────
        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var k = query.SearchKeyword.Trim();

            // PostgreSQL의 ILIKE 연산자로 번역되어 대소문자 구분 없이 검색을 수행합니다.
            // ※ 주의: 인프라 레벨에서 관련 컬럼들에 pg_trgm GIN 인덱스가 생성되어 있어야 대용량 데이터에서 Scan 성능이 보장됩니다.
            // ※ 팁: string.IsNullOrWhiteSpace(x.FullName) 조건은 EF Core 번역 시 불필요한 IS NOT NULL 조건을 양산할 수 있으므로, 
            //       DB 컬럼이 널 허용(Nullable)이 아니라면 EF.Functions.ILike 만 남겨두는 것이 SQL이 더 깔끔해집니다.
            baseQuery = baseQuery.Where(x =>
                EF.Functions.ILike(x.FullName, $"%{k}%") ||
                (!string.IsNullOrWhiteSpace(x.Email) && EF.Functions.ILike(x.Email, $"%{k}%")) ||
                EF.Functions.ILike(x.Pseudonym, $"%{k}%") ||
                (!string.IsNullOrWhiteSpace(x.Introduction) && EF.Functions.ILike(x.Introduction, $"%{k}%"))
            );
        }

        var totalCount = 0;

        // ── 2단계: 📊 전체 카운트 계산 (선택적 최적화) ──────────────────────────────────
        // 무한 스크롤(커서 페이징)의 핵심은 전체 카운트를 매번 안 세는 것이지만, 
        // 첫 페이지 진입 시점에 한해서 전체 데이터 개수를 UI에 보여주기 위해 선택적으로 Count를 수행합니다.
        if (string.IsNullOrWhiteSpace(query.Cursor))
        {
            totalCount = await baseQuery.CountAsync(ct);
        }

        // ── 3단계: 🚨 [커서 페이징의 핵심 방어막] 조건절 조율 ─────────────────────────────
        // 최신순 정렬(CreatedAt 기준) 시 동일 밀리초에 생성된 데이터가 존재할 수 있으므로,
        // 시간 조건이 같을 경우 고유 키인 Id(SequentialGuid)의 사전식 대소 비교를 결합하여 데이터 누락/중복을 완벽히 방지합니다.
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var (cursorTicks, cursorId) = CursorToken.Parse(query.Cursor);
            if (cursorTicks > 0 && cursorId != Guid.Empty)
            {
                var cursorDateTime = new DateTime(cursorTicks, DateTimeKind.Utc);
                baseQuery = baseQuery.Where(x => x.CreatedAt < cursorDateTime
                                                 || (x.CreatedAt == cursorDateTime && x.Id > cursorId));
            }
        }

        // ── 4단계: 🏎️ 데이터 추출 및 다음 페이지 판정 (Take Size + 1) ──────────────────────
        // 요청된 Size보다 '딱 1개 더' 가져옵니다. (예: 20개 요청 시 21개 조회)
        // 21번째 데이터가 실제로 존재한다면 "다음 페이지가 있다(hasMore = true)"고 판정하는 기가 막힌 트릭입니다.
        var rawItems = await baseQuery
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role) // 여기가 최선
            .OrderByDescending(x => x.CreatedAt)
            .Take(query.Size + 1)
            .ToListAsync(ct);

        // 가져온 데이터 수가 요청 수보다 많다면 다음 페이지가 있는 것임
        var hasMore = rawItems.Count > query.Size;

        // UI에 돌려줄 데이터는 요청한 딱 그 사이즈만큼만 자름
        var executionItems = rawItems.Take(query.Size).AsQueryable();

        // 엔티티 모델을 화면용 Response DTO(Schema)로 최종 변환 (AutoMapper 또는 커스텀 확장 메서드 활용)
        var mappedResponses = executionItems.AsSummary().ToList();


        string? nextCursor = null;

        // ── 5단계: 🛑 예외 케이스 방어막 및 조기 반환 ──────────────────────────────────────
        // 더 가져올 데이터가 없거나, 현재 페이지에 데이터가 아예 없다면 다음 커서 없이 즉시 성공 반환
        if (!hasMore || mappedResponses.Count == 0)
            return CursorPagedResult<UserSchema.Response>
                .Success(mappedResponses, nextCursor, hasMore, totalCount, "커서 목록을 전송했습니다.");

        // ── 6단계: 🎟️ 다음 커서 토큰 발행 ────────────────────────────────────────────────
        // 현재 페이지의 '가장 마지막 데이터'의 시간과 고유 ID를 조합하여
        // 다음 페이지의 시작점이 될 커서 토큰을 구워냅니다.
        var lastItem = executionItems.Last();
        nextCursor = CursorToken.Create(lastItem.CreatedAt, lastItem.Id);

        return CursorPagedResult<UserSchema.Response>
            .Success(mappedResponses, nextCursor, hasMore, totalCount, "커서 목록을 전송했습니다.");
    }

    /// <summary>
    /// Offset List
    /// AsSummary()
    /// </summary>
    public async Task<OffsetPagedResult<UserSchema.Response>> GetOffsetListAsync(
        OffsetPagedQuery query,
        CancellationToken ct)
    {
        var baseQuery = context.Users.IgnoreQueryFilters().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var k = query.SearchKeyword.Trim();
            var upperK = k.ToUpper();

            baseQuery = baseQuery.Where(x =>
                EF.Functions.ILike(x.Pseudonym, $"%{k}%") ||
                EF.Functions.ILike(x.FullName, $"%{k}%") ||
                (!string.IsNullOrWhiteSpace(x.Introduction) && EF.Functions
                    .ILike(x.Introduction, $"%{k}%")) ||

                // ⚡ 2. 기본 유니크 B-Tree 인덱스를 타고 빛의 속도로 앞글자 검색할 이메일!
                // NormalizedEmail은 대문자이므로 StartsWith(upperK)를 쓰면 디비가 0.001초 검색 
                (!string.IsNullOrWhiteSpace(x.NormalizedEmail) && x.NormalizedEmail.StartsWith(upperK))
            );
        }

        baseQuery = baseQuery.OrderBy(x => x.CreatedAt);
        var totalCount = await baseQuery.CountAsync(ct);
        var content = await baseQuery
            .Include(u => u.UserRoles)
            .ThenInclude(Ur => Ur.Role) // 명시적 Include!
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .AsSummary()
            .ToListAsync(ct);

        return OffsetPagedResult<UserSchema.Response>.Success(content, totalCount, query.PageNumber, query.PageSize);
    }

    /// <summary>
    /// Detail 단건 조회
    /// AsDetail()
    /// </summary>
    public async Task<Result<UserSchema.Response>> GetUserInfoByAdminAsync(string id)
    {
        var payload = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(Ur => Ur.Role) // 명시적 Include!
            .Where(x => x.Id.Equals(Guid.Parse(id)))
            .AsSummary()
            .FirstOrDefaultAsync();

        return payload == null
            ? Result<UserSchema.Response>.NotFound()
            : Result<UserSchema.Response>.Success(payload, null, "OK");
    }

    public async Task<Result<UserSchema.Response>> GetUserInfoAsync(string userId)
    {
        var payload = await context.Users.AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(Ur => Ur.Role)
            .Where(x => x.Id.Equals(Guid.Parse(userId)))
            .AsSummary()
            .FirstOrDefaultAsync();

        return payload == null
            ? Result<UserSchema.Response>.NotFound()
            : Result<UserSchema.Response>.Success(payload, null, "OK");
    }

    /// <summary>
    /// Create 생성
    /// ToEntity()
    /// </summary>
    public async Task<Result> CreateAsync(UserSchema.Request request, CancellationToken ct)
    {
        var entity = request.ToEntity();
        await userManager.UpdateNormalizedUserNameAsync(entity);
        context.Add(entity);
        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// 수정
    /// ApplyPatch()
    /// </summary>
    public async Task<Result> UpdateAsync(string id, UserSchema.Request request, CancellationToken ct)
    {
        if (request.Id == null && !id.Equals(request.Id.ToString()))
            return Result.Failure(RsCode.BadRequest, "잘못된 요청입니다.");

        var entity = await userManager.FindByIdAsync(id);
        if (entity == null)
            return Result.Failure(RsCode.Error, "사용자가 없습니다.");

        entity.ApplyPatch(request);
        var result = await userManager.UpdateAsync(entity);
        try
        {
            return !result.Succeeded
                ? Result.Failure(RsCode.Error, "사용자 정보 수정중 오류발생")
                : Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Conflict($"다른 사용자가 이미 정보를 수정했습니다. 잠시후 다시 시도해 주세요. {ex.Message}");
        }
    }

    /// <summary>
    /// 회원 복구 (Soft Delete 복원)
    /// </summary>
    public async Task<Result> RestoreAsync(string id)
    {
        // ✅ IgnoreQueryFilters() 로 직접 조회
        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (user is null)
            return Result.NotFound("복구하려는 회원을 찾을 수 없습니다.");

        if (user.DeletedAt is null)
            return Result.Conflict("탈퇴하지 않은 회원입니다.");

        user.DeletedAt = null;
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            var result = await userManager.UpdateAsync(user);
            return !result.Succeeded
                ? Result.Failure(RsCode.Error, "회원 복구 중 오류가 발생했습니다.")
                : Result.Success("회원 복구에 성공했습니다.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Conflict($"다른 사용자가 이미 회원정보를 변경했습니다. {ex.Message}");
        }
    }

    /// <summary>
    /// DELETE 회원삭제
    /// Soft Delete
    /// </summary>
    public async Task<Result> DeleteAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return Result.NotFound("삭제하려는 회원을 찾을 수 없습니다.");

        if (user.DeletedAt != null)
            return Result.Conflict("이미 탈퇴한 회원입니다.");

        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            var result = await userManager.UpdateAsync(user);
            return !result.Succeeded
                ? Result.Failure(RsCode.Error, "회원 삭제 중 오류가 발생했습니다.")
                : Result.Success("회원 탈퇴에 성공했습니다.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Conflict($"다른 사용자가 이미 회원정보를 삭제 했습니다.  {ex.Message}");
        }
    }

    /// <summary>
    /// 관리자전용 회원목록
    /// </summary>
    public async Task<Result<IEnumerable<UserSchema.Response>>> GetUsersForAdminAsync(CancellationToken ct)
    {
        // OnModelCreating에서 걸어두었던 'DeletedAt == null' 전역 필터가 완벽하게 우회(무시)됩니다.
        var users = await context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
            .ThenInclude(Ur => Ur.Role)
            .AsSplitQuery()
            .AsSummary()
            .ToListAsync(ct);

        return Result<IEnumerable<UserSchema.Response>>.Success(users, null, "전지적 신의 눈으로 전체 유저 조회 성공");
    }

    // public async Task<Result<IEnumerable<UserSchema.Response>>> GetAsync()
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<CursorPagedResult<UserSchema.Response>> GetCursorListAsync(CursorPagedQuery query, CancellationToken ct)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<OffsetPagedResult<UserSchema.Response>> GetOffsetListAsync(OffsetPagedQuery query, CancellationToken ct)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<CursorPagedResult<UserSchema.Response>> GetSemanticListAsync(SemanticSearchQuery query, CancellationToken ct)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result<UserSchema.Response>> GetByIdAsync(string id)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result> CreateAsync(UserSchema.Request request, CancellationToken ct)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result> UpdateAsync(string id, UserSchema.Request request, CancellationToken ct)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result> RestoreAsync(string id)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result> DeleteAsync(string id)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // public async Task<Result<IEnumerable<UserSchema.Response>>> GetUsersForAdminAsync(CancellationToken ct)
    // {
    //     throw new NotImplementedException();
    // }
}
