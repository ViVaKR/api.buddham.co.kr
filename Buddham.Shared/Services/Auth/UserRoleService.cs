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

public class UserRoleService(
    BuddhamDbContext context,
    RoleManager<ApplicationRole> roleManager) : IUserRoleService
{
    /// <summary>
    /// GET List ALL
    /// AsSummary()
    /// </summary>
    public async Task<Result<IEnumerable<UserRoleSchema.Response>>> GetAsync()
    {
        var query = context.Roles.AsNoTracking();
        var res = await query.AsSummary().ToListAsync();
        return Result<IEnumerable<UserRoleSchema.Response>>.Success(res, null, "OK");
    }

    /// <summary>
    /// Cursor List
    /// AsSummary()
    /// </summary>
    public async Task<CursorPagedResult<UserRoleSchema.Response>> GetCursorListAsync(
        CursorPagedQuery query,
        CancellationToken ct)
    {
        // 기본 NoTracking 쿼리 장착
        // 대청소 필터가 걸려있다면 자동으로 Soft Delete 방어
        var baseQuery = context.Roles.AsNoTracking().AsQueryable();

        // ILike 검색 필터
        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var k = query.SearchKeyword.Trim();
            baseQuery = baseQuery.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Name) && EF.Functions.ILike(x.Name, $"%{k}%")) ||
                (x.KoName != null && EF.Functions.ILike(x.KoName, $"%{k}%")) ||
                (x.NormalizedName != null && EF.Functions.ILike(x.NormalizedName, $"%{k}%")) ||
                (x.Description != null && EF.Functions.ILike(x.Description, $"%{k}%"))
            );
        }

        // [성능 향상 포인트]
        // 전체 카운트는 검색어가 있거나 첫 페이지 조회일 때만 가볍게 계산
        var totalCount = 0;
        if (string.IsNullOrWhiteSpace(query.Cursor))
            totalCount = await baseQuery.CountAsync(ct);

        // [커서 페이징의 핵심 방어막] 조건절 조율
        // 최신 순 정렬(CreatedAt 이나 UpdatedAt 기준) 시 동일 밀리초 데이터가 있을 수 있으므로 고유 키(Id)와 결합!
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var (cursorTicks, cursorId) = CursorToken.Parse(query.Cursor);
            if (cursorTicks > 0 && cursorId != Guid.Empty)
            {
                var cursorDateTime = new DateTime(cursorTicks, DateTimeKind.Utc);

                // 마지막으로 본 날짜보다 과거이거나, 날짜가 같으면 마지막으로 본 ID보다 문자열 순서가 뒤인 녀석만!"
                // 🔥 [Guid 맞춤형 조건절] 
                // EF Core가 PostgreSQL에서 가장 아름다운 SQL을 뽑아내도록 유도!
                baseQuery = baseQuery.Where(x =>
                    x.CreatedAt < cursorDateTime ||
                    (x.CreatedAt == cursorDateTime && x.Id > cursorId));
            }
        }

        // 4. 🔥 정렬 핏 고정 (날짜 내림차순, Id 오름차순으로 완벽한 고유 순서 보장)
        // 💡 다음 페이지 존재 여부(HasMore) 판별을 위해 요청 사이즈보다 1개 더 땡겨오기(Size + 1)!! 
        var rawItems = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Take(query.Size + 1)
            .ToListAsync(ct);

        // 5. 다음 페이지가 진짜 있는지 1개 더 땡겨온 결과로 판정!
        var hasMore = rawItems.Count > query.Size;

        // 진짜 보낼 데이터만 싹 잘라내기
        var executionItems = rawItems.Take(query.Size).AsQueryable();

        // 6. 앵귤러 항구로 보낼 DTO 매핑 처리
        var mappedResponses = executionItems.AsSummary().ToList();

        // 7. 🚀 다음 앵귤러가 들고 올 'NextCursor' 토큰 제조 
        string? nextCursor = null;
        if (hasMore && executionItems.Any())
        {
            var lastItem = executionItems.Last();
            // 마지막 데이터의 정렬 기준 시간과 Id를 바인딩하여 캡슐화!
            nextCursor = CursorToken.Create(lastItem.CreatedAt, lastItem.Id);
        }

        // 8. 명품 보따리에 담아서 리턴
        return CursorPagedResult<UserRoleSchema.Response>.Success(
            mappedResponses,
            nextCursor,
            hasMore,
            totalCount > 0 ? totalCount : mappedResponses.Count,
            "커서 목록을 성공적으로 전송했습니다."
        );
    }

    /// <summary>
    /// Offset List
    /// AsSummary()
    /// </summary>
    public async Task<OffsetPagedResult<UserRoleSchema.Response>> GetOffsetListAsync(OffsetPagedQuery query,
        CancellationToken ct)
    {
        var baseQuery = context.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var k = query.SearchKeyword.Trim();
            // EF Core SQL (WHERE... ILIKE...) 을 생성 하도록 유도
            // ILike : 대소문자 구분없음.
            baseQuery = baseQuery.Where(x =>
                (x.Name != null && EF.Functions.ILike(x.Name, $"%{k}%")) ||
                (x.KoName != null && EF.Functions.ILike(x.KoName, $"%{k}%")) ||
                (x.Description != null && EF.Functions.ILike(x.Description, $"%{k}%"))
            );
        }

        baseQuery = baseQuery.OrderBy(x => x.Name);
        var totalCount = await baseQuery.CountAsync(ct);
        var content = await baseQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .AsSummary()
            .ToListAsync(ct);

        return OffsetPagedResult<UserRoleSchema.Response>
            .Success(content, totalCount, query.PageNumber, query.PageSize);
    }

    /// <summary>
    /// Detail 단건조회
    /// AsDetail()
    /// </summary>
    public async Task<Result<UserRoleSchema.Response>> GetUserInfoByAdminAsync(string id)
    {
        var entity = await roleManager.FindByIdAsync(id);
        if (entity == null)
            return Result<UserRoleSchema.Response>.NotFound();
        var payload = entity.AsDetail();
        return Result<UserRoleSchema.Response>.Success(payload, null, "OK");
    }

    public async Task<Result<UserRoleSchema.Response>> GetUserInfoAsync(string userId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create Role
    /// 생성
    /// </summary>
    public async Task<Result> CreateAsync(UserRoleSchema.Request request, CancellationToken ct)
    {
        var exists = await roleManager.RoleExistsAsync(request.Name);
        if (exists) return Result.Conflict("역할이 중복되었습니다.");
        var entity = request.ToEntity();
        var result = await roleManager.CreateAsync(entity);
        return !result.Succeeded
            ? Result.Failure(RsCode.Error, "역할생성 중 오류발생")
            : Result.Success();
    }

    /// <summary>
    /// Update Role
    /// 업데이트
    /// </summary>
    public async Task<Result> UpdateAsync(string id, UserRoleSchema.Request request, CancellationToken ct)
    {
        if (request.Id == null && !id.Equals(request.Id.ToString()))
            return Result.Failure(RsCode.BadRequest, "잘못된 요청입니다.");
        var role = await roleManager.FindByIdAsync(id);

        if (role == null)
            return Result.Failure(RsCode.Error, "역할이 없습니다.");

        role.ApplyPatch(request);
        role.NormalizedName = roleManager.NormalizeKey(role.Name);

        try
        {
            var result = await roleManager.UpdateAsync(role);
            return !result.Succeeded ? Result.Failure(RsCode.Error, "역할 수정중 오류발생") : Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Conflict($"다른 사용자가 이미 이 역할 정보를 수정했습니다. 화면을 새로고침한 후 다시 시도해 주세요.{ex.Message}");
        }
    }

    public async Task<Result> RestoreAsync(string id)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Delete
    /// 역할 정보 소프트 삭제 (50세기형 하이테크 버전)
    /// </summary>
    public async Task<Result> DeleteAsync(string id)
    {
        // 1. 삭제할 대상을 최신 버전으로 땡겨온다!
        var role = await roleManager.FindByIdAsync(id);

        if (role is null)
            return Result.NotFound("삭제하려는 역할 정보를 찾을 수 없습니다.");

        // 🚨 만약 이미 소프트 삭제된 녀석이라면? 중복 삭제 방어막!
        if (role.DeletedAt != null)
            return Result.Conflict("이미 삭제 처리된 역할입니다.");

        // ========================================================
        // 🔥 [50세기형 핵심 퍼즐] 삭제를 '품격 있는 상태 변경'으로 승화!
        // ========================================================
        role.DeletedAt = DateTime.UtcNow; // 1. 삭제 타임스탬프 똭! (전역 필터 가동)
        role.IsActive = false; // 2. 활성 상태 꺼버리기! (조회 인덱스 최적화)
        role.UpdatedAt = DateTime.UtcNow; // 3. 수정일도 동기화

        try
        {
            // 🚨 무자비한 DeleteAsync 대신 'UpdateAsync'를 호출하여 소프트 삭제 안착!
            // 내부적으로 PostgreSQL의 xmin(RowVersion)도 새로운 트랜잭션 ID로 갱신되면서 
            // 완벽한 동시성 제어까지 한틀에 묶여 돌아간다네!
            var result = await roleManager.UpdateAsync(role);

            return !result.Succeeded
                ? Result.Failure(RsCode.Error, "역할 삭제(소프트) 중 오류가 발생했습니다.")
                : Result.Success("역할이 안전하게 삭제(보관)되었습니다.");
        }
        catch (DbUpdateConcurrencyException)
        {
            // 💥 삭제 버튼 누르는 그 찰나의 순간에 다른 중생이 수정하거나 먼저 삭제했다면?
            return Result.Conflict("다른 사용자가 이미 이 역할 정보를 변경했거나 삭제했습니다. 다시 확인해 주세요.");
        }
    }
}
