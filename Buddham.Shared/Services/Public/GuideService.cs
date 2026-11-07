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
using Microsoft.Extensions.Logging;
using Pgvector.EntityFrameworkCore;

namespace Buddham.Shared.Services.Public;

public class GuideService(
    PublicDbContext context,
    ICurrentUserService currentUser,
    IEntityAuthorizationService authorization,
    IEmbeddingService embeddingService,
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
    // 공통 헬퍼
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 소프트 삭제되지 않은 데이터만. 모든 조회/수정은 반드시 이것을 통해서만 접근한다.
    /// (전역 쿼리 필터를 이미 쓰고 있다면 Where 는 중복이지만 무해하다.)
    /// </summary>
    private IQueryable<Guide> ActiveGuides => context.Guides.Where(x => x.DeletedAt == null);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    // ══════════════════════════════════════════════════════════
    // 조회
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 전체 목록. 무제한 반환을 막기 위해 MaxPageSize 로 상한을 둔다.
    /// </summary>
    public async Task<Result<IEnumerable<GuideSchema.Response>>> GetAsync(CancellationToken ct)
    {
        var data = await ActiveGuides.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(AppInfrastructure.MaxPageSize)
            .AsSummary()
            .ToListAsync(ct);

        return Result<IEnumerable<GuideSchema.Response>>.Success(data, null, "전체목록 반환 성공");
    }

    public Task<CursorPagedResult<GuideSchema.Response>> GetCursorListAsync(
        CursorPagedQuery query, CancellationToken ct)
        => QueryCursorPageAsync(query.Cursor, query.Size, query.SearchKeyword, query.Descending, ct);

    public async Task<OffsetPagedResult<GuideSchema.Response>> GetOffsetListAsync(
        OffsetPagedQuery query, CancellationToken ct)
    {
        if (query.PageNumber < 1)
            throw new BuddhamException(RsCode.BadRequest, "페이지 번호는 1 이상이어야 합니다.");

        if (query.PageSize is < 1 or > AppInfrastructure.MaxPageSize)
            throw new BuddhamException(RsCode.BadRequest,
                $"페이지 크기는 1~{AppInfrastructure.MaxPageSize} 사이여야 합니다.");

        var baseQuery = ApplyKeyword(ActiveGuides.AsNoTracking(), NormalizeKeyword(query.SearchKeyword));
        var totalCount = await baseQuery.CountAsync(ct);

        var data = await ApplySort(baseQuery, query.SortBy, query.Descending)
            .ApplyOffset(query.PageNumber, query.PageSize)
            .AsSummary()
            .ToListAsync(ct);

        return OffsetPagedResult<GuideSchema.Response>.Success(
            data, totalCount, query.PageNumber, query.PageSize, "오프셋 페이지 목록 전송완료");
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
        var data = await ActiveGuides.AsNoTracking()
            .Where(x => x.Id == id)
            .AsSummary()
            .FirstOrDefaultAsync(ct);

        return data is null
            ? Result<GuideSchema.Response>.NotFound()
            : Result<GuideSchema.Response>.Success(data, null, "OK");
    }

    // ══════════════════════════════════════════════════════════
    // 생성 / 수정 / 삭제
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
        return await context.TrySaveChangeAsync(logger, EntityName, ct) ?? Result.Created();
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
        return await context.TrySaveChangeAsync(logger, EntityName, ct) ?? Result.Updated();
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

        return await context.TrySaveChangeAsync(logger, EntityName, ct) ?? Result.Success();
    }

    // ══════════════════════════════════════════════════════════
    // 페이징 / 검색 내부 구현
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// CreatedAt + Id 커서 페이징 (키워드 검색 선택). 실제 로직은 공통 헬퍼가 담당한다.
    /// </summary>
    private async Task<CursorPagedResult<GuideSchema.Response>> QueryCursorPageAsync(
        string? cursor, int size, string? keyword, bool descending, CancellationToken ct)
    {
        var normalizedKeyword = NormalizeKeyword(keyword);
        var source = ApplyKeyword(ActiveGuides.AsNoTracking(), normalizedKeyword);

        var slice = await source.ToCursorSliceAsync(
            createdAt: x => x.CreatedAt,
            id: x => x.Id,
            project: q => q.AsSummary(),
            keyOf: r => (r.CreatedAt, r.Id),
            options: new CursorSliceOptions(
                cursor, size, AppInfrastructure.MaxPageSize, normalizedKeyword, descending),
            ct: ct);

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

        // ── 키워드 → 임베딩 ────────────────────────────────────
        var (embedding, failure) = await TryEmbedAsync(keyword, ct);
        if (embedding is null)
            throw new BuddhamException(failure?.Code ?? RsCode.Error, "검색 서비스를 일시적으로 사용할 수 없습니다.");

        var queryVector = new Pgvector.Vector(embedding);
        var maxDistance = 1f - query.Threshold;

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
            .Take(query.Size + 1)
            .AsSummary()
            .ToListAsync(ct);

        var nextOffset = offset + query.Size;
        var hasMore = items.Count > query.Size && nextOffset < MaxSemanticOffset;

        if (items.Count > query.Size)
            items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore
            ? CursorToken.CreateVectorToken(nextOffset, keyword, query.Threshold)
            : null;

        return CursorPagedResult<GuideSchema.Response>.Success(
            items, nextCursor, hasMore, totalCount ?? 0, "벡터 유사도 검색 목록을 전송했습니다.");
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
}
