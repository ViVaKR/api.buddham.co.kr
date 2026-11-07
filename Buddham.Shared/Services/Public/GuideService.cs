using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Buddham.Core.Common;
using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Public;
using Buddham.Core.Interfaces.Common;
using Buddham.Core.Interfaces.Public;
using Buddham.Core.Schemas;
using Buddham.Shared.Constants;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Persistence;
using Buddham.Shared.Extensions.Public;
using Buddham.Shared.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Pgvector.EntityFrameworkCore;

namespace Buddham.Shared.Services.Public;

public class GuideService(
    PublicDbContext context,
    ICurrentUserService currentUser,
    IEntityAuthorizationService authorization,
    IEmbeddingService embeddingService,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<GuideService> logger
) : IGuideService
{
    private const string EntityName = "법문 가이드";

    // 필요하면 AppInfrastructure 로 옮긴다.
    private const int MaxContentLength = 100_000;

    // 벡터 검색은 Offset 에뮬레이션이라 깊은 페이지일수록 비싸다. 상한을 둔다.
    private const int MaxSemanticOffset = 1_000;

    // ══════════════════════════════════════════════════════════
    // 캐시 설정
    // ══════════════════════════════════════════════════════════

    // 응답 모양(Response)이 바뀌어 배포할 때 이전 직렬화 데이터를 읽지 않도록 버전을 올린다.
    private const string CachePrefix = "guide:v1";

    // 가이드 데이터가 바뀌면 이 태그 하나로 목록/상세 캐시를 한 번에 무효화한다.
    private const string GuideTag = "guides";
    private static readonly string[] GuideTags = [GuideTag];

    // L2(Redis) 5분 / L1(파드 메모리) 30초.
    // HybridCache 의 L1 은 파드별이라 다른 파드의 L1 은 즉시 무효화되지 않는다.
    // 따라서 L1 TTL 이 곧 "다른 파드에서 보일 수 있는 최대 지연"이다.
    private static readonly HybridCacheEntryOptions DataCacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromSeconds(30)
    };

    // 검색어 임베딩은 가이드 데이터와 무관하므로 태그 없이 길게, Redis 에만 저장한다.
    private static readonly HybridCacheEntryOptions EmbeddingCacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(24),
        Flags = HybridCacheEntryFlags.DisableLocalCache
    };

    // ══════════════════════════════════════════════════════════
    // 공통 헬퍼
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 소프트 삭제되지 않은 데이터만. 모든 조회/수정은 반드시 이것을 통해서만 접근한다.
    /// (전역 쿼리 필터를 이미 쓰고 있다면 Where 는 중복이지만 무해하다.)
    /// </summary>
    private IQueryable<Guide> ActiveGuides => context.Guides.Where(x => x.DeletedAt == null);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// 캐시 키: 파라미터를 해시해 길이/특수문자 문제를 없앤다 (파드 간 결정적).
    /// </summary>
    private static string CacheKey(string kind, params object?[] parts)
    {
        var raw = string.Join('\u001F',
            parts.Select(p => Convert.ToString(p, CultureInfo.InvariantCulture) ?? string.Empty));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return $"{CachePrefix}:{kind}:{hash}";
    }

    /// <summary>
    /// Read-through 캐시. 같은 키의 동시 미스는 한 번만 DB 로 간다(스탬피드 방지).
    /// 팩토리에서 던진 예외는 캐시되지 않는다.
    /// </summary>
    private ValueTask<T> CachedAsync<T>(
        string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken ct)
        => cache.GetOrCreateAsync(key, factory, DataCacheOptions, GuideTags, ct);

    /// <summary>
    /// 쓰기 성공 후 호출. DB 는 이미 커밋됐으므로 요청이 취소돼도 무효화는 수행하고(CancellationToken.None),
    /// 캐시 장애가 쓰기 결과를 실패로 만들지 않도록 예외는 삼킨다(TTL 이 최후의 안전망).
    /// </summary>
    private async Task InvalidateCacheAsync()
    {
        try
        {
            await cache.RemoveByTagAsync(GuideTag, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{Entity} cache invalidation failed. TTL 만료까지 이전 데이터가 보일 수 있습니다.", EntityName);
        }
    }

    // ══════════════════════════════════════════════════════════
    // 조회
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 전체 목록. 무제한 반환을 막기 위해 MaxPageSize 로 상한을 둔다.
    /// </summary>
    public async Task<Result<IEnumerable<GuideSchema.Response>>> GetAsync(CancellationToken ct)
    {
        var data = await CachedAsync(
            CacheKey("all"),
            async token => await ActiveGuides.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(AppInfrastructure.MaxPageSize)
                .AsSummary()
                .ToListAsync(token),
            ct);

        return Result<IEnumerable<GuideSchema.Response>>.Success(data, null, "전체목록 반환 성공");
    }

    public Task<CursorPagedResult<GuideSchema.Response>> GetCursorListAsync(
        CursorPagedQuery query, CancellationToken ct)
        => QueryCursorPageAsync(query.Cursor, query.Size, query.SearchKeyword, query.Descending, ct);

    public async Task<OffsetPagedResult<GuideSchema.Response>> GetOffsetListAsync(
        OffsetPagedQuery query, CancellationToken ct)
    {
        // 검증은 캐시 앞에서: 잘못된 요청이 캐시 키를 만들지 않게 한다.
        if (query.PageNumber < 1)
            throw new BuddhamException(RsCode.BadRequest, "페이지 번호는 1 이상이어야 합니다.");

        if (query.PageSize is < 1 or > AppInfrastructure.MaxPageSize)
            throw new BuddhamException(RsCode.BadRequest,
                $"페이지 크기는 1~{AppInfrastructure.MaxPageSize} 사이여야 합니다.");

        var keyword = NormalizeKeyword(query.SearchKeyword);
        var sortBy = query.SortBy?.Trim().ToLowerInvariant();

        var slice = await CachedAsync(
            CacheKey("offset", keyword, sortBy, query.Descending, query.PageNumber, query.PageSize),
            async token =>
            {
                var baseQuery = ApplyKeyword(ActiveGuides.AsNoTracking(), keyword);
                var totalCount = await baseQuery.CountAsync(token);

                var data = await ApplySort(baseQuery, sortBy, query.Descending)
                    .ApplyOffset(query.PageNumber, query.PageSize)
                    .AsSummary()
                    .ToListAsync(token);

                return new OffsetSlice(data, totalCount);
            },
            ct);

        return OffsetPagedResult<GuideSchema.Response>.Success(
            slice.Items, slice.TotalCount, query.PageNumber, query.PageSize, "오프셋 페이지 목록 전송완료");
    }

    public Task<CursorPagedResult<GuideSchema.Response>> GetSemanticListAsync(
        SemanticSearchQuery query, CancellationToken ct)
    {
        return string.IsNullOrWhiteSpace(query.Keyword)
            ? QueryCursorPageAsync(query.Cursor, query.Size, null, true, ct)
            : HandleVectorSearchAsync(query, ct);
    }

    public async Task<Result<GuideSchema.Response>> GetByIdAsync(long id, CancellationToken ct)
    {
        // 없는 Id 도 캐시한다(Create 시 태그 무효화로 정리되므로 안전).
        // HybridCache 에 null 을 직접 맡기지 않도록 래퍼에 담는다.
        var cached = await CachedAsync(
            CacheKey("detail", id),
            async token => new Cached<GuideSchema.Response>(
                await ActiveGuides.AsNoTracking()
                    .Where(x => x.Id == id)
                    .AsSummary()
                    .FirstOrDefaultAsync(token)),
            ct);

        return cached.Value is null
            ? Result<GuideSchema.Response>.NotFound()
            : Result<GuideSchema.Response>.Success(cached.Value, null, "OK");
    }

    // ══════════════════════════════════════════════════════════
    // 생성 / 수정 / 삭제  (성공 시에만 캐시 무효화)
    // ══════════════════════════════════════════════════════════

    public async Task<Result> CreateAsync(GuideSchema.Request request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure(RsCode.Unauthorized, "로그인이 필요합니다.");

        if (ValidateRequest(request) is { } invalid) return invalid;

        var (vector, failure) = await TryEmbedAsync(SummarizeForEmbedding(request.Title, request.Content), ct);
        if (failure is not null) return failure;

        var entity = request.ToEntity(userId, UtcNow);
        entity.Embedding = vector;

        context.Guides.Add(entity);

        var saveFailure = await context.TrySaveChangeAsync(logger, EntityName, ct);
        if (saveFailure is not null) return saveFailure;

        await InvalidateCacheAsync();
        return Result.Created();
    }

    public async Task<Result> UpdateAsync(long id, GuideSchema.Request request, CancellationToken ct)
    {
        if (ValidateRequest(request) is { } invalid) return invalid;

        var entity = await ActiveGuides.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result.NotFound();
        if (!authorization.CanModify(entity.UserId)) return Result.Forbidden();

        // 임베딩 입력 텍스트가 바뀌었거나, 기존 임베딩이 없을 때만 재생성
        var newText = SummarizeForEmbedding(request.Title, request.Content);
        var oldText = SummarizeForEmbedding(entity.Title, entity.Content);

        float[]? newEmbedding = null;
        if (entity.Embedding is null || newText != oldText)
        {
            var (vector, failure) = await TryEmbedAsync(newText, ct);
            if (failure is not null) return failure;
            newEmbedding = vector;
        }

        entity.ApplyUpdate(request, UtcNow, newEmbedding);

        var saveFailure = await context.TrySaveChangeAsync(logger, EntityName, ct);
        if (saveFailure is not null) return saveFailure;

        await InvalidateCacheAsync();
        return Result.Updated();
    }

    /// <summary>
    /// 소프트 삭제
    /// </summary>
    public async Task<Result> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await ActiveGuides.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result.NotFound();
        if (!authorization.CanModify(entity.UserId)) return Result.Forbidden();

        var now = UtcNow;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;

        var saveFailure = await context.TrySaveChangeAsync(logger, EntityName, ct);
        if (saveFailure is not null) return saveFailure;

        await InvalidateCacheAsync();
        return Result.Success();
    }

    // ══════════════════════════════════════════════════════════
    // 페이징 / 검색 내부 구현
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// CreatedAt + Id 커서 페이징 (키워드 검색 선택). 실제 로직은 공통 헬퍼가 담당한다.
    /// 잘못된 커서/크기는 팩토리 안에서 예외가 되며, 예외는 캐시되지 않는다.
    /// </summary>
    private async Task<CursorPagedResult<GuideSchema.Response>> QueryCursorPageAsync(
        string? cursor, int size, string? keyword, bool descending, CancellationToken ct)
    {
        var normalizedKeyword = NormalizeKeyword(keyword);

        var slice = await CachedAsync(
            CacheKey("cursor", cursor, size, normalizedKeyword, descending),
            async token =>
            {
                var source = ApplyKeyword(ActiveGuides.AsNoTracking(), normalizedKeyword);

                return await source.ToCursorSliceAsync(
                    createdAt: x => x.CreatedAt,
                    id: x => x.Id,
                    project: q => q.AsSummary(),
                    keyOf: r => (r.CreatedAt, r.Id),
                    options: new CursorSliceOptions(
                        cursor, size, AppInfrastructure.MaxPageSize, normalizedKeyword, descending),
                    ct: token);
            },
            ct);

        return CursorPagedResult<GuideSchema.Response>.Success(
            slice.Items, slice.NextCursor, slice.HasMore, slice.TotalCount ?? 0, "커서 목록을 전송했습니다.");
    }

    /// <summary>
    /// pgvector 코사인 유사도 검색 + Offset 커서 에뮬레이션
    /// </summary>
    private async Task<CursorPagedResult<GuideSchema.Response>> HandleVectorSearchAsync(
        SemanticSearchQuery query, CancellationToken ct)
    {
        if (query.Size is < 1 or > AppInfrastructure.MaxPageSize)
            throw new BuddhamException(RsCode.BadRequest,
                $"페이지 크기는 1~{AppInfrastructure.MaxPageSize} 사이여야 합니다.");

        var keyword = query.Keyword!.Trim();

        if (keyword.Length > AppInfrastructure.MaxKeywordLength)
            throw new BuddhamException(RsCode.BadRequest,
                $"검색어는 {AppInfrastructure.MaxKeywordLength}자 이하여야 합니다.");

        // NaN 도 이 조건에서 걸러진다
        if (!(query.Threshold is >= 0f and <= 1f))
            throw new BuddhamException(RsCode.BadRequest, "Threshold 는 0 ~ 1 사이어야 합니다.");

        // ── 커서에서 offset 복원 ───────────────────────────────
        var offset = 0;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!CursorToken.TryParseVectorToken(query.Cursor, out var token)
                || token is null
                || token.Keyword != keyword
                || token.Threshold != query.Threshold
                || token.Offset > MaxSemanticOffset)
                throw new BuddhamException(RsCode.BadRequest, "유효하지 않거나 검색 조건이 변경된 커서입니다.");

            offset = token.Offset;
        }

        // 키는 cursor 문자열이 아니라 offset 으로 만든다 (같은 페이지는 같은 키).
        var slice = await CachedAsync(
            CacheKey("semantic", keyword, query.Threshold, query.Size, offset),
            async token => await SearchVectorSliceAsync(keyword, query.Threshold, query.Size, offset, token),
            ct);

        return CursorPagedResult<GuideSchema.Response>.Success(
            slice.Items, slice.NextCursor, slice.HasMore, slice.TotalCount ?? 0, "벡터 유사도 검색 목록을 전송했습니다.");
    }

    private async Task<CursorSlice<GuideSchema.Response>> SearchVectorSliceAsync(
        string keyword, float threshold, int size, int offset, CancellationToken ct)
    {
        // ── 키워드 → 임베딩 (같은 검색어는 24시간 재사용) ─────────
        var (embedding, failure) = await TryEmbedQueryAsync(keyword, ct);
        if (embedding is null)
            throw new BuddhamException(failure?.Code ?? RsCode.Error, "검색 서비스를 일시적으로 사용할 수 없습니다.");

        var queryVector = new Pgvector.Vector(embedding);
        var maxDistance = 1f - threshold;

        var matched = ActiveGuides.AsNoTracking()
            .Where(x => x.Embedding != null && x.Embedding.CosineDistance(queryVector) <= maxDistance);

        // 첫 페이지에서만 전체 개수 계산
        int? totalCount = offset == 0 ? await matched.CountAsync(ct) : null;

        // 코사인 거리 오름차순 = 유사도 내림차순, 동점은 Id 로 안정 정렬
        // 참고: HNSW 인덱스는 기본 hnsw.ef_search(40) 개 후보만 반환한다.
        // 데이터가 많아지면 hnsw.ef_search 상향 또는 pgvector 0.8+ iterative scan 을 검토한다.
        var items = await matched
            .OrderBy(x => x.Embedding!.CosineDistance(queryVector))
            .ThenBy(x => x.Id)
            .Skip(offset)
            .Take(size + 1)
            .AsSummary()
            .ToListAsync(ct);

        var nextOffset = offset + size;
        var hasMore = items.Count > size && nextOffset < MaxSemanticOffset;

        if (items.Count > size)
            items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore
            ? CursorToken.CreateVectorToken(nextOffset, keyword, threshold)
            : null;

        return new CursorSlice<GuideSchema.Response>(items, nextCursor, hasMore, totalCount);
    }

    private static string? NormalizeKeyword(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

    /// <summary>
    /// 정규화(Trim)된 키워드를 받는다. 길이 초과 시 BadRequest.
    /// </summary>
    private static IQueryable<Guide> ApplyKeyword(IQueryable<Guide> query, string? keyword)
    {
        if (keyword is null)
            return query;

        if (keyword.Length > AppInfrastructure.MaxKeywordLength)
            throw new BuddhamException(RsCode.BadRequest,
                $"검색어는 {AppInfrastructure.MaxKeywordLength}자 이하여야 합니다.");

        var pattern = "%" + keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

        return query.Where(x =>
            EF.Functions.ILike(x.Title, pattern) ||
            EF.Functions.ILike(x.Content, pattern));
    }

    private static IQueryable<Guide> ApplySort(IQueryable<Guide> query, string? sortBy, bool descending)
    {
        return (sortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("title", true) => query.OrderByDescending(x => x.Title).ThenByDescending(x => x.Id),
            ("title", false) => query.OrderBy(x => x.Title).ThenBy(x => x.Id),
            ("id", true) => query.OrderByDescending(x => x.Id),
            ("id", false) => query.OrderBy(x => x.Id),
            (_, false) => query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
        };
    }

    // ══════════════════════════════════════════════════════════
    // 검증 / 임베딩
    // ══════════════════════════════════════════════════════════

    private static Result? ValidateRequest(GuideSchema.Request? request)
    {
        if (request is null)
            return Result.Failure(RsCode.BadRequest, "요청 본문이 비어 있습니다.");

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["title"] = ["제목은 필수입니다."];
        else if (request.Title.Trim().Length > AppInfrastructure.MaxTitleLength)
            errors["title"] = [$"제목은 {AppInfrastructure.MaxTitleLength}자 이하여야 합니다."];

        if (string.IsNullOrWhiteSpace(request.Content))
            errors["content"] = ["본문은 필수입니다."];
        else if (request.Content.Length > MaxContentLength)
            errors["content"] = [$"본문은 {MaxContentLength}자 이하여야 합니다."];

        if (!Enum.IsDefined(request.GuideCategory))
            errors["guideCategory"] = ["유효하지 않은 카테고리입니다."];

        return errors.Count == 0 ? null : Result.ValidationFailed(errors);
    }

    /// <summary>
    /// 임베딩 생성 표준 진입점. 반환값은 항상 (vector != null) 또는 (failure != null) 중 하나다.
    /// </summary>
    private async Task<(float[]? Vector, Result? Failure)> TryEmbedAsync(string text, CancellationToken ct)
    {
        var (vector, failure) = await embeddingService.GenerateAsync(text, ct);

        if (failure is not null)
        {
            logger.LogWarning("{Entity} embedding generation failed: {Code}", EntityName, failure.Code);
            return (null, failure);
        }

        return vector is null
            ? (null, Result.Failure(RsCode.Error, "임베딩 생성에 실패했습니다."))
            : (vector, null);
    }

    /// <summary>
    /// 검색어 임베딩 캐시판. 성공한 벡터만 캐시한다
    /// (실패는 예외로 팩토리를 빠져나가므로 HybridCache 가 저장하지 않는다).
    /// </summary>
    private async Task<(float[]? Vector, Result? Failure)> TryEmbedQueryAsync(string keyword, CancellationToken ct)
    {
        Result? failure = null;

        try
        {
            var vector = await cache.GetOrCreateAsync(
                CacheKey("embed", keyword),
                async token =>
                {
                    var (v, f) = await TryEmbedAsync(keyword, token);
                    if (v is not null) return v;

                    failure = f;
                    throw new EmbeddingUnavailableException();
                },
                EmbeddingCacheOptions,
                tags: null,
                cancellationToken: ct);

            return (vector, null);
        }
        catch (EmbeddingUnavailableException)
        {
            // 동시 호출 중 팩토리를 실행하지 않은 쪽은 failure 가 null 일 수 있다.
            return (null, failure ?? Result.Failure(RsCode.Error, "임베딩 생성에 실패했습니다."));
        }
    }

    private static string SummarizeForEmbedding(string? title, string? content)
    {
        var body = (content ?? string.Empty).Trim();

        if (body.Length > AppInfrastructure.EmbeddingContentChars)
        {
            body = body[..AppInfrastructure.EmbeddingContentChars];
            // 서러게이트 쌍이 중간에서 잘리는 것을 방지
            if (body.Length > 0 && char.IsHighSurrogate(body[^1])) body = body[..^1];
        }

        return $"제목: {(title ?? string.Empty).Trim()}\n본문: {body}";
    }

    // ══════════════════════════════════════════════════════════
    // 캐시 전용 타입 (직렬화 가능한 단순 레코드)
    // ══════════════════════════════════════════════════════════

    internal sealed record OffsetSlice(List<GuideSchema.Response> Items, int TotalCount);

    internal sealed record Cached<T>(T? Value);

    private sealed class EmbeddingUnavailableException : Exception;
}
