using Buddham.Core.Common;
using Buddham.Core.Common.Options;
using Buddham.Core.Common.Primitives;
using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces;
using Buddham.Core.Interfaces.Cache;
using Buddham.Core.Interfaces.Common;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Services.Crud;

/// <summary>
/// 캐시 + 소프트삭제 + 권한 + 검증 + 목록(전체/커서/오프셋)/상세 + 생성/수정/삭제 표준 구현.
/// 파생 클래스는 "엔티티마다 다른 부분"(매핑, 검색 필드, 정렬 컬럼, 검증 규칙)만 선언한다.
/// </summary>
public abstract class CrudService<TEntity, TRequest, TResponse>
    : IBaseService<long, TRequest, TResponse>,
      IDetailQueryableService<long, TResponse>,
      ICursorQueryable<TResponse>, IOffsetQueryable<TResponse>
    where TEntity : EntityBase
    where TRequest : class
    where TResponse : class, ICursorKeyed
{
    protected CrudService(DbContext context, CrudDependencies deps, EntityCacheOptions cacheOptions)
    {
        Context = context;
        Deps = deps;
        EntityName = cacheOptions.EntityName;
        Cache = deps.CacheFactory.Create(cacheOptions);
        Logger = deps.LoggerFactory.CreateLogger(GetType());
    }

    protected DbContext Context { get; }
    protected CrudDependencies Deps { get; }
    protected IEntityCache Cache { get; }
    protected ILogger Logger { get; }
    protected string EntityName { get; }

    // ══════════════════════════════════════════════════════════
    // 파생 클래스가 채우는 부분
    // ══════════════════════════════════════════════════════════

    /// <summary>엔티티 → 목록/상세 응답 투영 (보통 q.AsSummary()).</summary>
    protected abstract IQueryable<TResponse> Project(IQueryable<TEntity> query);

    protected abstract TEntity ToEntity(TRequest request, Guid userId, DateTime now);

    /// <summary>필드 갱신. 임베딩은 건드리지 않는다(EmbeddingCrudService 가 처리).</summary>
    protected abstract void ApplyUpdate(TEntity entity, TRequest request, DateTime now);

    /// <summary>요청 검증. request 는 null 이 아님이 보장된다. 통과하면 null.</summary>
    protected abstract Result? Validate(TRequest request);

    /// <summary>키워드 검색 대상 필드. 비우면 검색어 무시.</summary>
    protected virtual IReadOnlyList<System.Linq.Expressions.Expression<Func<TEntity, string?>>> SearchFields => [];

    /// <summary>허용 정렬 컬럼. 기본은 id 만.</summary>
    private static readonly SortMap<TEntity> DefaultSorts = new SortMap<TEntity>().Add("id", x => x.Id);
    protected virtual SortMap<TEntity> Sorts => DefaultSorts;
    // protected virtual SortMap<TEntity> Sorts { get; } = new SortMap<TEntity>().Add("id", x => x.Id);

    protected virtual bool CanModify(TEntity entity)
        => entity is IOwnedEntity owned
            ? Deps.Authorization.CanModify(owned.UserId)
            : Deps.CurrentUser.IsAdmin; // 소유자 없음 -> 관리자만

    /// <summary>생성 직전 훅. 실패를 반환하면 저장하지 않는다.</summary>
    protected virtual Task<Result?> OnCreatingAsync(TEntity entity, TRequest request, CancellationToken ct)
        => Task.FromResult<Result?>(null);

    /// <summary>수정 직전 훅(ApplyUpdate 이전, 기존 값이 아직 남아 있음). 실패를 반환하면 저장하지 않는다.</summary>
    protected virtual Task<Result?> OnUpdatingAsync(TEntity entity, TRequest request, CancellationToken ct)
        => Task.FromResult<Result?>(null);

    // ══════════════════════════════════════════════════════════
    // 공통 헬퍼
    // ══════════════════════════════════════════════════════════

    /// <summary>소프트 삭제되지 않은 데이터만. 모든 조회/수정은 반드시 이것을 통해서만 접근한다.</summary>
    protected IQueryable<TEntity> Active => Context.Set<TEntity>().Where(x => x.DeletedAt == null);

    protected DateTime UtcNow => Deps.TimeProvider.GetUtcNow().UtcDateTime;

    // ══════════════════════════════════════════════════════════
    // 조회
    // ══════════════════════════════════════════════════════════

    public virtual async Task<Result<IEnumerable<TResponse>>> GetAsync(CancellationToken ct)
    {
        var data = await Cache.GetOrCreateAsync(Cache.Key("all"),
            async token => await Project(Active.AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .Take(AppInfrastructure.MaxPageSize))
                .ToListAsync(token), ct);

        return Result<IEnumerable<TResponse>>.Success(data, null, "전체목록 반환 성공");
    }

    public virtual Task<CursorPagedResult<TResponse>> GetCursorListAsync(CursorPagedQuery query, CancellationToken ct)
        => QueryCursorPageAsync(query.Cursor, query.Size, query.SearchKeyword, query.Descending, ct);

    public virtual async Task<OffsetPagedResult<TResponse>> GetOffsetListAsync(OffsetPagedQuery query, CancellationToken ct)
    {
        // 검증은 캐시 앞에서: 잘못된 요청이 캐시 키를 만들지 않게 한다.
        Paging.EnsureOffsetPage(query.PageNumber, query.PageSize);

        var keyword = LikeSearch.Normalize(query.SearchKeyword);
        var sortBy = Sorts.Resolve(query.SortBy);

        var slice = await Cache.GetOrCreateAsync(
            Cache.Key("offset", keyword, sortBy, query.Descending, query.PageNumber, query.PageSize),
            async token =>
            {
                var baseQuery = Active.AsNoTracking().WhereAnyILike(keyword, SearchFields);
                var totalCount = await baseQuery.CountAsync(token);
                var data = await Project(Sorts.Apply(baseQuery, sortBy, query.Descending)
                        .ApplyOffset(query.PageNumber, query.PageSize))
                        .ToListAsync(token);

                return new OffsetSlice<TResponse>(data, totalCount);
            },
            ct);

        return OffsetPagedResult<TResponse>.Success(
            slice.Items, slice.TotalCount, query.PageNumber, query.PageSize, "오프셋 페이지 목록 전송완료");
    }

    public virtual async Task<Result<TResponse>> GetByIdAsync(long id, CancellationToken ct)
    {
        // 없는 Id 도 캐시한다(쓰기 시 태그 무효화로 정리되므로 안전).
        var item = await Cache.GetOrCreateNullableAsync<TResponse>(
            Cache.Key("detail", id),
            async token => await Project(Active.AsNoTracking().Where(x => x.Id == id))
                .FirstOrDefaultAsync(token),
            ct);

        return item is null
            ? Result<TResponse>.NotFound()
            : Result<TResponse>.Success(item, null, "OK");
    }

    // ══════════════════════════════════════════════════════════
    // 생성 / 수정 / 삭제  (성공 시에만 캐시 무효화)
    // ══════════════════════════════════════════════════════════

    public virtual async Task<Result> CreateAsync(TRequest request, CancellationToken ct)
    {
        if (Deps.CurrentUser.UserId is not { } userId)
            return Result.Failure(RsCode.Unauthorized, "로그인이 필요합니다.");

        if (ValidateRequest(request) is { } invalid) return invalid;

        var entity = ToEntity(request, userId, UtcNow);

        if (await OnCreatingAsync(entity, request, ct) is { } failure) return failure;

        Context.Set<TEntity>().Add(entity);

        if (await Context.TrySaveChangeAsync(Logger, EntityName, ct) is { } saveFailure) return saveFailure;

        await Cache.InvalidateAsync();
        return Result.Created();
    }

    public virtual async Task<Result> UpdateAsync(long id, TRequest request, CancellationToken ct)
    {
        if (ValidateRequest(request) is { } invalid) return invalid;

        var entity = await Active.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result.NotFound();
        if (!CanModify(entity)) return Result.Forbidden();

        if (await OnUpdatingAsync(entity, request, ct) is { } failure) return failure;

        ApplyUpdate(entity, request, UtcNow);

        if (await Context.TrySaveChangeAsync(Logger, EntityName, ct) is { } saveFailure) return saveFailure;

        await Cache.InvalidateAsync();
        return Result.Updated();
    }

    /// <summary>소프트 삭제</summary>
    public virtual async Task<Result> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await Active.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result.NotFound();
        if (!CanModify(entity)) return Result.Forbidden();

        var now = UtcNow;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;

        if (await Context.TrySaveChangeAsync(Logger, EntityName, ct) is { } saveFailure) return saveFailure;

        await Cache.InvalidateAsync();
        return Result.Success();
    }

    // ══════════════════════════════════════════════════════════
    // 내부 구현
    // ══════════════════════════════════════════════════════════

    private Result? ValidateRequest(TRequest? request)
        => request is null
            ? Result.Failure(RsCode.BadRequest, "요청 본문이 비어 있습니다.")
            : Validate(request);

    /// <summary>
    /// CreatedAt + Id 커서 페이징 (키워드 검색 선택).
    /// 잘못된 커서/크기는 팩토리 안에서 예외가 되며, 예외는 캐시되지 않는다.
    /// </summary>
    protected async Task<CursorPagedResult<TResponse>> QueryCursorPageAsync(
        string? cursor, int size, string? keyword, bool descending, CancellationToken ct)
    {
        var normalizedKeyword = LikeSearch.Normalize(keyword);

        var slice = await Cache.GetOrCreateAsync(
            Cache.Key("cursor", cursor, size, normalizedKeyword, descending),
            async token =>
            {
                var source = Active.AsNoTracking().WhereAnyILike(normalizedKeyword, SearchFields);

                return await source.ToCursorSliceAsync(
                    createdAt: x => x.CreatedAt,
                    id: x => x.Id,
                    project: q => Project(q),
                    keyOf: r => (r.CreatedAt, r.Id),
                    options: new CursorSliceOptions(
                        cursor, size, AppInfrastructure.MaxPageSize, normalizedKeyword, descending),
                    ct: token);
            },
            ct);

        return CursorPagedResult<TResponse>.Success(
            slice.Items, slice.NextCursor, slice.HasMore, slice.TotalCount ?? 0, "커서 목록을 전송했습니다.");
    }
}
