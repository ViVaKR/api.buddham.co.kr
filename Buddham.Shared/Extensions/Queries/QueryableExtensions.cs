namespace Buddham.Shared.Extensions.Queries;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        int page,
        int size
    )
    {
        var skip = (long)(page - 1) * size;

        if (skip > int.MaxValue) return query.Where(_ => false);

        return query.Skip((int)skip).Take(size);
    }
}
