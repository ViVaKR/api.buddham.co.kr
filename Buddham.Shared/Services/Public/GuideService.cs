using System.Security.Claims;
using Buddham.Core.Common;
using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Core.Interfaces.Public;
using Buddham.Core.Schemas;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Public;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Buddham.Shared.Services.Public;

public class GuideService(
    PublicDbContext context,
    IOllamaService ollamaService,
    UserManager<ApplicationUser> userManager) : IGuideService
{
    public async Task<Result<IEnumerable<GuideSchema.Response>>> GetAsync()
    {
        var data = await context.Guides.AsNoTracking().AsSummary().ToListAsync();
        return Result<IEnumerable<GuideSchema.Response>>.Success(data, null, "전체목록 반환 성공");
    }

    public async Task<CursorPagedResult<GuideSchema.Response>> GetCursorListAsync(CursorPagedQuery query, CancellationToken ct)
    {
        var baseQuery = context.Guides.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var k = query.SearchKeyword.Trim();
            baseQuery = baseQuery.Where(x =>
                EF.Functions.Like(x.Title, $"%{k}%") ||
                EF.Functions.Like(x.Content, $"%{k}%"));
        }

        var totalCount = 0;

        if (string.IsNullOrWhiteSpace(query.Cursor))
            totalCount = await baseQuery.CountAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var (cursorTicks, cursorId) = CursorToken.ParseToken(query.Cursor);
            if (cursorTicks > 0 && cursorId != 0)
            {
                var cursorDateTime = new DateTime(cursorTicks, DateTimeKind.Utc);
                baseQuery = baseQuery.Where(x =>
                    x.CreatedAt < cursorDateTime ||
                    (x.CreatedAt == cursorDateTime && x.Id > cursorId));
            }
        }

        var rawItems = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(query.Size + 1)
            .ToListAsync(ct);

        var hasMore = rawItems.Count > query.Size;
        var executionItems = rawItems.Take(query.Size).AsQueryable();
        var mappedResponses = executionItems.AsSummary().ToList();

        string? nextCursor = null;
        if (!hasMore || mappedResponses.Count == 0)
        {
            return CursorPagedResult<GuideSchema.Response>.Success
                (mappedResponses, nextCursor, hasMore, totalCount, "커서 목록을 전송했습니다.");
        }

        var lastItem = executionItems.Last();
        nextCursor = CursorToken.CreateToken(lastItem.CreatedAt, lastItem.Id);

        return CursorPagedResult<GuideSchema.Response>.Success(
            mappedResponses,
            nextCursor,
            hasMore,
            totalCount,
            "커서 목록을 전송했습니다.");
    }

    public async Task<OffsetPagedResult<GuideSchema.Response>> GetOffsetListAsync(OffsetPagedQuery query, CancellationToken ct)
    {
        var baseQuery = context.Guides.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var k = query.SearchKeyword.Trim();
            baseQuery = baseQuery.Where(x =>
                EF.Functions.ILike(x.Title, $"%{k}%") ||
                EF.Functions.ILike(x.Content, $"%{k}%"));
        }

        baseQuery = query.SortBy switch
        {
            "creaateAt" => baseQuery.OrderBy(x => x.CreatedAt),
            "title" => baseQuery.OrderBy(x => x.Title),
            "id" => baseQuery.OrderByDescending(x => x.Id),
            _ => baseQuery.OrderByDescending(x => x.CreatedAt)
        };

        var totalCount = await baseQuery.CountAsync(ct);

        var data = await baseQuery.Skip(query.PageSize * (query.PageNumber - 1))
            .Take(query.PageSize)
            .AsSummary()
            .ToListAsync(cancellationToken: ct);

        return OffsetPagedResult<GuideSchema.Response>.Success(
            data,
            totalCount,
            query.PageNumber,
            query.PageSize,
            "오프셋 페이지 목록 전송완료");
    }

    public async Task<CursorPagedResult<GuideSchema.Response>> GetSemanticListAsync(SemanticSearchQuery query, CancellationToken ct)
    {
        // ── 🔀 분기점: 벡터 검색 vs 기존 커서 페이징 ──────────────────────────
        // SearchKeyword가 있으면 벡터 유사도 검색 경로로 진입
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            return await HandleVectorSearchAsync(query, ct);

        return await HandleCursorPagedAsync(query, ct);
    }

    public async Task<Result<GuideSchema.Response>> GetByIdAsync(long id)
    {
        var entity = await context.Guides.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return Result<GuideSchema.Response>.NotFound();
        var payload = entity.AsDetail();
        return Result<GuideSchema.Response>.Success(payload, null, "OK");
    }

    /// <summary>
    /// 생성
    /// </summary>
    public async Task<Result> CreateAsync(GuideSchema.Request request, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(ClaimTypes.NameIdentifier);
        if (user is null) return Result.Forbidden();

        // ── 1. 입력값 검증 ────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure(RsCode.BadRequest, "제목과 본문은 필수입니다.");

        // ── 2. 임베딩 텍스트 조합 ─────────────────────────────────
        // 테이블마다 이 부분만 맞춤 설정
        var textToEmbed = SummarizeForEmbedding(request);

        // ── 3. 임베딩 생성 (Ollama / bge-m3) ─────────────────────
        float[] vectorData;
        try
        {
            vectorData = await ollamaService.GetEmbeddingAsync(textToEmbed, ct);
        }
        catch (HttpRequestException ex)
        {
            // Ollama 서버 다운 or 네트워크 오류
            // → DB 저장 없이 즉시 반환 (글로벌 핸들러 불필요)
            return Result.Failure(RsCode.Error, $"임베딩 서비스에 연결할 수 없습니다.- {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            // 차원 불일치 등 OllamaService 내부 검증 실패
            return Result.Failure(RsCode.BadRequest, ex.Message);
        }

        var entity = request.ToEntity();
        entity.UserId = user.Id;
        entity.Embedding = vectorData;

        // ── 4. 엔티티 구성 ────────────────────────────────────────
        context.Guides.Add(entity);

        // ── 5. 저장 ───────────────────────────────────────────────
        try
        {
            await context.SaveChangesAsync(ct);
            return Result.Success("법문 가이드가 성공적으로 저장되었습니다.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Conflict($"다른 사용자가 동시에 처리 중입니다. 다시 시도해주세요. - {ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            // 유니크 제약, FK 위반 등 DB 레벨 오류
            return Result.Error($"데이터 저장 중 오류가 발생했습니다. - {ex.Message}");
        }
    }

    /// <summary>
    /// 수정
    /// UPDATE
    /// </summary>
    public async Task<Result> UpdateAsync(long id, GuideSchema.Request request, CancellationToken ct)
    {
        // ── 1. 단건 조회 ──────────────────────────────────────────
        var entity = await context.Guides
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);

        if (entity == null)
            return Result.NotFound();

        // ── 2. 임베딩 재생성 (Title/Content 변경 시에만) ──────────
        // 변경 없으면 Ollama 호출 스킵 → 비용/레이턴시 절감
        float[]? newEmbedding = null;
        var isContentChanged = entity.Title != request.Title
                               || entity.Content != request.Content;

        if (isContentChanged)
        {
            try
            {
                var textToEmbed = SummarizeForEmbedding(request);

                var vectorData = await ollamaService.GetEmbeddingAsync(textToEmbed, ct);

                newEmbedding = vectorData;
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure(RsCode.Error, $"임베딩 서비스에 연결할 수 없습니다. - " +
                                                    $"{ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(RsCode.BadRequest, ex.Message);
            }
        }

        // ── 3. 엔티티 패치 ────────────────────────────────────────
        entity.ApplyPatch(request, newEmbedding);

        // ── 4. 저장 ───────────────────────────────────────────────
        try
        {
            await context.SaveChangesAsync(ct);
            return Result.Updated("법문 가이드가 성공적으로 수정되었습니다.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Conflict($"다른 사용자가 이미 수정했습니다. 새로고침 후 다시 시도해주세요. - " +
                                   $"{ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            return Result.Error($"데이터 저장 중 오류가 발생했습니다. - {ex.Message}");
        }
    }

    /// <summary>
    /// DELETE
    /// 삭제
    /// </summary>
    public async Task<Result> DeleteAsync(long id)
    {
        var data = await context.Guides.FirstOrDefaultAsync(x => x.Id == id);
        if (data == null)
            return Result.NotFound();
        data.DeletedAt = DateTime.UtcNow;
        data.UpdatedAt = DateTime.UtcNow;
        try
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Conflict($"다른 사용자가 이미 데이터를 삭제 했습니다. - {ex.Message}");
        }
    }

    /// <summary>
    /// pgvector 코사인 유사도 기반 검색 + Offset 커서 에뮬레이
    /// </summary>
    private async Task<CursorPagedResult<GuideSchema.Response>> HandleVectorSearchAsync(
        SemanticSearchQuery query,
        CancellationToken ct
    )
    {
        var keyword = query.Keyword.Trim();
        var currentOffset = 0;
        // ── 커서에서 Offset 복원 ──────────────────────────────────────────────
        // 두 번째 페이지 이후 요청이라면 커서에서 offset과 keyword를 꺼냄
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var (isVector, parsedOffset, parsedKeyword) = CursorToken.ParseVectorToken(query.Cursor);
            // 🛡️ 커서 위변조 방지: 키워드가 다르면 커서 무효화하고 처음부터 검색
            if (isVector && parsedKeyword == keyword)
                currentOffset = parsedOffset;
        }

        // ── 1단계: 키워드 → 임베딩 벡터 변환 ────────────────────────────────
        var embeddingArray = await ollamaService.GetEmbeddingAsync(keyword, ct);
        var queryVector = new Pgvector.Vector(embeddingArray);
        // ── 2단계: 전체 카운트 (첫 페이지 진입 시에만) ───────────────────────
        var totalCount = 0;
        if (currentOffset == 0)
        {
            // 임계값 이상인 유사 문서 전체 수 계산
            totalCount = await context.Guides
                .AsNoTracking()
                .Where(x => x.Embedding != null
                            && x.Embedding.CosineDistance(queryVector) <= 1f - query.Threshold)
                .CountAsync(ct);
        }

        // ── 3단계: 유사도 정렬 + Offset 기반 페이징 ─────────────────────────
        // [설계 이유] 벡터 유사도는 score 값이 실수(float)라 CreatedAt처럼
        // "다음 기준점"을 커서에 담기 애매함. Score 기반 커서를 쓰면 동점 처리가
        // 복잡해지므로, 안정성을 위해 Offset 에뮬레이션 방식을 채택.
        var rawItems = await context.Guides.AsNoTracking()
            .Where(x => x.Embedding!.CosineDistance(queryVector) <= (1f -
                                                                     query.Threshold))
            // 유사도 높은 순 (코사인 거리 오름차순 = 유사도 내림차순)
            .OrderBy(x => x.Embedding!.CosineDistance(queryVector))
            // 동점(동일 유사도) 시 Id로 보조 정렬 → 결과 안정성 보장
            .ThenBy(x => x.Id)
            .Skip(currentOffset)
            .Take(query.Size + 1) // 다음 페이지 존재 여부 판정용 + 1
            .ToListAsync(ct);

        // ── 4단계: 다음 페이지 판정 ──────────────────────────────────────────
        var hasMore = rawItems.Count > query.Size;
        var pageItems = rawItems.Skip(query.Size).AsQueryable();
        var mappedResponses = pageItems.AsSummary().ToList();
        // ── 5단계: 다음 커서 발행 ────────────────────────────────────────────
        string? nextCursor = null;
        if (!hasMore || mappedResponses.Count <= 0)
            return CursorPagedResult<GuideSchema.Response>.Success(
                mappedResponses,
                nextCursor,
                hasMore,
                totalCount,
                "백터 유사도 검색 목록을 전홋ㅇ했습니다."
            );

        var nextOffset = currentOffset + query.Size;
        nextCursor = CursorToken.CreateVectorToken(nextOffset, keyword);

        return CursorPagedResult<GuideSchema.Response>.Success(
            mappedResponses,
            nextCursor,
            hasMore,
            totalCount,
            "백터 유사도 검색 목록을 전송했습니다."
        );
    }

    /// <summary>
    /// 기존 CreatedAt + Id 커서 방식 (SearchKeyword 없을 때)
    /// </summary>
    private async Task<CursorPagedResult<GuideSchema.Response>> HandleCursorPagedAsync(
        SemanticSearchQuery query,
        CancellationToken ct)
    {
        var baseQuery = context.Guides.AsNoTracking();

        var totalCount = 0;
        if (string.IsNullOrWhiteSpace(query.Cursor))
            totalCount = await baseQuery.CountAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var (cursorTicks, cursorId) = CursorToken.ParseToken(query.Cursor);
            if (cursorTicks > 0 && cursorId != 0)
            {
                var cursorDateTime = new DateTime(cursorTicks, DateTimeKind.Utc);
                baseQuery = baseQuery.Where(x =>
                    x.CreatedAt < cursorDateTime ||
                    (x.CreatedAt == cursorDateTime && x.Id > cursorId));
            }
        }

        var rawItems = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(query.Size + 1)
            .ToListAsync(ct);

        var hasMore = rawItems.Count > query.Size;
        var pageItems = rawItems.Take(query.Size).AsQueryable();
        var mappedResponses = pageItems.AsSummary().ToList();

        string? nextCursor = null;
        if (!hasMore || mappedResponses.Count <= 0)
            return CursorPagedResult<GuideSchema.Response>.Success(
                mappedResponses,
                nextCursor,
                hasMore,
                totalCount,
                "커서 목록을 전송했습니다.");

        var lastItem = pageItems.Last();
        nextCursor = CursorToken.CreateToken(lastItem.CreatedAt, lastItem.Id);

        return CursorPagedResult<GuideSchema.Response>.Success(
            mappedResponses,
            nextCursor,
            hasMore,
            totalCount,
            "커서 목록을 전송했습니다.");
    }

    private static string SummarizeForEmbedding(GuideSchema.Request request)
    {
        var title = request.Title;
        var content = request.Content;

        // 텍스트 조합 전략은 벡터DB에 저장할 텍스트를 어떻게 구성할지에 대한 계획입니다.
        // 오늘의 법문과 같은 경우, 제목,
        // 본문 앞부분을 조합하여 하나의 텍스트로 만드는 전략을 사용합니다. 이렇게 하면,
        // 검색 시에 제목과 요약이 중요한 역할을 하면서도, 본문의 핵심 내용도 포함되어 검색의 정확도를 높일 수 있습니다.
        // 특히, 긴 본문에서 앞부분을 선택하는 이유는 일반적으로 중요한 정보가 앞부분에 위치하는 경우가 많기 때문입니다.
        // 또한, 벡터DB의 토큰 제한(예: 1024 토큰)을 고려하여 텍스트를 최적화하는 것이 중요합니다.
        return $"""
                제목: {title}
                본문: {content[..Math.Min(content.Length, 500)]}
                """;
    }
}
