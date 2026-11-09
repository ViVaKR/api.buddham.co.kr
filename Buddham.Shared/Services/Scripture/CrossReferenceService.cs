using Buddham.Core.Entities.Scriptures;
using Buddham.Core.EnumTypes;
using Buddham.Core.Scriptures;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Scripture;

public sealed record ParallelInput(
    long FromWorkId, long ToWorkId, ParallelKind Kind,
    long? FromSegmentId, long? ToSegmentId, string? Note);

public sealed record ParallelView(
    long Id, bool Outbound, long OtherWorkId, string OtherWorkCode, string OtherWorkTitleKo,
    ParallelKind Kind, long? ThisSegmentId, long? OtherSegmentId, string? Note);

public sealed record TopicInput(
    long? ParentId, string Code, string NameKo, string? NamePali, string? NameSanskrit, string? Description, int SortOrder);

/// <summary>병행경: 서로 다른 경(Work) 사이의 관계. 작품 단위 또는 세그먼트 단위로 건다.</summary>
public sealed class ParallelService(ScriptureDbContext db) : ScriptureServiceBase(db)
{
    public async Task<CanonParallel> AddAsync(ParallelInput i, CancellationToken ct)
    {
        if (i.FromWorkId == i.ToWorkId) throw Rule.Invalid("같은 경끼리는 병행경으로 이을 수 없네.");
        await EnsureExistsAsync<CanonWork>(i.FromWorkId, "기준 경", ct);
        await EnsureExistsAsync<CanonWork>(i.ToWorkId, "대상 경", ct);

        // 작품 단위(둘 다 null) 또는 세그먼트 단위(둘 다 값) 중 하나여야 한다.
        if ((i.FromSegmentId is null) != (i.ToSegmentId is null))
            throw Rule.Invalid("세그먼트 단위 병행은 양쪽 세그먼트를 모두 지정해야 하네.");

        if (i.FromSegmentId is { } fs) await EnsureSegmentOfWorkAsync(fs, i.FromWorkId, ct);
        if (i.ToSegmentId is { } ts) await EnsureSegmentOfWorkAsync(ts, i.ToWorkId, ct);

        // 방향이 반대인 같은 관계도 중복으로 본다. (A→B, B→A)
        var a = i.FromWorkId; var b = i.ToWorkId; var sa = i.FromSegmentId; var sb = i.ToSegmentId; var kind = i.Kind;
        var duplicated = await AnyAsync<CanonParallel>(p => p.Kind == kind &&
            ((p.FromWorkId == a && p.ToWorkId == b && p.FromSegmentId == sa && p.ToSegmentId == sb) ||
             (p.FromWorkId == b && p.ToWorkId == a && p.FromSegmentId == sb && p.ToSegmentId == sa)), ct);
        if (duplicated) throw Rule.Conflict("이미 같은 병행 관계가 있네.");

        var e = new CanonParallel
        {
            FromWorkId = a, ToWorkId = b, Kind = kind,
            FromSegmentId = sa, ToSegmentId = sb,
            Note = ScriptureText.Blank(i.Note),
        };
        Db.Add(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task RemoveAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<CanonParallel>(id, "병행경 관계", ct);
        SoftDelete(e);
        await SaveAsync(ct);
    }

    /// <summary>이 경에 걸린 병행 관계를 방향과 상관없이 "상대 경" 기준으로 돌려준다.</summary>
    public async Task<IReadOnlyList<ParallelView>> GetForWorkAsync(long workId, CancellationToken ct)
    {
        await EnsureExistsAsync<CanonWork>(workId, "경", ct);

        var rows = await Db.Set<CanonParallel>().AsNoTracking().Alive()
            .Where(p => p.FromWorkId == workId || p.ToWorkId == workId)
            .Where(p => p.FromWork.DeletedAt == null && p.ToWork.DeletedAt == null)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id, p.Kind, p.Note, p.FromWorkId, p.ToWorkId, p.FromSegmentId, p.ToSegmentId,
                FromCode = p.FromWork.Code, FromTitle = p.FromWork.TitleKo,
                ToCode = p.ToWork.Code, ToTitle = p.ToWork.TitleKo,
            })
            .ToListAsync(ct);

        return rows.Select(p => p.FromWorkId == workId
            ? new ParallelView(p.Id, true, p.ToWorkId, p.ToCode, p.ToTitle, p.Kind, p.FromSegmentId, p.ToSegmentId, p.Note)
            : new ParallelView(p.Id, false, p.FromWorkId, p.FromCode, p.FromTitle, p.Kind, p.ToSegmentId, p.FromSegmentId, p.Note)).ToList();
    }

    /// <summary>읽는 중인 세그먼트에 걸린 세그먼트 단위 병행(양방향)을 돌려준다.</summary>
    public async Task<IReadOnlyList<ParallelView>> GetForSegmentAsync(long segmentId, CancellationToken ct)
    {
        var rows = await Db.Set<CanonParallel>().AsNoTracking().Alive()
            .Where(p => p.FromSegmentId == segmentId || p.ToSegmentId == segmentId)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id, p.Kind, p.Note, p.FromWorkId, p.ToWorkId, p.FromSegmentId, p.ToSegmentId,
                FromCode = p.FromWork.Code, FromTitle = p.FromWork.TitleKo,
                ToCode = p.ToWork.Code, ToTitle = p.ToWork.TitleKo,
            })
            .ToListAsync(ct);

        return rows.Select(p => p.FromSegmentId == segmentId
            ? new ParallelView(p.Id, true, p.ToWorkId, p.ToCode, p.ToTitle, p.Kind, p.FromSegmentId, p.ToSegmentId, p.Note)
            : new ParallelView(p.Id, false, p.FromWorkId, p.FromCode, p.FromTitle, p.Kind, p.ToSegmentId, p.FromSegmentId, p.Note)).ToList();
    }

    private async Task EnsureSegmentOfWorkAsync(long segmentId, long workId, CancellationToken ct)
    {
        var owner = await Db.Set<CanonSegment>().AsNoTracking().Alive()
            .Where(s => s.Id == segmentId)
            .Select(s => (long?)s.Edition.WorkId)
            .FirstOrDefaultAsync(ct);

        if (owner is null) throw Rule.NotFound("세그먼트", segmentId);
        if (owner != workId) throw Rule.Invalid($"세그먼트 #{segmentId} 는 경 #{workId} 의 것이 아니네.");
    }
}

/// <summary>교리 주제(트리)와 세그먼트 태깅. 사람이 단 태그가 AI 태그보다 우선한다.</summary>
public sealed class TopicService(ScriptureDbContext db) : ScriptureServiceBase(db)
{
    // ───────────────────────── 주제 트리 ─────────────────────────

    public async Task<DhammaTopic> CreateAsync(TopicInput i, CancellationToken ct)
    {
        if (i.ParentId is { } pid) await EnsureExistsAsync<DhammaTopic>(pid, "상위 주제", ct);

        var e = new DhammaTopic { ParentId = i.ParentId };
        Apply(e, i);
        Db.Add(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task<DhammaTopic> UpdateAsync(long id, TopicInput i, CancellationToken ct)
    {
        var e = await FindAsync<DhammaTopic>(id, "주제", ct);

        if (i.ParentId != e.ParentId)
        {
            if (i.ParentId is { } pid)
            {
                await EnsureExistsAsync<DhammaTopic>(pid, "상위 주제", ct);
                await EnsureNoCycleAsync<DhammaTopic>(id, pid, "주제", "ParentId", ct);
            }
            e.ParentId = i.ParentId;
        }

        Apply(e, i);
        Touch(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<DhammaTopic>(id, "주제", ct);
        if (await AnyAsync<DhammaTopic>(x => x.ParentId == id, ct)) throw Rule.Conflict("하위 주제가 있어 지울 수 없네.");
        if (await AnyAsync<CanonSegmentTopic>(x => x.TopicId == id, ct)) throw Rule.Conflict("이 주제가 달린 세그먼트가 있어 지울 수 없네.");
        SoftDelete(e);
        await SaveAsync(ct);
    }

    private static void Apply(DhammaTopic e, TopicInput i)
    {
        e.Code = NormalizeCode(i.Code);
        e.NameKo = RequireText(i.NameKo, "한글 이름", 300);
        e.NamePali = OptionalText(i.NamePali, "빠알리 이름", 300);
        e.NameSanskrit = OptionalText(i.NameSanskrit, "산스크리트 이름", 300);
        e.Description = ScriptureText.Blank(i.Description);
        e.SortOrder = i.SortOrder;
    }

    // ───────────────────────── 태깅 ─────────────────────────

    /// <summary>
    /// 세그먼트에 주제를 단다. 이미 달려 있으면 병합한다.
    /// - 사람(Manual)이 AI 태그 위에 달면 Manual 로 승격하고 신뢰도를 비운다.
    /// - AI 가 이미 사람이 단 태그에 달려고 하면 아무것도 바꾸지 않는다.
    /// - AI 끼리면 더 높은 신뢰도를 남긴다.
    /// </summary>
    public async Task<CanonSegmentTopic> TagAsync(
        long segmentId, long topicId, TagSource source, double? confidence, string? note, CancellationToken ct)
    {
        await EnsureExistsAsync<CanonSegment>(segmentId, "세그먼트", ct);
        await EnsureExistsAsync<DhammaTopic>(topicId, "주제", ct);

        if (source == TagSource.Manual) confidence = null;
        else if (confidence is { } c && (double.IsNaN(c) || c is < 0 or > 1)) throw Rule.Invalid("신뢰도는 0과 1 사이여야 하네.");

        var existing = await Db.Set<CanonSegmentTopic>().Alive()
            .FirstOrDefaultAsync(x => x.SegmentId == segmentId && x.TopicId == topicId, ct);

        if (existing is null)
        {
            existing = new CanonSegmentTopic
            {
                SegmentId = segmentId, TopicId = topicId, Source = source,
                Confidence = confidence, Note = ScriptureText.Blank(note),
            };
            Db.Add(existing);
        }
        else if (source == TagSource.Manual)
        {
            existing.Source = TagSource.Manual;
            existing.Confidence = null;
            existing.Note = ScriptureText.Blank(note) ?? existing.Note;
            Touch(existing);
        }
        else if (existing.Source != TagSource.Manual)
        {
            existing.Confidence = Math.Max(existing.Confidence ?? 0, confidence ?? 0);
            Touch(existing);
        }

        await SaveAsync(ct);
        return existing;
    }

    public async Task UntagAsync(long segmentId, long topicId, CancellationToken ct)
    {
        var existing = await Db.Set<CanonSegmentTopic>().Alive()
            .FirstOrDefaultAsync(x => x.SegmentId == segmentId && x.TopicId == topicId, ct)
            ?? throw Rule.NotFound("주제 태그", segmentId);
        SoftDelete(existing);
        await SaveAsync(ct);
    }

    /// <summary>주제(선택: 하위 주제 포함)가 달린 공개 세그먼트를 Id 커서로 돌려준다. "법(Dhamma)" 메뉴의 기반.</summary>
    public async Task<IReadOnlyList<SegmentView>> GetSegmentsByTopicAsync(
        long topicId, bool includeDescendants, long afterSegmentId, int take, CancellationToken ct)
    {
        await EnsureExistsAsync<DhammaTopic>(topicId, "주제", ct);
        var topicIds = includeDescendants ? await DescendantIdsAsync(topicId, ct) : [topicId];
        take = Math.Clamp(take, 1, 100);

        return await Db.Set<CanonSegment>().AsNoTracking().Alive()
            .Where(s => s.Id > afterSegmentId
                        && s.Edition.DeletedAt == null && s.Edition.Status == EditionStatus.Published
                        && s.Topics.Any(t => t.DeletedAt == null && topicIds.Contains(t.TopicId)))
            .OrderBy(s => s.Id)
            .Take(take)
            .Select(s => new SegmentView(s.Id, s.SegmentRef, s.SortOrder, s.Kind, s.PageRef, s.Text))
            .ToListAsync(ct);
    }

    /// <summary>주제 트리는 작아서 한 번에 읽어 메모리에서 후손을 펼친다.</summary>
    private async Task<List<long>> DescendantIdsAsync(long rootId, CancellationToken ct)
    {
        var edges = await Db.Set<DhammaTopic>().AsNoTracking().Alive()
            .Where(t => t.ParentId != null)
            .Select(t => new { t.Id, ParentId = t.ParentId!.Value })
            .ToListAsync(ct);
        var children = edges.ToLookup(e => e.ParentId, e => e.Id);

        var result = new List<long> { rootId };
        for (var n = 0; n < result.Count; n++) result.AddRange(children[result[n]]);
        return result;
    }
}
