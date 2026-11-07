using System.Linq.Expressions;
using Buddham.Core.Common;
using Buddham.Shared.Middlewares;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Extensions.Persistence;

/// <summary>
/// 커서 페이징 옵션.
/// Keyword 는 서비스에서 정규화(Trim)한 값을 넘기는 것을 권장한다 (여기서도 한 번 더 Trim 한다).
/// </summary>
public sealed record CursorSliceOptions(string? Cursor, int Size, int MaxSize, string? Keyword, bool Descending, bool IncludeTotal = true);

/// <summary>
/// 커서 페이징 결과(전송용 Result 로 감싸기 전의 순수 데이터).
/// TotalCount 는 첫 페이지에서만 계산되며, 계산하지 않았으면 null.
/// </summary>
public sealed record CursorSlice<T>(List<T> Items, string? NextCursor, bool HasMore, int? TotalCount);

public static class QueryableCursorExtensions
{
    /// <summary>
    /// (CreatedAt, Id) 복합 키 기반 커서 페이징.
    /// 정렬 방향과 커서 비교 방향은 항상 동일한 Descending 값으로 결정되므로 중복/누락이 없다.
    /// </summary>
    /// <param name="source">필터(키워드, 소프트 삭제 등)가 모두 적용된 쿼리. 커서 조건은 여기서 추가한다.</param>
    /// <param name="createdAt">정렬 1순위 키 선택자 (예: x => x.CreatedAt)</param>
    /// <param name="id">정렬 2순위(타이브레이커) 키 선택자 (예: x => x.Id)</param>
    /// <param name="project">엔티티 → 응답 프로젝션 (예: q => q.AsSummary())</param>
    /// <param name="keyOf">프로젝션 결과에서 커서 키를 꺼내는 함수 (예: r => (r.CreatedAt, r.Id))</param>
    public static async Task<CursorSlice<TResult>> ToCursorSliceAsync<TEntity, TResult>(
        this IQueryable<TEntity> source,
        Expression<Func<TEntity, DateTime>> createdAt,
        Expression<Func<TEntity, long>> id,
        Func<IQueryable<TEntity>, IQueryable<TResult>> project,
        Func<TResult, (DateTime? SortValue, long Id)> keyOf,
        CursorSliceOptions options,
        CancellationToken ct)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(createdAt);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(keyOf);
        ArgumentNullException.ThrowIfNull(options);

        // 1) 입력 검증 (가장 먼저)
        if (options.Size < 1 || options.Size > options.MaxSize)
            throw new BuddhamException(RsCode.BadRequest, $"페이지 크기는 1~{options.MaxSize} 사이여야 합니다.");

        var keyword = string.IsNullOrWhiteSpace(options.Keyword) ? null : options.Keyword.Trim();
        var descending = options.Descending;

        // 2) 첫 페이지: 전체 개수 / 이후 페이지: 커서 검증 + 조건 적용
        int? totalCount = null;

        if (string.IsNullOrWhiteSpace(options.Cursor))
        {
            if (options.IncludeTotal)
                totalCount = await source.CountAsync(ct);
        }
        else
        {
            if (!CursorToken.TryParseToken(options.Cursor, out var token)
                || token is null
                || token.Keyword != keyword
                || token.Descending != descending)
                throw new BuddhamException(RsCode.BadRequest, "유효하지 않거나 조회 조건이 변경된 커서입니다.");

            var cursorAt = new DateTime(token.Ticks, DateTimeKind.Utc);
            source = source.Where(BuildCursorFilter(createdAt, id, cursorAt, token.Id, descending));
        }

        // 3) 정렬 (타이브레이커 Id 필수)
        var ordered = descending
            ? source.OrderByDescending(createdAt).ThenByDescending(id)
            : source.OrderBy(createdAt).ThenBy(id);

        // 4) size + 1 조회로 다음 페이지 존재 여부 판별
        var items = await project(ordered.TakeCursorWindow(options.Size)).ToListAsync(ct);

        var hasMore = items.Count > options.Size;
        if (hasMore) items.RemoveAt(items.Count - 1);

        string? nextCursor = null;
        if (hasMore)
        {
            var (sortValue, lastId) = keyOf(items[^1]);
            nextCursor = CursorToken.CreateToken(sortValue, lastId, keyword, descending);
        }

        return new CursorSlice<TResult>(items, nextCursor, hasMore, totalCount);
    }

    /// <summary>
    /// 내림차순: (at &lt; c) || (at == c &amp;&amp; id &lt; cid)
    /// 오름차순: (at &gt; c) || (at == c &amp;&amp; id &gt; cid)
    /// 커서 값은 클로저(Box) 멤버 접근으로 넣어 EF 가 SQL 파라미터로 처리하게 한다
    /// (Constant 로 직접 넣으면 커서마다 SQL 리터럴이 달라져 쿼리 플랜 캐시가 오염된다).
    /// </summary>
    private static Expression<Func<T, bool>> BuildCursorFilter<T>(
        Expression<Func<T, DateTime>> createdAt,
        Expression<Func<T, long>> id,
        DateTime cursorAt,
        long cursorId,
        bool descending)
    {
        var parameter = createdAt.Parameters[0];
        var atExpr = createdAt.Body;
        var idExpr = new ParameterReplacer(id.Parameters[0], parameter).Visit(id.Body)!;

        Expression atValue = Expression.Property(
            Expression.Constant(new Box<DateTime>(cursorAt)), nameof(Box<DateTime>.Value));
        Expression idValue = Expression.Property(
            Expression.Constant(new Box<long>(cursorId)), nameof(Box<long>.Value));

        Expression atCompare = descending
            ? Expression.LessThan(atExpr, atValue)
            : Expression.GreaterThan(atExpr, atValue);

        Expression idCompare = descending
            ? Expression.LessThan(idExpr, idValue)
            : Expression.GreaterThan(idExpr, idValue);

        var body = Expression.OrElse(
            atCompare,
            Expression.AndAlso(Expression.Equal(atExpr, atValue), idCompare));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private sealed record Box<TValue>(TValue Value);

    private sealed class ParameterReplacer(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
