using System.Linq.Expressions;
using Buddham.Core.Common;
using Buddham.Core.Common.Primitives;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces;
using Buddham.Core.Interfaces.Cache;
using Buddham.Core.Interfaces.Scripture;
using Buddham.Core.Scriptures;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Buddham.Shared.Services.Scripture;

// ───────────────────────── 규칙 위반 ─────────────────────────

public enum RuleFailure { NotFound, Invalid, Conflict, Forbidden }

/// <summary>
/// 서비스 내부에서 규칙 위반을 즉시 중단시키는 용도. 서비스 경계(ScriptureResults)에서 Result 로 바뀌므로
/// 컨트롤러·클라이언트에는 기존 Result 봉투만 보인다.
/// </summary>
public sealed class ScriptureRuleException(RuleFailure failure, string message) : Exception(message)
{
    public RuleFailure Failure { get; } = failure;
}

internal static class Rule
{
    public static ScriptureRuleException NotFound(string what, long id) => new(RuleFailure.NotFound, $"{what}(#{id})를 찾을 수 없네.");
    public static ScriptureRuleException Invalid(string message) => new(RuleFailure.Invalid, message);
    public static ScriptureRuleException Conflict(string message) => new(RuleFailure.Conflict, message);
    public static ScriptureRuleException Forbidden(string message) => new(RuleFailure.Forbidden, message);
}

/// <summary>규칙 예외 → 기존 Result / RsCode 변환. 서비스의 공개 메서드는 이것으로 감싼다.</summary>
public static class ScriptureResults
{
    public static RsCode ToRsCode(this RuleFailure failure) => failure switch
    {
        RuleFailure.NotFound => RsCode.NotFound,
        RuleFailure.Conflict => RsCode.Conflict,
        RuleFailure.Forbidden => RsCode.Forbidden,
        _ => RsCode.UnprocessableEntity,
    };

    public static async Task<Result<T>> RunAsync<T>(Func<Task<T>> action, bool created = false)
    {
        try
        {
            var value = await action();
            return created ? Result<T>.Created(value) : Result<T>.Success(value);
        }
        catch (ScriptureRuleException ex)
        {
            return Result<T>.Failure(ex.Failure.ToRsCode(), ex.Message);
        }
    }

    /// <summary>오프셋 페이징 응답용. 규칙 위반은 같은 봉투(IsSuccess=false)로 돌려준다.</summary>
    public static async Task<OffsetPagedResult<T>> RunOffsetAsync<T>(Func<Task<OffsetPagedResult<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (ScriptureRuleException ex)
        {
            return new OffsetPagedResult<T> { IsSuccess = false, Message = ex.Message, Code = ex.Failure.ToRsCode() };
        }
    }

    /// <summary>커서 페이징 응답용.</summary>
    public static async Task<CursorPagedResult<T>> RunCursorAsync<T>(Func<Task<CursorPagedResult<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (ScriptureRuleException ex)
        {
            return new CursorPagedResult<T> { IsSuccess = false, Message = ex.Message, Code = ex.Failure.ToRsCode() };
        }
    }

    public static async Task<Result> RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Result.Success();
        }
        catch (ScriptureRuleException ex)
        {
            return Result.Failure(ex.Failure.ToRsCode(), ex.Message);
        }
    }
}

// ───────────────────────── soft-delete 확장 ─────────────────────────

public static class ScriptureQueryExtensions
{
    /// <summary>ISoftDeletable 의 DeletedAt IS NULL. 전역 쿼리 필터가 있어도 무해하다.</summary>
    public static IQueryable<T> Alive<T>(this IQueryable<T> query) where T : class, ISoftDeletable
        => query.Where(x => x.DeletedAt == null);
}

// ───────────────────────── 서비스 베이스 ─────────────────────────

/// <param name="db">경전 DbContext</param>
/// <param name="cache">캐시를 무효화해야 하는(트리 등을 바꾸는) 서비스만 넘긴다. 읽기 전용 서비스는 생략.</param>
public abstract class ScriptureServiceBase(ScriptureDbContext db, IRedisCacheService? cache = null)
{
    private const int MaxTreeDepth = 32;

    /// <summary>Configuration 에 정의된 제약 이름 → 사용자에게 보여줄 문장.</summary>
    private static readonly Dictionary<string, string> ConstraintMessages = new(StringComparer.Ordinal)
    {
        ["ux_canons_code"] = "이미 같은 코드의 판본계통이 있네.",
        ["ux_canon_traditions_code"] = "이미 같은 코드의 전승 계통이 있네.",
        ["ux_canon_divisions_canon_code"] = "이 판본계통 안에 같은 코드의 부(部)가 이미 있네. (부모가 달라도 중복 불가)",
        ["ux_canon_collections_division_code"] = "이 부(部) 안에 같은 코드의 집성이 이미 있네.",
        ["ux_canon_groups_collection_code"] = "이 집성 안에 같은 코드의 그룹이 이미 있네. (부모가 달라도 중복 불가)",
        ["ux_canon_works_collection_code"] = "이 집성 안에 같은 코드의 경이 이미 있네.",
        ["ux_canon_work_identifiers_scheme_value"] = "같은 체계의 같은 번호를 다른 경이 이미 쓰고 있네.",
        ["ux_canon_work_identifiers_one_primary"] = "경마다 대표 식별자는 하나뿐이네.",
        ["ux_canon_segments_edition_ref"] = "이 판본 안에 같은 주소(ref)의 세그먼트가 이미 있네.",
        ["ux_canon_segment_topics_segment_topic"] = "이 세그먼트에는 이미 그 주제가 달려 있네.",
        ["ux_lekhana_entries_session_segment"] = "이 세션에서 그 세그먼트는 이미 기록되었네.",
        ["ux_dhamma_topics_code"] = "이미 같은 코드의 주제가 있네.",
        ["ux_canon_speakers_code"] = "이미 같은 코드의 발화자가 있네.",
        ["ux_canon_sources_code"] = "이미 같은 코드의 출전이 있네.",
        ["ck_canon_parallels_distinct_works"] = "같은 경끼리는 병행경으로 이을 수 없네.",
    };

    private static string Describe(string? constraint, string fallback)
        => constraint is not null && ConstraintMessages.TryGetValue(constraint, out var message)
            ? message
            : $"{fallback} ({constraint})";

    protected ScriptureDbContext Db { get; } = db;

    protected static DateTime Now => DateTime.UtcNow;

    /// <summary>접두어로 시작하는 캐시를 지운다. 저장이 성공한 뒤에만 호출한다.</summary>
    protected Task InvalidateAsync(string prefix)
        => cache is null ? Task.CompletedTask : cache.RemoveByPrefixAsync(prefix);

    // ----- 조회 -----

    protected async Task<T> FindAsync<T>(long id, string what, CancellationToken ct) where T : EntityBase
        => await Db.Set<T>().Alive().FirstOrDefaultAsync(x => x.Id == id, ct)
           ?? throw Rule.NotFound(what, id);

    protected async Task EnsureExistsAsync<T>(long id, string what, CancellationToken ct) where T : EntityBase
    {
        if (!await Db.Set<T>().Alive().AnyAsync(x => x.Id == id, ct))
            throw Rule.NotFound(what, id);
    }

    protected Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct) where T : EntityBase
        => Db.Set<T>().Alive().AnyAsync(predicate, ct);

    // ----- 변경 -----

    protected void Touch(EntityBase entity) => entity.UpdatedAt = Now;

    protected void SoftDelete(EntityBase entity)
    {
        entity.DeletedAt = Now;
        entity.UpdatedAt = Now;
    }

    /// <summary>unique/FK 위반을 사용자에게 의미 있는 규칙 예외로 바꿔 저장한다.</summary>
    protected async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await Db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            Exception mapped = pg.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => Rule.Conflict(Describe(pg.ConstraintName, "이미 같은 값이 있네.")),
                PostgresErrorCodes.ForeignKeyViolation => Rule.Invalid(Describe(pg.ConstraintName, "참조 관계가 맞지 않네.")),
                PostgresErrorCodes.CheckViolation => Rule.Invalid(Describe(pg.ConstraintName, "허용되지 않는 값이네.")),
                _ => ex,
            };
            throw mapped;
        }
    }

    // ----- 입력 정규화 -----

    protected static string NormalizeCode(string? raw)
        => ScriptureText.TryNormalizeCode(raw, out var code)
            ? code
            : throw Rule.Invalid("코드는 소문자 영숫자와 . _ - 만 쓸 수 있고 100자 이내여야 하네.");

    protected static string RequireText(string? raw, string label, int maxLength)
    {
        var s = ScriptureText.Clean(raw);
        if (s.Length == 0) throw Rule.Invalid($"{label}은(는) 비워둘 수 없네.");
        if (s.Length > maxLength) throw Rule.Invalid($"{label}은(는) {maxLength}자를 넘을 수 없네.");
        return s;
    }

    protected static string? OptionalText(string? raw, string label, int maxLength)
    {
        var s = ScriptureText.Blank(raw);
        if (s is not null && s.Length > maxLength) throw Rule.Invalid($"{label}은(는) {maxLength}자를 넘을 수 없네.");
        return s;
    }

    /// <summary>코드 + 이름 3종(한글·원어·로마자)을 공유하는 엔터티의 공통 입력 처리 (ICodedName).</summary>
    protected static void ApplyCodedName<T>(T e, string? code, string? nameKo, string? nameOriginal, string? nameRoman)
        where T : ICodedName
    {
        e.Code = NormalizeCode(code);
        e.NameKo = RequireText(nameKo, "한글 이름", 300);
        e.NameOriginal = OptionalText(nameOriginal, "원어 이름", 500);
        e.NameRoman = OptionalText(nameRoman, "로마자 이름", 500);
    }

    // ----- 자기참조 트리 -----

    /// <summary>
    /// selfId 를 newParentId 아래로 옮겨도 순환이 생기지 않는지 확인한다.
    /// newParentId 에서 위로 올라가며 selfId 를 만나면 순환이다.
    /// </summary>
    protected async Task EnsureNoCycleAsync<T>(long selfId, long? newParentId, string label, string parentProperty, CancellationToken ct)
        where T : EntityBase
    {
        var current = newParentId;
        var hops = 0;
        while (current is { } id)
        {
            if (id == selfId) throw Rule.Invalid($"{label} 계층이 순환하게 되네.");
            if (++hops > MaxTreeDepth) throw Rule.Invalid($"{label} 계층이 너무 깊네.");

            current = await Db.Set<T>()
                .Where(x => x.Id == id)
                .Select(x => EF.Property<long?>(x, parentProperty))
                .FirstOrDefaultAsync(ct);
        }
    }
}
