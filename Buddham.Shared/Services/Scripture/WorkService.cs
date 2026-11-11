using Buddham.Core.Common.Queries;
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
/// 경 = 추상적 작품 단위. 실제 원문은 Edition/Segment 에 있고, 여기서는 위치·층위·귀속·식별자를 다룬다.
/// 경이 늘거나 줄면 메뉴 트리의 경 수가 달라지므로 트리 캐시를 무효화한다.
/// </summary>
public sealed class WorkService(ScriptureDbContext db, IRedisCacheService cache)
    : ScriptureServiceBase(db, cache), IWorkService
{
    // ═════════════════════ 조회 ═════════════════════

    public Task<OffsetPagedResult<Schema.Work.Response>> GetWorksAsync(
        long? collectionId, long? groupId, OffsetPagedQuery query, CancellationToken ct)
        => ScriptureResults.RunOffsetAsync(async () =>
        {
            var q = Db.Set<CanonWork>().AsNoTracking().Alive();
            if (collectionId is { } cid) q = q.Where(x => x.CollectionId == cid);
            if (groupId is { } gid) q = q.Where(x => x.GroupId == gid);

            if (ScriptureText.Blank(query.SearchKeyword) is { } keyword)
            {
                var pattern = "%" + ScriptureText.EscapeLike(keyword) + "%";
                q = q.Where(x => EF.Functions.ILike(x.TitleKo, pattern)
                                 || EF.Functions.ILike(x.Code, pattern)
                                 || (x.TitleRoman != null && EF.Functions.ILike(x.TitleRoman, pattern))
                                 || (x.TitleOriginal != null && EF.Functions.ILike(x.TitleOriginal, pattern)));
            }

            var total = await q.CountAsync(ct);

            // SortBy 가 없으면 읽는 순서(SortOrder 오름차순). OffsetPagedQuery.Descending 의 기본값(true)은 정렬 기준을 명시했을 때만 적용한다.
            var ordered = (query.SortBy?.Trim().ToLowerInvariant()) switch
            {
                "code" => query.Descending ? q.OrderByDescending(x => x.Code) : q.OrderBy(x => x.Code),
                "title" => query.Descending ? q.OrderByDescending(x => x.TitleKo) : q.OrderBy(x => x.TitleKo),
                "createdat" => query.Descending ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt),
                _ => q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id),
            };

            var items = await ordered
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .AsResponse()
                .ToListAsync(ct);

            return OffsetPagedResult<Schema.Work.Response>.Success(items, total, query.PageNumber, query.PageSize);
        });

    public Task<Result<Schema.Work.Detail>> GetDetailAsync(long id, bool includeUnpublishedEditions, CancellationToken ct)
        => ScriptureResults.RunAsync(() => ReadDetailAsync(id, includeUnpublishedEditions, ct));

    private async Task<Schema.Work.Detail> ReadDetailAsync(long id, bool includeUnpublishedEditions, CancellationToken ct)
        => await Db.Set<CanonWork>().AsNoTracking().Alive()
               .Where(x => x.Id == id)
               .AsDetail(includeUnpublishedEditions)
               .FirstOrDefaultAsync(ct)
           ?? throw Rule.NotFound("경", id);

    // ═════════════════════ 경 CRUD ═════════════════════

    public Task<Result<Schema.Work.Detail>> CreateAsync(Schema.Work.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            await ValidateAsync(null, r, ct);

            var prepared = PrepareIdentifiers(r.Identifiers);
            await EnsureIdentifiersFreeAsync(prepared, excludeWorkId: null, ct);

            var e = new CanonWork();
            Apply(e, r);
            Db.Add(e);

            foreach (var id in prepared)
                Db.Add(new CanonWorkIdentifier { Work = e, Scheme = id.Scheme, Value = id.Value, IsPrimary = id.IsPrimary });

            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadDetailAsync(e.Id, includeUnpublishedEditions: true, ct);
        }, created: true);

    public Task<Result<Schema.Work.Detail>> UpdateAsync(long id, Schema.Work.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            if (r.Identifiers is { Count: > 0 })
                throw Rule.Invalid("식별자는 수정 요청으로 바꿀 수 없네. 식별자 전용 기능(추가·대표 지정·삭제)을 쓰게.");

            var e = await FindAsync<CanonWork>(id, "경", ct);
            await ValidateAsync(id, r, ct);
            Apply(e, r);
            Touch(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
            return await ReadDetailAsync(id, includeUnpublishedEditions: true, ct);
        });

    public Task<Result> DeleteAsync(long id, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            var e = await FindAsync<CanonWork>(id, "경", ct);

            if (await AnyAsync<CanonEdition>(x => x.WorkId == id, ct)) throw Rule.Conflict("판본이 있는 경은 지울 수 없네. 판본을 먼저 정리하게.");
            if (await AnyAsync<CanonWork>(x => x.CommentaryOfWorkId == id, ct)) throw Rule.Conflict("이 경을 주석 대상으로 삼는 경이 있어 지울 수 없네.");
            if (await AnyAsync<CanonParallel>(x => x.FromWorkId == id || x.ToWorkId == id, ct)) throw Rule.Conflict("병행경 관계가 걸려 있어 지울 수 없네.");

            // 식별자는 경과 함께 지워야 (scheme, value) 유일 인덱스가 풀린다.
            var identifiers = await Db.Set<CanonWorkIdentifier>().Alive().Where(x => x.WorkId == id).ToListAsync(ct);
            foreach (var identifier in identifiers)
            {
                identifier.IsPrimary = false;
                SoftDelete(identifier);
            }

            SoftDelete(e);
            await SaveAsync(ct);
            await InvalidateAsync(ScriptureCacheKeys.Tree);
        });

    private async Task ValidateAsync(long? selfId, Schema.Work.Request r, CancellationToken ct)
    {
        await EnsureExistsAsync<CanonCollection>(r.CollectionId, "집성", ct);

        if (r.GroupId is { } gid)
        {
            var group = await FindAsync<CanonGroup>(gid, "그룹", ct);
            if (group.CollectionId != r.CollectionId) throw Rule.Invalid("그룹이 다른 집성에 속해 있네.");
        }

        if (r.SpeakerId is { } sid) await EnsureExistsAsync<CanonSpeaker>(sid, "화자", ct);

        // 층위 규칙: Root 는 주석 대상이 없고, Commentary / SubCommentary 는 반드시 대상이 있다. Other 는 선택.
        if (r.Layer == TextLayer.Root && r.CommentaryOfWorkId is not null)
            throw Rule.Invalid("원전(Root) 층의 경에는 주석 대상을 둘 수 없네.");
        if (r.Layer is TextLayer.Commentary or TextLayer.SubCommentary && r.CommentaryOfWorkId is null)
            throw Rule.Invalid("주석·복주 층의 경에는 주석 대상이 필요하네.");

        if (r.CommentaryOfWorkId is { } target)
        {
            await EnsureExistsAsync<CanonWork>(target, "주석 대상 경", ct);
            if (selfId is { } me)
            {
                if (target == me) throw Rule.Invalid("자기 자신을 주석할 수는 없네.");
                await EnsureNoCycleAsync<CanonWork>(me, target, "주석", "CommentaryOfWorkId", ct);
            }
        }

        var code = NormalizeCode(r.Code);
        if (await AnyAsync<CanonWork>(x => x.CollectionId == r.CollectionId && x.Code == code && x.Id != (selfId ?? 0), ct))
            throw Rule.Conflict($"이 집성에 이미 '{code}' 코드의 경이 있네.");
    }

    private static void Apply(CanonWork e, Schema.Work.Request r)
    {
        e.CollectionId = r.CollectionId;
        e.GroupId = r.GroupId;
        e.Code = NormalizeCode(r.Code);
        e.TitleKo = RequireText(r.TitleKo, "한글 제목", 500);
        e.TitleOriginal = OptionalText(r.TitleOriginal, "원어 제목", 1000);
        e.TitleRoman = OptionalText(r.TitleRoman, "로마자 제목", 500);
        e.Kind = r.Kind;
        e.Layer = r.Layer;
        e.Attribution = r.Attribution;
        e.AttributionNote = ScriptureText.Blank(r.AttributionNote);
        e.SpeakerId = r.SpeakerId;
        e.CommentaryOfWorkId = r.Layer == TextLayer.Root ? null : r.CommentaryOfWorkId;
        e.Summary = ScriptureText.Blank(r.Summary);
        e.SortOrder = r.SortOrder;
    }

    // ═════════════════════ 식별자 ═════════════════════
    // 같은 경이 SN 12.2 / 잡아함 288 / T99 처럼 여러 번호를 갖는다. (scheme, value) 는 전체에서 유일, 경당 대표(primary)는 하나.

    public Task<Result<Schema.Identifier.Response>> AddIdentifierAsync(long workId, Schema.Identifier.Request r, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
        {
            await EnsureExistsAsync<CanonWork>(workId, "경", ct);

            var prepared = PrepareIdentifiers([new Schema.Identifier.Request { Scheme = r.Scheme, Value = r.Value, IsPrimary = false }]);
            await EnsureIdentifiersFreeAsync(prepared, excludeWorkId: workId, ct);
            var item = prepared[0];

            var hasPrimary = await AnyAsync<CanonWorkIdentifier>(x => x.WorkId == workId && x.IsPrimary, ct);
            var wantsPrimary = r.IsPrimary || !hasPrimary; // 첫 식별자는 자동으로 대표

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

            return await Db.Set<CanonWorkIdentifier>().AsNoTracking().Where(x => x.Id == e.Id).AsResponse().FirstAsync(ct);
        }, created: true);

    public Task<Result> SetPrimaryIdentifierAsync(long workId, long identifierId, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
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
        });

    public Task<Result> RemoveIdentifierAsync(long workId, long identifierId, CancellationToken ct)
        => ScriptureResults.RunAsync(async () =>
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
        });

    public Task<Result<IReadOnlyList<Schema.Identifier.Match>>> FindByIdentifierAsync(string value, string? scheme, CancellationToken ct)
        => ScriptureResults.RunAsync<IReadOnlyList<Schema.Identifier.Match>>(async () =>
        {
            var normalized = ScriptureText.NormalizeIdentifier(value);
            if (normalized.Length == 0) return [];
            var lower = normalized.ToLowerInvariant();

            var q = Db.Set<CanonWorkIdentifier>().AsNoTracking().Alive().Where(x => x.Value.ToLower() == lower);
            if (scheme is not null)
            {
                var sch = NormalizeScheme(scheme);
                q = q.Where(x => x.Scheme == sch);
            }

            return await q
                .Where(x => x.Work.DeletedAt == null)
                .OrderBy(x => x.Work.SortOrder)
                .Select(x => new Schema.Identifier.Match
                {
                    WorkId = x.WorkId, CollectionId = x.Work.CollectionId, Code = x.Work.Code,
                    TitleKo = x.Work.TitleKo, Scheme = x.Scheme, Value = x.Value,
                })
                .Take(20)
                .ToListAsync(ct);
        });

    // ═════════════════════ 식별자 보조 ═════════════════════

    /// <summary>체계 코드: 대문자 영숫자와 - _ (예: SC, PTS, VRI, T, CBETA, KBC), 32자 이내.</summary>
    private static string NormalizeScheme(string? raw)
    {
        var s = ScriptureText.Clean(raw).ToUpperInvariant();
        if (s.Length is 0 or > 32 || !s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            throw Rule.Invalid("식별자 체계는 대문자 영숫자와 - _ 만 쓸 수 있고 32자 이내여야 하네. (예: SC, PTS, T, CBETA)");
        return s;
    }

    private static List<Schema.Identifier.Request> PrepareIdentifiers(IEnumerable<Schema.Identifier.Request>? raw)
    {
        var list = new List<Schema.Identifier.Request>();
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

    private async Task EnsureIdentifiersFreeAsync(IReadOnlyList<Schema.Identifier.Request> items, long? excludeWorkId, CancellationToken ct)
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
