using Buddham.Core.Entities.Scriptures;
using Buddham.Core.EnumTypes;
using Buddham.Core.Scriptures;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Scripture;

public sealed record WorkInput(
    long CollectionId, long? GroupId, string Code,
    string TitleKo, string? TitleOriginal, string? TitleRoman,
    WorkKind Kind, TextLayer Layer, AttributionKind Attribution, string? AttributionNote,
    long? SpeakerId, long? CommentaryOfWorkId, string? Summary, int SortOrder);

public sealed record IdentifierInput(string Scheme, string Value, bool IsPrimary = false);

public sealed record WorkMatch(long WorkId, long CollectionId, string Code, string TitleKo, string Scheme, string Value);

public sealed record WorkDetail(
    long Id, long CollectionId, long? GroupId, string Code, string TitleKo, string? TitleOriginal, string? TitleRoman,
    string? Summary, IReadOnlyList<WorkDetail.IdentifierView> Identifiers, IReadOnlyList<WorkDetail.EditionView> Editions)
{
    public sealed record IdentifierView(long Id, string Scheme, string Value, bool IsPrimary);
    public sealed record EditionView(long Id, string LanguageCode, string? Script, EditionKind Kind, string? Revision,
        EditionStatus Status, int SegmentCount, string? TranslatorName);
}

/// <summary>
/// 경 = 추상적 작품 단위. 실제 원문은 Edition/Segment 에 있고, 여기서는 위치·층위·귀속·식별자를 다룬다.
/// </summary>
public sealed class WorkService(ScriptureDbContext db) : ScriptureServiceBase(db)
{
    // ───────────────────────── 경 CRUD ─────────────────────────

    public async Task<CanonWork> CreateAsync(WorkInput i, IReadOnlyList<IdentifierInput>? identifiers, CancellationToken ct)
    {
        await ValidateAsync(null, i, ct);

        var prepared = PrepareIdentifiers(identifiers);
        await EnsureIdentifiersFreeAsync(prepared, excludeWorkId: null, ct);

        var e = new CanonWork();
        Apply(e, i);
        Db.Add(e);

        foreach (var id in prepared)
            Db.Add(new CanonWorkIdentifier { Work = e, Scheme = id.Scheme, Value = id.Value, IsPrimary = id.IsPrimary });

        await SaveAsync(ct);
        return e;
    }

    public async Task<CanonWork> UpdateAsync(long id, WorkInput i, CancellationToken ct)
    {
        var e = await FindAsync<CanonWork>(id, "경", ct);
        await ValidateAsync(id, i, ct);
        Apply(e, i);
        Touch(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<CanonWork>(id, "경", ct);

        if (await AnyAsync<CanonEdition>(x => x.WorkId == id, ct)) throw Rule.Conflict("판본이 있는 경은 지울 수 없네. 판본을 먼저 정리하게.");
        if (await AnyAsync<CanonWork>(x => x.CommentaryOfWorkId == id, ct)) throw Rule.Conflict("이 경을 주석 대상으로 삼는 경이 있어 지울 수 없네.");
        if (await AnyAsync<CanonParallel>(x => x.FromWorkId == id || x.ToWorkId == id, ct)) throw Rule.Conflict("병행경 관계가 걸려 있어 지울 수 없네.");

        // 식별자는 경과 함께 지워야 (scheme, value) 유일 인덱스가 풀린다.
        var identifiers = await Db.Set<CanonWorkIdentifier>().Alive().Where(x => x.WorkId == id).ToListAsync(ct);
        foreach (var identifier in identifiers) SoftDelete(identifier);

        SoftDelete(e);
        await SaveAsync(ct);
    }

    private async Task ValidateAsync(long? selfId, WorkInput i, CancellationToken ct)
    {
        await EnsureExistsAsync<CanonCollection>(i.CollectionId, "집성", ct);

        if (i.GroupId is { } gid)
        {
            var group = await FindAsync<CanonGroup>(gid, "그룹", ct);
            if (group.CollectionId != i.CollectionId) throw Rule.Invalid("그룹이 다른 집성에 속해 있네.");
        }

        if (i.SpeakerId is { } sid) await EnsureExistsAsync<CanonSpeaker>(sid, "화자", ct);

        // 층위 규칙: Root 는 주석 대상이 없고, Commentary / SubCommentary 는 반드시 대상이 있다. Other 는 선택.
        if (i.Layer == TextLayer.Root && i.CommentaryOfWorkId is not null)
            throw Rule.Invalid("원전(Root) 층의 경에는 주석 대상을 둘 수 없네.");
        if (i.Layer is TextLayer.Commentary or TextLayer.SubCommentary && i.CommentaryOfWorkId is null)
            throw Rule.Invalid("주석·복주 층의 경에는 주석 대상이 필요하네.");

        if (i.CommentaryOfWorkId is { } target)
        {
            await EnsureExistsAsync<CanonWork>(target, "주석 대상 경", ct);
            if (selfId is { } me)
            {
                if (target == me) throw Rule.Invalid("자기 자신을 주석할 수는 없네.");
                await EnsureNoCycleAsync<CanonWork>(me, target, "주석", "CommentaryOfWorkId", ct);
            }
        }

        var code = NormalizeCode(i.Code);
        if (await AnyAsync<CanonWork>(x => x.CollectionId == i.CollectionId && x.Code == code && x.Id != (selfId ?? 0), ct))
            throw Rule.Conflict($"이 집성에 이미 '{code}' 코드의 경이 있네.");
    }

    private static void Apply(CanonWork e, WorkInput i)
    {
        e.CollectionId = i.CollectionId;
        e.GroupId = i.GroupId;
        e.Code = NormalizeCode(i.Code);
        e.TitleKo = RequireText(i.TitleKo, "한글 제목", 500);
        e.TitleOriginal = OptionalText(i.TitleOriginal, "원어 제목", 1000);
        e.TitleRoman = OptionalText(i.TitleRoman, "로마자 제목", 500);
        e.Kind = i.Kind;
        e.Layer = i.Layer;
        e.Attribution = i.Attribution;
        e.AttributionNote = ScriptureText.Blank(i.AttributionNote);
        e.SpeakerId = i.SpeakerId;
        e.CommentaryOfWorkId = i.Layer == TextLayer.Root ? null : i.CommentaryOfWorkId;
        e.Summary = ScriptureText.Blank(i.Summary);
        e.SortOrder = i.SortOrder;
    }

    // ───────────────────────── 식별자 ─────────────────────────
    // 같은 경이 SN 12.2 / 잡아함 288 / T99 처럼 여러 번호를 갖는다. (scheme, value) 는 전체에서 유일, 경당 대표(primary)는 하나.

    public async Task<CanonWorkIdentifier> AddIdentifierAsync(long workId, IdentifierInput input, CancellationToken ct)
    {
        await EnsureExistsAsync<CanonWork>(workId, "경", ct);

        var prepared = PrepareIdentifiers([input with { IsPrimary = false }]);
        await EnsureIdentifiersFreeAsync(prepared, excludeWorkId: workId, ct);
        var item = prepared[0];

        var hasPrimary = await AnyAsync<CanonWorkIdentifier>(x => x.WorkId == workId && x.IsPrimary, ct);
        var wantsPrimary = input.IsPrimary || !hasPrimary; // 첫 식별자는 자동으로 대표

        await using var tx = await Db.Database.BeginTransactionAsync(ct);

        if (wantsPrimary && hasPrimary)
        {
            await Db.Set<CanonWorkIdentifier>().Alive()
                .Where(x => x.WorkId == workId && x.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false), ct);
        }

        var e = new CanonWorkIdentifier { WorkId = workId, Scheme = item.Scheme, Value = item.Value, IsPrimary = wantsPrimary };
        Db.Add(e);
        await SaveAsync(ct);
        await tx.CommitAsync(ct);
        return e;
    }

    public async Task SetPrimaryIdentifierAsync(long workId, long identifierId, CancellationToken ct)
    {
        var target = await Db.Set<CanonWorkIdentifier>().Alive()
            .FirstOrDefaultAsync(x => x.Id == identifierId && x.WorkId == workId, ct)
            ?? throw Rule.NotFound("식별자", identifierId);
        if (target.IsPrimary) return;

        // 부분 유일 인덱스(경당 대표 1개)는 문장 단위로 검사되므로 먼저 내리고 올린다.
        await using var tx = await Db.Database.BeginTransactionAsync(ct);
        await Db.Set<CanonWorkIdentifier>().Alive()
            .Where(x => x.WorkId == workId && x.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false), ct);

        target.IsPrimary = true;
        Touch(target);
        await SaveAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task RemoveIdentifierAsync(long workId, long identifierId, CancellationToken ct)
    {
        var target = await Db.Set<CanonWorkIdentifier>().Alive()
            .FirstOrDefaultAsync(x => x.Id == identifierId && x.WorkId == workId, ct)
            ?? throw Rule.NotFound("식별자", identifierId);

        await using var tx = await Db.Database.BeginTransactionAsync(ct);

        var wasPrimary = target.IsPrimary;
        target.IsPrimary = false;
        SoftDelete(target);
        await SaveAsync(ct);

        // 대표를 지웠다면 가장 먼저 등록된 나머지를 대표로 승격
        if (wasPrimary)
        {
            var next = await Db.Set<CanonWorkIdentifier>().Alive()
                .Where(x => x.WorkId == workId).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
            if (next is not null)
            {
                next.IsPrimary = true;
                Touch(next);
                await SaveAsync(ct);
            }
        }

        await tx.CommitAsync(ct);
    }

    /// <summary>"SN 12.2", "t99" 같은 번호로 경을 찾는다. scheme 이 달라 겹칠 수 있어 목록으로 돌려준다.</summary>
    public async Task<IReadOnlyList<WorkMatch>> FindByIdentifierAsync(string raw, string? scheme, CancellationToken ct)
    {
        var value = ScriptureText.NormalizeIdentifier(raw);
        if (value.Length == 0) return [];
        var lower = value.ToLowerInvariant();

        var q = Db.Set<CanonWorkIdentifier>().AsNoTracking().Alive().Where(x => x.Value.ToLower() == lower);
        if (scheme is not null)
        {
            var sch = NormalizeScheme(scheme);
            q = q.Where(x => x.Scheme == sch);
        }

        return await q
            .Where(x => x.Work.DeletedAt == null)
            .OrderBy(x => x.Work.SortOrder)
            .Select(x => new WorkMatch(x.WorkId, x.Work.CollectionId, x.Work.Code, x.Work.TitleKo, x.Scheme, x.Value))
            .Take(20)
            .ToListAsync(ct);
    }

    public async Task<WorkDetail> GetDetailAsync(long id, bool includeUnpublishedEditions, CancellationToken ct)
    {
        var w = await Db.Set<CanonWork>().AsNoTracking().Alive().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw Rule.NotFound("경", id);

        var identifiers = await Db.Set<CanonWorkIdentifier>().AsNoTracking().Alive()
            .Where(x => x.WorkId == id)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Id)
            .Select(x => new WorkDetail.IdentifierView(x.Id, x.Scheme, x.Value, x.IsPrimary))
            .ToListAsync(ct);

        var editions = await Db.Set<CanonEdition>().AsNoTracking().Alive()
            .Where(x => x.WorkId == id && (includeUnpublishedEditions || x.Status == EditionStatus.Published))
            .OrderBy(x => x.LanguageCode).ThenBy(x => x.Kind).ThenBy(x => x.Id)
            .Select(x => new WorkDetail.EditionView(x.Id, x.LanguageCode, x.Script, x.Kind, x.Revision, x.Status, x.SegmentCount, x.TranslatorName))
            .ToListAsync(ct);

        return new WorkDetail(w.Id, w.CollectionId, w.GroupId, w.Code, w.TitleKo, w.TitleOriginal, w.TitleRoman, w.Summary, identifiers, editions);
    }

    // ───────────────────────── 식별자 보조 ─────────────────────────

    /// <summary>체계 코드: 대문자 영숫자와 - _ (예: SC, PTS, VRI, T, CBETA, KBC), 32자 이내.</summary>
    private static string NormalizeScheme(string? raw)
    {
        var s = ScriptureText.Clean(raw).ToUpperInvariant();
        if (s.Length is 0 or > 32 || !s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            throw Rule.Invalid("식별자 체계는 대문자 영숫자와 - _ 만 쓸 수 있고 32자 이내여야 하네. (예: SC, PTS, T, CBETA)");
        return s;
    }

    private static List<IdentifierInput> PrepareIdentifiers(IReadOnlyList<IdentifierInput>? raw)
    {
        var list = new List<IdentifierInput>();
        if (raw is null) return list;

        var seen = new HashSet<(string, string)>();
        foreach (var r in raw)
        {
            var scheme = NormalizeScheme(r.Scheme);
            var value = ScriptureText.NormalizeIdentifier(r.Value);
            if (value.Length == 0) throw Rule.Invalid("식별자 값이 비어 있네.");
            if (value.Length > 100) throw Rule.Invalid("식별자 값은 100자를 넘을 수 없네.");
            if (!seen.Add((scheme, value.ToLowerInvariant()))) continue; // 입력 안의 중복은 합친다
            list.Add(r with { Scheme = scheme, Value = value });
        }

        if (list.Count(x => x.IsPrimary) > 1) throw Rule.Invalid("대표 식별자는 하나만 지정할 수 있네.");
        if (list.Count > 0 && !list.Any(x => x.IsPrimary)) list[0] = list[0] with { IsPrimary = true };
        return list;
    }

    private async Task EnsureIdentifiersFreeAsync(IReadOnlyList<IdentifierInput> items, long? excludeWorkId, CancellationToken ct)
    {
        foreach (var item in items)
        {
            var holder = await Db.Set<CanonWorkIdentifier>().AsNoTracking().Alive()
                .Where(x => x.Scheme == item.Scheme && x.Value == item.Value && x.WorkId != (excludeWorkId ?? 0))
                .Select(x => (long?)x.WorkId)
                .FirstOrDefaultAsync(ct);

            if (holder is { } workId)
                throw Rule.Conflict($"식별자 '{item.Value}'({item.Scheme})는 이미 경 #{workId} 이(가) 쓰고 있네.");
        }
    }
}
