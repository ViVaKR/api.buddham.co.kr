namespace Buddham.Shared.Extensions.Persistence;

public static class QueryablePaginationExtensions
{
    /// <summary>
    /// Offset 페이징을 적용한다. page와 size의 유효성은 호출 경계에서 검증한다.
    /// Skip의 int 범위를 초과하면 빈 쿼리를 반환한다.
    /// </summary>
    public static IQueryable<T> ApplyOffset<T>(
        this IQueryable<T> query,
        int page,
        int size)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        var skip = ((long)page - 1) * size;
        return skip > int.MaxValue
            ? query.Where(_ => false)
            : query.Skip((int)skip).Take(size);
    }

    /// <summary>
    /// Cursor 페이지 조회에 필요한 다음 항목 판별용 Take(size + 1).
    /// </summary>
    public static IQueryable<T> TakeCursorWindow<T>(
        this IQueryable<T> query,
        int size)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (size < 1 || size == int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(size));

        return query.Take(size + 1);
    }
}
