using Buddham.Core.Common;
using Buddham.Core.Common.Options;
using Buddham.Core.Common.Primitives;
using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces;
using Buddham.Core.Interfaces.Common;
using Buddham.Shared.Caching;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Persistence;
using Buddham.Shared.Middlewares;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Buddham.Shared.Services.Crud;

/// <summary>
/// CrudService + 임베딩 생성/갱신 + pgvector 시맨틱 검색.
/// 임베딩이 필요 없는 엔티티는 CrudService 를 그대로 상속하면 된다.
/// </summary>
public abstract class EmbeddingCrudService<TEntity, TRequest, TResponse>
    : CrudService<TEntity, TRequest, TResponse>, ISemanticQueryable<TResponse>
    where TEntity : EntityBase, IEmbeddable
    where TRequest : class
    where TResponse : class, ICursorKeyed
{
    private static readonly Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions QueryEmbeddingCacheOptions
        = CacheProfiles.RedisOnly(TimeSpan.FromHours(24));

    private readonly IEmbeddingService _embeddingService;

    protected EmbeddingCrudService(
        DbContext context, CrudDependencies deps, IEmbeddingService embeddingService, EntityCacheOptions cacheOptions)
        : base(context, deps, cacheOptions)
    {
        _embeddingService = embeddingService;
    }

    /// <summary>요청 → 임베딩 입력 텍스트. 보통 BuildEmbeddingText(title, content).</summary>
    protected abstract string EmbeddingText(TRequest request);

    /// <summary>기존 엔티티 → 임베딩 입력 텍스트 (변경 여부 비교용).</summary>
    protected abstract string EmbeddingText(TEntity entity);

    /// <summary>제목+본문 표준 포맷. 본문은 상한까지만 자르고 서로게이트 쌍이 깨지지 않게 한다.</summary>
    protected static string BuildEmbeddingText(string? title, string? content)
    {
        var body = (content ?? string.Empty).Trim();
        if (body.Length > AppInfrastructure.EmbeddingContentChars)
        {
            body = body[..AppInfrastructure.EmbeddingContentChars];
            if (body.Length > 0 && char.IsHighSurrogate(body[^1])) body = body[..^1];
        }
        return $"제목: {(title ?? string.Empty).Trim()}\n본문: {body}";
    }

    // ══════════════════════════════════════════════════════════
    // 생성/수정 훅: 임베딩 처리
    // ══════════════════════════════════════════════════════════

    protected override async Task<Result?> OnCreatingAsync(TEntity entity, TRequest request, CancellationToken ct)
    {
        var (vector, failure) = await TryEmbedAsync(EmbeddingText(request), ct);
        if (failure is not null) return failure;

        entity.Embedding = vector;
        return null;
    }

    protected override async Task<Result?> OnUpdatingAsync(TEntity entity, TRequest request, CancellationToken ct)
    {
        // 임베딩 입력 텍스트가 바뀌었거나, 기존 임베딩이 없을 때만 재생성
        var newText = EmbeddingText(request);
        if (entity.Embedding is not null && newText == EmbeddingText(entity))
            return null;

        var (vector, failure) = await TryEmbedAsync(newText, ct);
        if (failure is not null) return failure;

        entity.Embedding = vector;
        return null;
    }

    // ══════════════════════════════════════════════════════════
    // 시맨틱 검색
    // ══════════════════════════════════════════════════════════

    public virtual Task<CursorPagedResult<TResponse>> GetSemanticListAsync(
        SemanticSearchQuery query, CancellationToken ct)
        => string.IsNullOrWhiteSpace(query.Keyword)
            ? QueryCursorPageAsync(query.Cursor, query.Size, null, true, ct)
            : HandleVectorSearchAsync(query, ct);

    /// <summary>pgvector 코사인 유사도 검색 + Offset 커서 에뮬레이션</summary>
    private async Task<CursorPagedResult<TResponse>> HandleVectorSearchAsync(
        SemanticSearchQuery query, CancellationToken ct)
    {
        Paging.EnsureSize(query.Size);

        var keyword = query.Keyword!.Trim();
        Paging.EnsureKeyword(keyword);
        Paging.EnsureThreshold(query.Threshold);

        var offset = 0;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!CursorToken.TryParseVectorToken(query.Cursor, out var token)
                || token is null
                || token.Keyword != keyword
                || token.Threshold != query.Threshold
                || token.Offset > AppInfrastructure.MaxSemanticOffset)
                throw new BuddhamException(RsCode.BadRequest, "유효하지 않거나 검색 조건이 변경된 커서입니다.");

            offset = token.Offset;
        }

        // 키는 cursor 문자열이 아니라 offset 으로 만든다 (같은 페이지는 같은 키).
        var slice = await Cache.GetOrCreateAsync(
            Cache.Key("semantic", keyword, query.Threshold, query.Size, offset),
            async token => await SearchVectorSliceAsync(keyword, query.Threshold, query.Size, offset, token),
            ct);

        return CursorPagedResult<TResponse>.Success(
            slice.Items, slice.NextCursor, slice.HasMore, slice.TotalCount ?? 0, "벡터 유사도 검색 목록을 전송했습니다.");
    }

    private async Task<CursorSlice<TResponse>> SearchVectorSliceAsync(
        string keyword, float threshold, int size, int offset, CancellationToken ct)
    {
        var (embedding, failure) = await TryEmbedQueryAsync(keyword, ct);
        if (embedding is null)
            throw new BuddhamException(failure?.Code ?? RsCode.Error, "검색 서비스를 일시적으로 사용할 수 없습니다.");

        var queryVector = new Pgvector.Vector(embedding);
        var maxDistance = 1f - threshold;

        var matched = Active.AsNoTracking()
            .Where(x => x.Embedding != null && x.Embedding.CosineDistance(queryVector) <= maxDistance);

        // 첫 페이지에서만 전체 개수 계산
        int? totalCount = offset == 0 ? await matched.CountAsync(ct) : null;

        // 코사인 거리 오름차순 = 유사도 내림차순, 동점은 Id 로 안정 정렬
        // 참고: HNSW 인덱스는 기본 hnsw.ef_search(40) 개 후보만 반환한다.
        // 데이터가 많아지면 hnsw.ef_search 상향 또는 pgvector 0.8+ iterative scan 을 검토한다.
        var items = await Project(matched
                .OrderBy(x => x.Embedding!.CosineDistance(queryVector))
                .ThenBy(x => x.Id)
                .Skip(offset)
                .Take(size + 1))
            .ToListAsync(ct);

        var nextOffset = offset + size;
        var hasMore = items.Count > size && nextOffset < AppInfrastructure.MaxSemanticOffset;

        if (items.Count > size)
            items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore
            ? CursorToken.CreateVectorToken(nextOffset, keyword, threshold)
            : null;

        return new CursorSlice<TResponse>(items, nextCursor, hasMore, totalCount);
    }

    // ══════════════════════════════════════════════════════════
    // 임베딩 호출
    // ══════════════════════════════════════════════════════════

    /// <summary>임베딩 생성 표준 진입점. 반환값은 (vector != null) 또는 (failure != null) 중 하나다.</summary>
    private async Task<(float[]? Vector, Result? Failure)> TryEmbedAsync(string text, CancellationToken ct)
    {
        var (vector, failure) = await _embeddingService.GenerateAsync(text, ct);
        if (failure is not null)
            return (null, failure);

        return vector is null
            ? (null, Result.Failure(RsCode.Error, "임베딩 생성에 실패했습니다."))
            : (vector, null);
    }

    /// <summary>검색어 임베딩 캐시판. 성공한 벡터만 캐시한다.</summary>
    private Task<(float[]? Vector, Result? Failure)> TryEmbedQueryAsync(string keyword, CancellationToken ct)
        => Cache.GetOrCreateIfSuccessAsync(
            Cache.Key("embed", keyword),
            token => TryEmbedAsync(keyword, token),
            QueryEmbeddingCacheOptions,
            ct);
}
