using Buddham.Core.Entities.Scriptures;
using Buddham.Core.EnumTypes;
using Buddham.Core.Scriptures;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Scripture;

public sealed record DivisionInput(
    long CanonId, long? ParentId, string Code, string NameKo, string? NameOriginal, string? NameRoman,
    DivisionKind Kind, int SortOrder);

public sealed record CollectionInput(
    long DivisionId, string Code, string NameKo, string? NameOriginal, string? NameRoman,
    CollectionKind? Kind, int SortOrder,
    long? TraditionId, CertaintyLevel TraditionCertainty, string? TraditionNote);

public sealed record GroupInput(
    long CollectionId, long? ParentId, string Code, string NameKo, string? NameOriginal, string? NameRoman,
    GroupKind? Kind, int SortOrder);

/// <summary>
/// 판본계통(Canon) → 부(Division, 트리) → 집성(Collection) → 그룹(Group, 트리) 의 구조 규칙.
/// 모든 FK 가 Restrict 이므로 삭제는 "비어 있을 때만" soft delete 한다.
/// </summary>
public sealed class HierarchyService(ScriptureDbContext db) : ScriptureServiceBase(db)
{
    // ───────────────────────── 부(Division) ─────────────────────────

    public async Task<CanonDivision> CreateDivisionAsync(DivisionInput i, CancellationToken ct)
    {
        await EnsureExistsAsync<Canon>(i.CanonId, "판본계통", ct);
        if (i.ParentId is { } pid) await EnsureSameCanonParentAsync(i.CanonId, pid, ct);

        var e = new CanonDivision { CanonId = i.CanonId, ParentId = i.ParentId };
        ApplyDivision(e, i);
        Db.Add(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task<CanonDivision> UpdateDivisionAsync(long id, DivisionInput i, CancellationToken ct)
    {
        var e = await FindAsync<CanonDivision>(id, "부(部)", ct);
        if (i.CanonId != e.CanonId)
            throw Rule.Invalid("부(部)의 판본계통은 바꿀 수 없네. 새로 만들어 옮기게.");

        if (i.ParentId != e.ParentId)
        {
            if (i.ParentId is { } pid)
            {
                await EnsureSameCanonParentAsync(e.CanonId, pid, ct);
                await EnsureNoCycleAsync<CanonDivision>(e.Id, pid, "부(部)", "ParentId", ct);
            }
            e.ParentId = i.ParentId;
        }

        ApplyDivision(e, i);
        Touch(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task DeleteDivisionAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<CanonDivision>(id, "부(部)", ct);
        if (await AnyAsync<CanonDivision>(x => x.ParentId == id, ct)) throw Rule.Conflict("하위 부(部)가 있어 지울 수 없네.");
        if (await AnyAsync<CanonCollection>(x => x.DivisionId == id, ct)) throw Rule.Conflict("이 부(部)에 속한 집성이 있어 지울 수 없네.");
        SoftDelete(e);
        await SaveAsync(ct);
    }

    private async Task EnsureSameCanonParentAsync(long canonId, long parentId, CancellationToken ct)
    {
        var parent = await FindAsync<CanonDivision>(parentId, "상위 부(部)", ct);
        if (parent.CanonId != canonId) throw Rule.Invalid("상위 부(部)가 다른 판본계통에 속해 있네.");
    }

    private static void ApplyDivision(CanonDivision e, DivisionInput i)
    {
        ApplyCodedName(e, i.Code, i.NameKo, i.NameOriginal, i.NameRoman);
        e.Kind = i.Kind;
        e.SortOrder = i.SortOrder;
    }

    // ───────────────────────── 집성(Collection) ─────────────────────────

    public async Task<CanonCollection> CreateCollectionAsync(CollectionInput i, CancellationToken ct)
    {
        await EnsureExistsAsync<CanonDivision>(i.DivisionId, "부(部)", ct);
        await ValidateTraditionAsync(i, ct);

        var e = new CanonCollection { DivisionId = i.DivisionId };
        ApplyCollection(e, i);
        Db.Add(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task<CanonCollection> UpdateCollectionAsync(long id, CollectionInput i, CancellationToken ct)
    {
        var e = await FindAsync<CanonCollection>(id, "집성", ct);

        if (i.DivisionId != e.DivisionId)
        {
            // 다른 부로 옮기는 것은 같은 판본계통 안에서만 허용
            var from = await FindAsync<CanonDivision>(e.DivisionId, "기존 부(部)", ct);
            var to = await FindAsync<CanonDivision>(i.DivisionId, "새 부(部)", ct);
            if (from.CanonId != to.CanonId) throw Rule.Invalid("다른 판본계통의 부(部)로는 옮길 수 없네.");
            e.DivisionId = i.DivisionId;
        }

        await ValidateTraditionAsync(i, ct);
        ApplyCollection(e, i);
        Touch(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task DeleteCollectionAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<CanonCollection>(id, "집성", ct);
        if (await AnyAsync<CanonGroup>(x => x.CollectionId == id, ct)) throw Rule.Conflict("이 집성에 속한 그룹이 있어 지울 수 없네.");
        if (await AnyAsync<CanonWork>(x => x.CollectionId == id, ct)) throw Rule.Conflict("이 집성에 속한 경이 있어 지울 수 없네.");
        SoftDelete(e);
        await SaveAsync(ct);
    }

    /// <summary>
    /// 귀속은 불확실해도 된다(Unknown / Disputed / Probable 은 전승이 null 이어도 됨, 사유는 TraditionNote).
    /// 단 "확립(Established)"은 전승 지정이 필요하고, "미상(Unknown)"에 전승을 지정할 수는 없다.
    /// </summary>
    private async Task ValidateTraditionAsync(CollectionInput i, CancellationToken ct)
    {
        if (i.TraditionId is null && i.TraditionCertainty == CertaintyLevel.Established)
            throw Rule.Invalid("귀속이 '확립(Established)'이라면 전승을 지정해야 하네.");
        if (i.TraditionId is not null && i.TraditionCertainty == CertaintyLevel.Unknown)
            throw Rule.Invalid("전승을 지정했다면 확실성을 '미상(Unknown)'으로 둘 수 없네. 불확실하면 Probable 이나 Disputed 를 쓰게.");
        if (i.TraditionId is { } tid)
            await EnsureExistsAsync<CanonTradition>(tid, "전승 계통", ct);
    }

    private static void ApplyCollection(CanonCollection e, CollectionInput i)
    {
        ApplyCodedName(e, i.Code, i.NameKo, i.NameOriginal, i.NameRoman);
        e.Kind = i.Kind;
        e.SortOrder = i.SortOrder;
        e.TraditionId = i.TraditionId;
        e.TraditionCertainty = i.TraditionCertainty;
        e.TraditionNote = ScriptureText.Blank(i.TraditionNote);
    }

    // ───────────────────────── 그룹(Group, 가변 깊이 트리) ─────────────────────────

    public async Task<CanonGroup> CreateGroupAsync(GroupInput i, CancellationToken ct)
    {
        await EnsureExistsAsync<CanonCollection>(i.CollectionId, "집성", ct);
        if (i.ParentId is { } pid) await EnsureSameCollectionParentAsync(i.CollectionId, pid, ct);

        var e = new CanonGroup { CollectionId = i.CollectionId, ParentId = i.ParentId };
        ApplyGroup(e, i);
        Db.Add(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task<CanonGroup> UpdateGroupAsync(long id, GroupInput i, CancellationToken ct)
    {
        var e = await FindAsync<CanonGroup>(id, "그룹", ct);
        if (i.CollectionId != e.CollectionId)
            throw Rule.Invalid("그룹의 집성은 바꿀 수 없네. 새로 만들어 옮기게.");

        if (i.ParentId != e.ParentId)
        {
            if (i.ParentId is { } pid)
            {
                await EnsureSameCollectionParentAsync(e.CollectionId, pid, ct);
                await EnsureNoCycleAsync<CanonGroup>(e.Id, pid, "그룹", "ParentId", ct);
            }
            e.ParentId = i.ParentId;
        }

        ApplyGroup(e, i);
        Touch(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task DeleteGroupAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<CanonGroup>(id, "그룹", ct);
        if (await AnyAsync<CanonGroup>(x => x.ParentId == id, ct)) throw Rule.Conflict("하위 그룹이 있어 지울 수 없네.");
        if (await AnyAsync<CanonWork>(x => x.GroupId == id, ct)) throw Rule.Conflict("이 그룹에 속한 경이 있어 지울 수 없네.");
        SoftDelete(e);
        await SaveAsync(ct);
    }

    private async Task EnsureSameCollectionParentAsync(long collectionId, long parentId, CancellationToken ct)
    {
        var parent = await FindAsync<CanonGroup>(parentId, "상위 그룹", ct);
        if (parent.CollectionId != collectionId) throw Rule.Invalid("상위 그룹이 다른 집성에 속해 있네.");
    }

    private static void ApplyGroup(CanonGroup e, GroupInput i)
    {
        ApplyCodedName(e, i.Code, i.NameKo, i.NameOriginal, i.NameRoman);
        e.Kind = i.Kind;
        e.SortOrder = i.SortOrder;
    }

    // ───────────────────────── 판본계통·전승 삭제 가드 ─────────────────────────
    // (Canon / Tradition 의 단순 CRUD 는 코드 중복 검사 외 규칙이 없어 컨트롤러에서 직접 처리해도 된다.)

    public async Task DeleteCanonAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<Canon>(id, "판본계통", ct);
        if (await AnyAsync<CanonDivision>(x => x.CanonId == id, ct)) throw Rule.Conflict("이 판본계통에 속한 부(部)가 있어 지울 수 없네.");
        SoftDelete(e);
        await SaveAsync(ct);
    }

    public async Task DeleteTraditionAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<CanonTradition>(id, "전승 계통", ct);
        if (await AnyAsync<CanonTradition>(x => x.ParentId == id, ct)) throw Rule.Conflict("하위 전승이 있어 지울 수 없네.");
        if (await AnyAsync<Canon>(x => x.TraditionId == id, ct)) throw Rule.Conflict("이 전승을 쓰는 판본계통이 있어 지울 수 없네.");
        if (await AnyAsync<CanonCollection>(x => x.TraditionId == id, ct)) throw Rule.Conflict("이 전승을 쓰는 집성이 있어 지울 수 없네.");
        SoftDelete(e);
        await SaveAsync(ct);
    }

    /// <summary>전승 트리의 부모 이동(순환 방지 포함).</summary>
    public async Task MoveTraditionAsync(long id, long? newParentId, CancellationToken ct)
    {
        var e = await FindAsync<CanonTradition>(id, "전승 계통", ct);
        if (newParentId is { } pid)
        {
            await EnsureExistsAsync<CanonTradition>(pid, "상위 전승", ct);
            await EnsureNoCycleAsync<CanonTradition>(id, pid, "전승", "ParentId", ct);
        }
        e.ParentId = newParentId;
        Touch(e);
        await SaveAsync(ct);
    }
}
