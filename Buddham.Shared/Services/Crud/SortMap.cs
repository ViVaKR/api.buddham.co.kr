using System.Linq.Expressions;
using Buddham.Core.Common.Primitives;
using Buddham.Core.Interfaces.Common;

namespace Buddham.Shared.Services.Crud;

// ══════════════════════════════════════════════════════════
// 3) 정렬: 허용 컬럼만 선언하면 된다 (화이트리스트 + Id 타이브레이커)
// ══════════════════════════════════════════════════════════

public sealed class SortMap<T> where T : EntityBase
{
    private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> _rules
        = new(StringComparer.OrdinalIgnoreCase);

    public SortMap<T> Add<TKey>(string name, Expression<Func<T, TKey>> key)
    {
        _rules[name] = (q, desc) => desc
            ? q.OrderByDescending(key).ThenByDescending(x => x.Id)
            : q.OrderBy(key).ThenBy(x => x.Id);
        return this;
    }

    public string? Resolve(string? sortBy)
    {
        var key = sortBy?.Trim();
        return key is not null && _rules.ContainsKey(key) ? key.ToLowerInvariant() : null;
    }

    /// <summary>모르는 정렬 키는 CreatedAt + Id 로 폴백한다.</summary>
    public IOrderedQueryable<T> Apply(IQueryable<T> query, string? sortBy, bool descending)
    {
        if (sortBy is not null && _rules.TryGetValue(sortBy.Trim(), out var rule))
            return rule(query, descending);

        return descending
            ? query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id);
    }
}
