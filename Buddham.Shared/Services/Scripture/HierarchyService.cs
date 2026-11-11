using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Scriptures;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Cache;
using Buddham.Core.Interfaces.Scripture;
using Buddham.Core.Scriptures;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Schema = Buddham.Core.Schemas.ScriptureSchema;

namespace Buddham.Shared.Services.Scripture;

/// <summary>
/// 전승 · 판본계통(Canon) → 부(Division, 트리) → 집성(Collection) → 그룹(Group, 트리) 의 구조 규칙.
/// 모든 FK 가 Restrict 이므로 삭제는 "비어 있을 때만" soft delete 한다.
/// 판본계통·부·집성·그룹을 바꾸면 메뉴 트리 캐시를 무효화한다.
/// </summary>
public sealed class HierarchyService(ScriptureDbContext db, IRedisCacheService cache)
    : ScriptureServiceBase(db, cache), IHierarchyService
{
    // ═════════════════════ 전승 계통 ═════════════════════

    public Task<Result<IReadOnlyList<Schema.Tradition.Response>>> GetTraditionsAsync(CancellationToken ct)
        => ScriptureResults.RunAsync<IReadOnlyList<Schema.Tradition.Response>>(async () =>
            await Db.Set<CanonTradition>().AsNoTracking().Alive().OrderBy(x => x.Code).AsResponse().ToListAsync(ct));

    public Task<Result<Schema.Tradition.Response>> CreateTraditionAsync(Schema.Tradition.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            if (r.ParentId is { } pid) await EnsureExistsAsync<CanonTradition>(pid, "상위 전승", ct);

            var e = new CanonTradition { ParentId = r.ParentId };
            ApplyTradition(e, r);
            Db.Add(e);
            await SaveAsync(ct);
            return await ReadTraditionAsync(e.Id, ct);
        }, created: true);

    public Task<Result<Schema.Tradition.Response>> UpdateTraditionAsync(long id, Schema.Tradition.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonTradition>(id, "전승 계통", ct);

            if (r.ParentId != e.ParentId)
            {
                if (r.ParentId is { } pid)
                {
                    await EnsureExistsAsync<CanonTradition>(pid, "상위 전승", ct);
                    await EnsureNoCycleAsync<CanonTradition>(id, pid, "전승", "ParentId", ct);
                }
                e.ParentId = r.ParentId;
            }

            ApplyTradition(e, r);
            Touch(e);
            await SaveAsync(ct);
            return await ReadTraditionAsync(id, ct);
        });

    public Task<Result> DeleteTraditionAsync(long id, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonTradition>(id, "전승 계통", ct);
            if (await AnyAsync<CanonTradition>(x => x.ParentId == id, ct)) throw Rule.Conflict("하위 전승이 있어 지울 수 없네.");
            if (await AnyAsync<Canon>(x => x.TraditionId == id, ct)) throw Rule.Conflict("이 전승을 쓰는 판본계통이 있어 지울 수 없네.");
            if (await AnyAsync<CanonCollection>(x => x.TraditionId == id, ct)) throw Rule.Conflict("이 전승을 쓰는 집성이 있어 지울 수 없네.");
            SoftDelete(e);
            await SaveAsync(ct);
        });

    private static void ApplyTradition(CanonTradition e, Schema.Tradition.Request r)
    {
        ApplyCodedName(e, r.Code, r.NameKo, r.NameOriginal, r.NameRoman);
        e.Description = ScriptureText.Blank(r.Description);
    }

    private Task<Schema.Tradition.Response> ReadTraditionAsync(long id, CancellationToken ct)
        => Db.Set<CanonTradition>().AsNoTracking().Where(x => x.Id == id).AsResponse().FirstAsync(ct);

    // ═════════════════════ 판본계통 ═════════════════════

    public Task<Result<IReadOnlyList<Schema.Canon.Response>>> GetCanonsAsync(CancellationToken ct)
        => ScriptureResults.RunAsync<IReadOnlyList<Schema.Canon.Response>>(async () =>
            await Db.Set<Canon>().AsNoTracking().Alive().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).AsResponse().ToListAsync(ct));

    public Task<Result<Schema.Canon.Response>> CreateCanonAsync(Schema.Canon.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = new Canon();
            await ApplyCanonAsync(e, r, ct);
            Db.Add(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadCanonAsync(e.Id, ct);
        }, created: true);

    public Task<Result<Schema.Canon.Response>> UpdateCanonAsync(long id, Schema.Canon.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<Canon>(id, "판본계통", ct);
            await ApplyCanonAsync(e, r, ct);
            Touch(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadCanonAsync(id, ct);
        });

    public Task<Result> DeleteCanonAsync(long id, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<Canon>(id, "판본계통", ct);
            if (await AnyAsync<CanonDivision>(x => x.CanonId == id, ct)) throw Rule.Conflict("이 판본계통에 속한 부(部)가 있어 지울 수 없네.");
            SoftDelete(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
        });

    private async Task ApplyCanonAsync(Canon e, Schema.Canon.Request r, CancellationToken ct)
    {
        if (r.TraditionId is { } tid) await EnsureExistsAsync<CanonTradition>(tid, "전승 계통", ct);

        var language = ScriptureText.Blank(r.PrimaryLanguage);
        if (language is not null)
        {
            if (!ScriptureText.TryNormalizeLanguage(language, out var normalized))
                throw Rule.Invalid("주 언어 코드가 올바르지 않네. (예: pi, lzh, sa, bo, ko)");
            language = normalized;
        }

        ApplyCodedName(e, r.Code, r.NameKo, r.NameOriginal, r.NameRoman);
        e.TraditionId = r.TraditionId;
        e.PrimaryLanguage = language;
        e.Description = ScriptureText.Blank(r.Description);
        e.SortOrder = r.SortOrder;
    }

    private Task<Schema.Canon.Response> ReadCanonAsync(long id, CancellationToken ct)
        => Db.Set<Canon>().AsNoTracking().Where(x => x.Id == id).AsResponse().FirstAsync(ct);

    // ═════════════════════ 부(Division) ═════════════════════

    public Task<Result<IReadOnlyList<Schema.Division.Response>>> GetDivisionsAsync(long? canonId, CancellationToken ct)
        => ScriptureResults.RunAsync<IReadOnlyList<Schema.Division.Response>>(async () =>
        {
            var q = Db.Set<CanonDivision>().AsNoTracking().Alive();
            if (canonId is { } cid) q = q.Where(x => x.CanonId == cid);
            return await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).AsResponse().ToListAsync(ct);
        });

    public Task<Result<Schema.Division.Response>> CreateDivisionAsync(Schema.Division.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            await EnsureExistsAsync<Canon>(r.CanonId, "판본계통", ct);
            if (r.ParentId is { } pid) await EnsureSameCanonParentAsync(r.CanonId, pid, ct);

            var e = new CanonDivision { CanonId = r.CanonId, ParentId = r.ParentId };
            ApplyDivision(e, r);
            Db.Add(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadDivisionAsync(e.Id, ct);
        }, created: true);

    public Task<Result<Schema.Division.Response>> UpdateDivisionAsync(long id, Schema.Division.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonDivision>(id, "부(部)", ct);
            if (r.CanonId != e.CanonId)
                throw Rule.Invalid("부(部)의 판본계통은 바꿀 수 없네. 새로 만들어 옮기게.");

            if (r.ParentId != e.ParentId)
            {
                if (r.ParentId is { } pid)
                {
                    await EnsureSameCanonParentAsync(e.CanonId, pid, ct);
                    await EnsureNoCycleAsync<CanonDivision>(e.Id, pid, "부(部)", "ParentId", ct);
                }
                e.ParentId = r.ParentId;
            }

            ApplyDivision(e, r);
            Touch(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadDivisionAsync(id, ct);
        });

    public Task<Result> DeleteDivisionAsync(long id, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonDivision>(id, "부(部)", ct);
            if (await AnyAsync<CanonDivision>(x => x.ParentId == id, ct)) throw Rule.Conflict("하위 부(部)가 있어 지울 수 없네.");
            if (await AnyAsync<CanonCollection>(x => x.DivisionId == id, ct)) throw Rule.Conflict("이 부(部)에 속한 집성이 있어 지울 수 없네.");
            SoftDelete(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
        });

    private async Task EnsureSameCanonParentAsync(long canonId, long parentId, CancellationToken ct)
    {
        var parent = await FindAsync<CanonDivision>(parentId, "상위 부(部)", ct);
        if (parent.CanonId != canonId) throw Rule.Invalid("상위 부(部)가 다른 판본계통에 속해 있네.");
    }

    private static void ApplyDivision(CanonDivision e, Schema.Division.Request r)
    {
        ApplyCodedName(e, r.Code, r.NameKo, r.NameOriginal, r.NameRoman);
        e.Kind = r.Kind;
        e.SortOrder = r.SortOrder;
    }

    private Task<Schema.Division.Response> ReadDivisionAsync(long id, CancellationToken ct)
        => Db.Set<CanonDivision>().AsNoTracking().Where(x => x.Id == id).AsResponse().FirstAsync(ct);

    // ═════════════════════ 집성(Collection) ═════════════════════

    public Task<Result<IReadOnlyList<Schema.Collection.Response>>> GetCollectionsAsync(long? divisionId, CancellationToken ct)
        => ScriptureResults.RunAsync<IReadOnlyList<Schema.Collection.Response>>(async () =>
        {
            var q = Db.Set<CanonCollection>().AsNoTracking().Alive();
            if (divisionId is { } did) q = q.Where(x => x.DivisionId == did);
            return await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).AsResponse().ToListAsync(ct);
        });

    public Task<Result<Schema.Collection.Response>> CreateCollectionAsync(Schema.Collection.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            await EnsureExistsAsync<CanonDivision>(r.DivisionId, "부(部)", ct);
            await ValidateTraditionAsync(r, ct);

            var e = new CanonCollection { DivisionId = r.DivisionId };
            ApplyCollection(e, r);
            Db.Add(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadCollectionAsync(e.Id, ct);
        }, created: true);

    public Task<Result<Schema.Collection.Response>> UpdateCollectionAsync(long id, Schema.Collection.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonCollection>(id, "집성", ct);

            if (r.DivisionId != e.DivisionId)
            {
                // 다른 부로 옮기는 것은 같은 판본계통 안에서만 허용
                var from = await FindAsync<CanonDivision>(e.DivisionId, "기존 부(部)", ct);
                var to = await FindAsync<CanonDivision>(r.DivisionId, "새 부(部)", ct);
                if (from.CanonId != to.CanonId) throw Rule.Invalid("다른 판본계통의 부(部)로는 옮길 수 없네.");
                e.DivisionId = r.DivisionId;
            }

            await ValidateTraditionAsync(r, ct);
            ApplyCollection(e, r);
            Touch(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadCollectionAsync(id, ct);
        });

    public Task<Result> DeleteCollectionAsync(long id, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonCollection>(id, "집성", ct);
            if (await AnyAsync<CanonGroup>(x => x.CollectionId == id, ct)) throw Rule.Conflict("이 집성에 속한 그룹이 있어 지울 수 없네.");
            if (await AnyAsync<CanonWork>(x => x.CollectionId == id, ct)) throw Rule.Conflict("이 집성에 속한 경이 있어 지울 수 없네.");
            SoftDelete(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
        });

    /// <summary>
    /// 귀속은 불확실해도 된다(Unknown / Disputed / Probable 은 전승이 null 이어도 됨, 사유는 TraditionNote).
    /// 단 "확립(Established)"은 전승 지정이 필요하고, "미상(Unknown)"에 전승을 지정할 수는 없다.
    /// </summary>
    private async Task ValidateTraditionAsync(Schema.Collection.Request r, CancellationToken ct)
    {
        if (r.TraditionId is null && r.TraditionCertainty == CertaintyLevel.Established)
            throw Rule.Invalid("귀속이 '확립(Established)'이라면 전승을 지정해야 하네.");
        if (r.TraditionId is not null && r.TraditionCertainty == CertaintyLevel.Unknown)
            throw Rule.Invalid("전승을 지정했다면 확실성을 '미상(Unknown)'으로 둘 수 없네. 불확실하면 Probable 이나 Disputed 를 쓰게.");
        if (r.TraditionId is { } tid)
            await EnsureExistsAsync<CanonTradition>(tid, "전승 계통", ct);
    }

    private static void ApplyCollection(CanonCollection e, Schema.Collection.Request r)
    {
        ApplyCodedName(e, r.Code, r.NameKo, r.NameOriginal, r.NameRoman);
        e.Kind = r.Kind;
        e.SortOrder = r.SortOrder;
        e.TraditionId = r.TraditionId;
        e.TraditionCertainty = r.TraditionCertainty;
        e.TraditionNote = ScriptureText.Blank(r.TraditionNote);
    }

    private Task<Schema.Collection.Response> ReadCollectionAsync(long id, CancellationToken ct)
        => Db.Set<CanonCollection>().AsNoTracking().Where(x => x.Id == id).AsResponse().FirstAsync(ct);

    // ═════════════════════ 그룹(Group, 가변 깊이 트리) ═════════════════════

    public Task<Result<IReadOnlyList<Schema.Group.Response>>> GetGroupsAsync(long? collectionId, CancellationToken ct)
        => ScriptureResults.RunAsync<IReadOnlyList<Schema.Group.Response>>(async () =>
        {
            var q = Db.Set<CanonGroup>().AsNoTracking().Alive();
            if (collectionId is { } cid) q = q.Where(x => x.CollectionId == cid);
            return await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).AsResponse().ToListAsync(ct);
        });

    public Task<Result<Schema.Group.Response>> CreateGroupAsync(Schema.Group.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            await EnsureExistsAsync<CanonCollection>(r.CollectionId, "집성", ct);
            if (r.ParentId is { } pid) await EnsureSameCollectionParentAsync(r.CollectionId, pid, ct);

            var e = new CanonGroup { CollectionId = r.CollectionId, ParentId = r.ParentId };
            ApplyGroup(e, r);
            Db.Add(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadGroupAsync(e.Id, ct);
        }, created: true);

    public Task<Result<Schema.Group.Response>> UpdateGroupAsync(long id, Schema.Group.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonGroup>(id, "그룹", ct);
            if (r.CollectionId != e.CollectionId)
                throw Rule.Invalid("그룹의 집성은 바꿀 수 없네. 새로 만들어 옮기게.");

            if (r.ParentId != e.ParentId)
            {
                if (r.ParentId is { } pid)
                {
                    await EnsureSameCollectionParentAsync(e.CollectionId, pid, ct);
                    await EnsureNoCycleAsync<CanonGroup>(e.Id, pid, "그룹", "ParentId", ct);
                }
                e.ParentId = r.ParentId;
            }

            ApplyGroup(e, r);
            Touch(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadGroupAsync(id, ct);
        });

    public Task<Result> DeleteGroupAsync(long id, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonGroup>(id, "그룹", ct);
            if (await AnyAsync<CanonGroup>(x => x.ParentId == id, ct)) throw Rule.Conflict("하위 그룹이 있어 지울 수 없네.");
            if (await AnyAsync<CanonWork>(x => x.GroupId == id, ct)) throw Rule.Conflict("이 그룹에 속한 경이 있어 지울 수 없네.");
            SoftDelete(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
        });

    private async Task EnsureSameCollectionParentAsync(long collectionId, long parentId, CancellationToken ct)
    {
        var parent = await FindAsync<CanonGroup>(parentId, "상위 그룹", ct);
        if (parent.CollectionId != collectionId) throw Rule.Invalid("상위 그룹이 다른 집성에 속해 있네.");
    }

    private static void ApplyGroup(CanonGroup e, Schema.Group.Request r)
    {
        ApplyCodedName(e, r.Code, r.NameKo, r.NameOriginal, r.NameRoman);
        e.Kind = r.Kind;
        e.SortOrder = r.SortOrder;
    }

    private Task<Schema.Group.Response> ReadGroupAsync(long id, CancellationToken ct)
        => Db.Set<CanonGroup>().AsNoTracking().Where(x => x.Id == id).AsResponse().FirstAsync(ct);
}
