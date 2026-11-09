using Buddham.Core.Entities.Scriptures;
using Buddham.Core.EnumTypes;
using Buddham.Core.Scriptures;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Buddham.Core.Interfaces.Ollama;
using Microsoft.Extensions.Configuration;

namespace Buddham.Shared.Services.Scripture;

public sealed record EditionInput(
    long WorkId, string LanguageCode, string? Script, EditionKind Kind,
    string? Revision, string? TranslatorName, long? SourceId, long? BasedOnEditionId);

public sealed record SegmentInput(string Ref, string Text, SegmentKind Kind, string? PageRef = null);

public sealed record ImportResult(int Added, int Updated, int Unchanged, int Removed, int EmbeddingsInvalidated);

public sealed record StatusChangeResult(EditionStatus Status, int SegmentCount, string? ContentHash, IReadOnlyList<string> Warnings);

/// <summary>
/// 판본(Edition = 같은 경의 언어·판본별 원문) 생명주기와 세그먼트 적재.
/// 원문은 한 곳에만 있어야 하므로, 공개된 판본은 잠그고 수정은 "검수로 되돌리기 → 가져오기 → 재공개" 로만 한다.
/// </summary>
public sealed class EditionService(ScriptureDbContext db) : ScriptureServiceBase(db)
{
    private static readonly Dictionary<EditionStatus, EditionStatus[]> Transitions = new()
    {
        [EditionStatus.Draft] = [EditionStatus.InReview],
        [EditionStatus.InReview] = [EditionStatus.Draft, EditionStatus.Published],
        [EditionStatus.Published] = [EditionStatus.InReview], // 정정이 필요하면 검토 상태로 되돌린다
    };

    // ───────────────────────── 판본 생성/수정 ─────────────────────────

    public async Task<CanonEdition> CreateAsync(EditionInput i, CancellationToken ct)
    {
        await EnsureExistsAsync<CanonWork>(i.WorkId, "경", ct);
        var (language, script) = await ValidateAsync(null, i, ct);

        var e = new CanonEdition { WorkId = i.WorkId, Status = EditionStatus.Draft, SegmentCount = 0 };
        Apply(e, i, language, script);
        Db.Add(e);
        await SaveAsync(ct);
        return e;
    }

    public async Task<CanonEdition> UpdateAsync(long id, EditionInput i, CancellationToken ct)
    {
        var e = await FindAsync<CanonEdition>(id, "판본", ct);
        if (e.Status == EditionStatus.Published)
            throw Rule.Conflict("공개된 판본의 메타데이터는 먼저 검토(InReview) 상태로 되돌린 뒤 고치게.");
        if (i.WorkId != e.WorkId) throw Rule.Invalid("판본이 속한 경은 바꿀 수 없네.");

        var (language, script) = await ValidateAsync(id, i, ct);
        Apply(e, i, language, script);
        Touch(e);
        await SaveAsync(ct);
        return e;
    }

    private async Task<(string Language, string? Script)> ValidateAsync(long? selfId, EditionInput i, CancellationToken ct)
    {
        if (!ScriptureText.TryNormalizeLanguage(i.LanguageCode, out var language))
            throw Rule.Invalid("언어 코드가 올바르지 않네. (예: pi, lzh, ko, bo, sa, en)");
        if (!ScriptureText.TryNormalizeScript(i.Script, out var script))
            throw Rule.Invalid("문자 체계는 4글자 ISO 15924 코드여야 하네. (예: Latn, Hani, Hang, Deva, Tibt)");

        if (i.SourceId is { } sid) await EnsureExistsAsync<CanonSource>(sid, "출처", ct);

        // 번역본은 어느 판본을 저본으로 삼았는지 반드시 알아야 정렬 읽기가 가능하다.
        if (i.Kind == EditionKind.Translation && i.BasedOnEditionId is null)
            throw Rule.Invalid("번역본은 저본 판본(BasedOn)을 지정해야 하네.");

        if (i.BasedOnEditionId is { } bid)
        {
            if (bid == selfId) throw Rule.Invalid("자기 자신을 저본으로 삼을 수는 없네.");
            var basis = await FindAsync<CanonEdition>(bid, "저본 판본", ct);
            if (basis.WorkId != i.WorkId) throw Rule.Invalid("저본 판본은 같은 경의 판본이어야 하네.");
        }

        var revision = ScriptureText.Blank(i.Revision);
        var duplicated = await AnyAsync<CanonEdition>(x =>
            x.WorkId == i.WorkId && x.LanguageCode == language && x.Kind == i.Kind
            && x.Revision == revision && x.SourceId == i.SourceId && x.Id != (selfId ?? 0), ct);
        if (duplicated) throw Rule.Conflict("같은 경·언어·종류·개정·출처의 판본이 이미 있네.");

        return (language, script);
    }

    private static void Apply(CanonEdition e, EditionInput i, string language, string? script)
    {
        e.LanguageCode = language;
        e.Script = script;
        e.Kind = i.Kind;
        e.Revision = OptionalText(i.Revision, "개정", 100);
        e.TranslatorName = OptionalText(i.TranslatorName, "번역자", 300);
        e.SourceId = i.SourceId;
        e.BasedOnEditionId = i.BasedOnEditionId;
    }

    // ───────────────────────── 상태 전이 ─────────────────────────

    public async Task<StatusChangeResult> ChangeStatusAsync(long id, EditionStatus target, CancellationToken ct)
    {
        var e = await FindAsync<CanonEdition>(id, "판본", ct);
        if (e.Status == target) throw Rule.Conflict("이미 그 상태이네.");
        if (!Transitions[e.Status].Contains(target))
            throw Rule.Invalid($"{e.Status} → {target} 전이는 허용되지 않네.");

        var warnings = new List<string>();

        if (target == EditionStatus.Published)
            await PrepareForPublishAsync(e, warnings, ct);

        e.Status = target;
        Touch(e);
        await SaveAsync(ct);
        return new StatusChangeResult(e.Status, e.SegmentCount, e.ContentHash, warnings);
    }

    /// <summary>공개 직전 점검: 세그먼트 수·해시 갱신, 번역본이면 저본과의 정렬(SegmentRef) 점검.</summary>
    private async Task PrepareForPublishAsync(CanonEdition e, List<string> warnings, CancellationToken ct)
    {
        var rows = await Db.Set<CanonSegment>().AsNoTracking().Alive()
            .Where(s => s.EditionId == e.Id)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Select(s => new { s.SegmentRef, s.TextHash, s.Text })
            .ToListAsync(ct);

        if (rows.Count == 0) throw Rule.Invalid("세그먼트가 없는 판본은 공개할 수 없네.");

        e.SegmentCount = rows.Count;
        e.ContentHash = ScriptureText.ContentHash(rows.Select(r => (r.SegmentRef, r.TextHash ?? ScriptureText.TextHash(r.Text))));

        if (e.BasedOnEditionId is { } baseId)
        {
            var baseRefs = await Db.Set<CanonSegment>().AsNoTracking().Alive()
                .Where(s => s.EditionId == baseId).Select(s => s.SegmentRef).ToListAsync(ct);

            var mine = rows.Select(r => r.SegmentRef).ToHashSet(StringComparer.Ordinal);
            var theirs = baseRefs.ToHashSet(StringComparer.Ordinal);

            var missing = theirs.Count(r => !mine.Contains(r));
            var extra = mine.Count(r => !theirs.Contains(r));
            if (missing > 0) warnings.Add($"저본에는 있으나 이 번역본에 없는 세그먼트가 {missing}개 있네.");
            if (extra > 0) warnings.Add($"저본에 없는 세그먼트 주소가 {extra}개 있네. 정렬 읽기에서 짝을 찾지 못하네.");

            var baseStatus = await Db.Set<CanonEdition>().AsNoTracking().Where(x => x.Id == baseId).Select(x => x.Status).FirstAsync(ct);
            if (baseStatus != EditionStatus.Published) warnings.Add("저본 판본이 아직 공개 상태가 아니네.");
        }
    }

    // ───────────────────────── 세그먼트 가져오기 ─────────────────────────

    /// <summary>
    /// 입력 목록(순서 = 읽는 순서)을 판본에 맞춘다.
    /// ref 가 같으면 갱신(같은 Id 유지 → 사경·주제·병행 참조가 보존됨), 없으면 추가.
    /// removeMissing 이면 입력에 없는 세그먼트는 지우되, 다른 곳에서 참조 중이면 전체를 거부한다.
    /// 본문이 바뀐 세그먼트는 임베딩을 무효화한다.
    /// </summary>
    public async Task<ImportResult> ImportSegmentsAsync(long editionId, IReadOnlyList<SegmentInput> inputs, bool removeMissing, CancellationToken ct)
    {
        var edition = await FindAsync<CanonEdition>(editionId, "판본", ct);
        if (edition.Status == EditionStatus.Published)
            throw Rule.Conflict("공개된 판본은 잠겨 있네. 먼저 검토(InReview) 상태로 되돌리게.");
        if (inputs.Count == 0 && !removeMissing) throw Rule.Invalid("가져올 세그먼트가 없네.");

        var prepared = Prepare(inputs);

        var existing = await Db.Set<CanonSegment>().Alive()
            .Where(s => s.EditionId == editionId)
            .ToDictionaryAsync(s => s.SegmentRef, StringComparer.Ordinal, ct);

        int added = 0, updated = 0, unchanged = 0, invalidated = 0;

        for (var idx = 0; idx < prepared.Count; idx++)
        {
            var p = prepared[idx];
            var order = idx + 1;

            if (existing.Remove(p.Ref, out var seg))
            {
                var textChanged = seg.TextHash != p.Hash;
                if (!textChanged && seg.SortOrder == order && seg.Kind == p.Kind && seg.PageRef == p.PageRef)
                {
                    unchanged++;
                    continue;
                }

                if (textChanged)
                {
                    seg.Text = p.Text;
                    seg.TextNormalized = p.Normalized;
                    seg.TextHash = p.Hash;
                    seg.Embedding = null;
                    seg.EmbeddingModel = null;
                    seg.EmbeddedAt = null;
                    invalidated++;
                }

                seg.SortOrder = order;
                seg.Kind = p.Kind;
                seg.PageRef = p.PageRef;
                Touch(seg);
                updated++;
            }
            else
            {
                Db.Add(new CanonSegment
                {
                    EditionId = editionId,
                    SegmentRef = p.Ref,
                    SortOrder = order,
                    Kind = p.Kind,
                    PageRef = p.PageRef,
                    Text = p.Text,
                    TextNormalized = p.Normalized,
                    TextHash = p.Hash,
                });
                added++;
            }
        }

        // existing 에 남은 것 = 입력에 없는 세그먼트
        var removed = 0;
        if (removeMissing && existing.Count > 0)
        {
            await EnsureNotReferencedAsync(existing.Values.ToList(), ct);
            foreach (var seg in existing.Values) SoftDelete(seg);
            removed = existing.Count;
        }

        edition.SegmentCount = prepared.Count + (removeMissing ? 0 : existing.Count);
        if (added + updated + removed > 0)
        {
            edition.ContentHash = null; // 공개 시점에 다시 계산
            Touch(edition);
        }

        await SaveAsync(ct); // 한 번의 SaveChanges = 한 트랜잭션
        return new ImportResult(added, updated, unchanged, removed, invalidated);
    }

    private sealed record Prepared(string Ref, string Text, string Normalized, string Hash, SegmentKind Kind, string? PageRef);

    private static List<Prepared> Prepare(IReadOnlyList<SegmentInput> inputs)
    {
        var result = new List<Prepared>(inputs.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var n = 0; n < inputs.Count; n++)
        {
            var s = inputs[n];
            if (!ScriptureText.TryNormalizeRef(s.Ref, out var r))
                throw Rule.Invalid($"{n + 1}번째 세그먼트의 주소(ref)가 올바르지 않네. (1~{ScriptureText.MaxRefLength}자, 제어문자 불가)");
            if (!seen.Add(r))
                throw Rule.Invalid($"세그먼트 주소 '{r}' 가 입력 안에서 중복되네.");

            var text = ScriptureText.Clean(s.Text);
            if (text.Length == 0) throw Rule.Invalid($"'{r}' 의 본문이 비어 있네.");

            var page = OptionalText(s.PageRef, "쪽 표기", 100);
            result.Add(new Prepared(r, text, ScriptureText.Normalize(text), ScriptureText.TextHash(text), s.Kind, page));
        }

        return result;
    }

    private async Task EnsureNotReferencedAsync(IReadOnlyList<CanonSegment> segments, CancellationToken ct)
    {
        var ids = segments.Select(s => s.Id).ToList();

        var referenced = new HashSet<long>();
        referenced.UnionWith(await Db.Set<CanonSegmentTopic>().Alive().Where(t => ids.Contains(t.SegmentId)).Select(t => t.SegmentId).Distinct().ToListAsync(ct));
        referenced.UnionWith(await Db.Set<LekhanaEntry>().Alive().Where(x => ids.Contains(x.SegmentId)).Select(x => x.SegmentId).Distinct().ToListAsync(ct));
        referenced.UnionWith(await Db.Set<CanonParallel>().Alive().Where(p => p.FromSegmentId != null && ids.Contains(p.FromSegmentId.Value)).Select(p => p.FromSegmentId!.Value).ToListAsync(ct));
        referenced.UnionWith(await Db.Set<CanonParallel>().Alive().Where(p => p.ToSegmentId != null && ids.Contains(p.ToSegmentId.Value)).Select(p => p.ToSegmentId!.Value).ToListAsync(ct));

        if (referenced.Count == 0) return;

        var refs = segments.Where(s => referenced.Contains(s.Id)).Select(s => s.SegmentRef).Take(10);
        throw Rule.Conflict($"사경·주제·병행경이 참조 중인 세그먼트 {referenced.Count}개는 지울 수 없네. 예: {string.Join(", ", refs)}");
    }

    // ───────────────────────── 판본 삭제 ─────────────────────────

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var e = await FindAsync<CanonEdition>(id, "판본", ct);
        if (e.Status == EditionStatus.Published) throw Rule.Conflict("공개된 판본은 지울 수 없네. 먼저 검토(InReview) 상태로 되돌리게.");
        if (await AnyAsync<CanonEdition>(x => x.BasedOnEditionId == id, ct)) throw Rule.Conflict("이 판본을 저본으로 삼는 번역본이 있어 지울 수 없네.");
        if (await AnyAsync<LekhanaSession>(x => x.EditionId == id, ct)) throw Rule.Conflict("사경 기록이 있는 판본은 지울 수 없네.");

        var segments = await Db.Set<CanonSegment>().Alive().Where(s => s.EditionId == id).ToListAsync(ct);
        await EnsureNotReferencedAsync(segments, ct);

        foreach (var s in segments) SoftDelete(s);
        SoftDelete(e);
        await SaveAsync(ct);
    }
}

/// <summary>임베딩이 비어 있는 세그먼트를 채운다. 백그라운드 서비스나 관리자 버튼에서 반복 호출한다.</summary>
public sealed class SegmentEmbeddingService(ScriptureDbContext db, IOllamaService ollama, IConfiguration config) : ScriptureServiceBase(db)
{
    private const int Dimensions = ScriptureConstants.EmbeddingDimensions;

    /// <returns>이번 호출에서 채운 개수. 0 이면 더 할 일이 없다.</returns>
    public async Task<int> EmbedPendingAsync(int batchSize, long? editionId, CancellationToken ct)
    {
        batchSize = Math.Clamp(batchSize, 1, 256);

        var q = Db.Set<CanonSegment>().Alive()
            .Where(s => s.Embedding == null);
        if (editionId is { } eid) q = q.Where(s => s.EditionId == eid);

        var batch = await q.OrderBy(s => s.Id).Take(batchSize).ToListAsync(ct);
        if (batch.Count == 0) return 0;

        // IOllamaService 는 단건 API 라 순차 호출한다. (배치 오버로드가 생기면 한 번에 호출하도록 교체)
        var model = config["Ollama:Model"] ?? "bge-m3";
        var vectors = new List<float[]>(batch.Count);
        foreach (var segment in batch)
            vectors.Add(await ollama.GetEmbeddingAsync(segment.Text, ct));

        var now = Now;
        for (var n = 0; n < batch.Count; n++)
        {
            if (vectors[n].Length != Dimensions)
                throw new InvalidOperationException($"임베딩 차원이 {vectors[n].Length} 이네. {Dimensions} 이어야 하네.");

            batch[n].Embedding = vectors[n];
            batch[n].EmbeddingModel = model;
            batch[n].EmbeddedAt = now;
        }

        await SaveAsync(ct);
        return batch.Count;
    }
}
