using Buddham.Core.Common;
using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Core.Interfaces.Tangwha;
using Buddham.Core.Schemas;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Public;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Public;

public class TangwhaService(
    PublicDbContext context,
    IOllamaService ollamaService,
    UserManager<ApplicationUser> userManager) : ITangwhaService
{

    public async Task<Result<IEnumerable<TangwhaSchema.Response>>> GetAsync()
    {
        var data = await context.Tangwhas.AsNoTracking().AsSummary().ToListAsync();
        return Result<IEnumerable<TangwhaSchema.Response>>.Success(data, null, "전체목록 반환 성공");
    }

    /// <summary>
    /// Create
    /// </summary>
    public Task<Result> CreateAsync(TangwhaSchema.Request request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// DELETE
    /// </summary>
    public async Task<Result> DeleteAsync(long id)
    {
        var data = await context.Tangwhas.FirstOrDefaultAsync(x => x.Id == id);
        if (data == null) return Result.NotFound();

        data.DeletedAt = DateTime.UtcNow;
        data.UpdatedAt = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }

        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Conflict($"데이터가 이미 삭제 되었습니다. - {ex.Message}");
        }
    }


    public async Task<Result<TangwhaSchema.Response>> GetByIdAsync(long id)
    {
        var entity = await context.Tangwhas.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return Result<TangwhaSchema.Response>.NotFound();
        var payload = entity.AsDetail();
        return Result<TangwhaSchema.Response>.Success(payload, null, "OK");
    }

    public async Task<CursorPagedResult<TangwhaSchema.Response>> GetCursorListAsync(CursorPagedQuery query, CancellationToken ct)
    {
        var baseQuery = context.Tangwhas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var k = query.SearchKeyword.Trim();
            baseQuery = baseQuery.Where(x =>
                EF.Functions.Like(x.Title, $"%{k}%") ||
                EF.Functions.Like(x.Description, $"%{k}%"));
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
                baseQuery = baseQuery.Where(x => x.CreatedAt < x.CreatedAt || (x.CreatedAt == cursorDateTime && x.Id > cursorId));
            }
        }

        var rawItems = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(query.Size + 1)
            .ToListAsync();

        var hasMore = rawItems.Count > query.Size;
        var executionItems = rawItems.Take(query.Size).AsQueryable();
        var mappedResponses = executionItems.AsSummary().ToList();

        string? nextCursor = null;
        if (!hasMore || mappedResponses.Count == 0)
        {
            return CursorPagedResult<TangwhaSchema.Response>.Success
            (mappedResponses, nextCursor, hasMore, totalCount, "커서목록 전송완료");
        }

        var lastItem = executionItems.Last();
        nextCursor = CursorToken.CreateToken(lastItem.CreatedAt, lastItem.Id);

        return CursorPagedResult<TangwhaSchema.Response>.Success(
            mappedResponses,
            nextCursor,
            hasMore,
            totalCount,
            "커서목록 전송완료"
        );
    }

    public Task<OffsetPagedResult<TangwhaSchema.Response>> GetOffsetListAsync(OffsetPagedQuery query, CancellationToken ct)
    {
        // query.PageSize = Math.Clamp(query.PageSize, 1, 20); // 1 ~ 100
        // var q = con
        throw new NotImplementedException();
    }

    public async Task<CursorPagedResult<TangwhaSchema.Response>> GetSemanticListAsync(SemanticSearchQuery query, CancellationToken ct)
    {
        throw new NotImplementedException();
        // if (!string.IsNullOrWhiteSpace(query.Keyword))
        //     return await HandleVectorSearchAsync(query, ct);
    }

    public Task<Result> UpdateAsync(long id, TangwhaSchema.Request request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private static string SummarizeForEmbedding(TangwhaSchema.Request request)
    {
        var title = request.Title;
        var description = request.Description ?? string.Empty;

        return $"""
                제목: {title}
                본문: {description[..Math.Min(description.Length, 500)]}
                """;
    }
}
