using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Crud;

// ══════════════════════════════════════════════════════════
// 2) 키워드 검색: 필드만 선언하면 ILIKE OR 조건을 만들어 준다
// ══════════════════════════════════════════════════════════

public static class LikeSearch
{
    public static string? Normalize(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

    public static string ToPattern(string keyword)
        => "%" + keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    // EF 가 상수가 아니라 "파라미터"로 인식하도록 객체 멤버로 감싼다 (쿼리 플랜 캐시 오염 방지).
    private sealed class PatternHolder(string value)
    {
        public string Value { get; } = value;
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }

    /// <summary>정규화된 키워드를 받는다. 길이 초과 시 BadRequest.</summary>
    public static IQueryable<T> WhereAnyILike<T>(
        this IQueryable<T> query, string? keyword, IReadOnlyList<Expression<Func<T, string?>>> fields)
    {
        if (keyword is null || fields.Count == 0)
            return query;

        Paging.EnsureKeyword(keyword);

        var pattern = Expression.Property(
            Expression.Constant(new PatternHolder(ToPattern(keyword))), nameof(PatternHolder.Value));
        var efFunctions = Expression.Property(null, typeof(EF), nameof(EF.Functions));
        var ilike = typeof(NpgsqlDbFunctionsExtensions).GetMethod(
            nameof(NpgsqlDbFunctionsExtensions.ILike),
            [typeof(DbFunctions), typeof(string), typeof(string)])!;

        var param = Expression.Parameter(typeof(T), "x");
        Expression? body = null;

        foreach (var field in fields)
        {
            var member = new ParameterReplacer(field.Parameters[0], param).Visit(field.Body);
            var call = Expression.Call(ilike, efFunctions, member, pattern);
            body = body is null ? call : Expression.OrElse(body, call);
        }

        return query.Where(Expression.Lambda<Func<T, bool>>(body!, param));
    }
}
