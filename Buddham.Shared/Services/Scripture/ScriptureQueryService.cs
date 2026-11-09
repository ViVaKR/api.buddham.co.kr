using Buddham.Core.Entities.Scriptures;
using Buddham.Core.EnumTypes;
using Buddham.Core.Scriptures;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Buddham.Core.Interfaces.Ollama;
using Pgvector.EntityFrameworkCore;

namespace Buddham.Shared.Services.Scripture;

public enum TreeNodeType { Canon, Division, Collection, Group }

public sealed class TreeNode
{
    public required TreeNodeType Type { get; init; }
    public required long Id { get; init; }
    public required string Code { get; init; }
    public required string NameKo { get; init; }
    public string? NameOriginal { get; init; }
    public string? Kind { get; init; }
    /// <summary>이 노드 아래(자손 포함)의 경 수. 메뉴 배지용.</summary>
    public int WorkCount { get; set; }
    public List<TreeNode> Children { get; } = [];
}

public sealed record SegmentView(long Id, string Ref, int SortOrder, SegmentKind Kind, string? PageRef, string Text);
public sealed record SegmentPage(long EditionId, IReadOnlyList<SegmentView> Items, int? NextAfterSortOrder);
public sealed record RefText(long EditionId, string Ref, string Text);
public sealed record AlignedRow(string Ref, int SortOrder, IReadOnlyDictionary<long, string> TextsByEdition);

public enum SearchMode { Text, Semantic, Hybrid }

public sealed record SearchRequest(
    string Query, SearchMode Mode = SearchMode.Hybrid,
    string? LanguageCode = null, long? CollectionId = null, long? WorkId = null, int Take = 20);

public sealed record SearchHit(
    long SegmentId, string SegmentRef, string Text, long EditionId, string LanguageCode,
    long WorkId, string WorkTitleKo, double Score);

/// <summary>읽기 전용 조회. 일반 독자는 공개(Published) 판본만, 편집자는 모든 판본을 본다.</summary>
public sealed class ScriptureQueryService(
    ScriptureDbContext db,
    IOllamaService ollama,
    ILogger<ScriptureQueryService> log) : ScriptureServiceBase(db)
{
    private const int RrfK = 60;

    // ───────────────────────── 메뉴용 전체 트리 ─────────────────────────

    /// <summary>판본계통 → 부(트리) → 집성 → 그룹(트리). 평평하게 읽어 메모리에서 조립한다.</summary>
    public async Task<IReadOnlyList<TreeNode>> GetCanonTreeAsync(CancellationToken ct)
    {
        var canons = await Db.Set<Canon>().AsNoTracking().Alive().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var divisions = await Db.Set<CanonDivision>().AsNoTracking().Alive().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var collections = await Db.Set<CanonCollection>().AsNoTracking().Alive().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var groups = await Db.Set<CanonGroup>().AsNoTracking().Alive().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);

        var counts = await Db.Set<CanonWork>().AsNoTracking().Alive()
            .GroupBy(w => new { w.CollectionId, w.GroupId })
            .Select(g => new { g.Key.CollectionId, g.Key.GroupId, N = g.Count() })
            .ToListAsync(ct);
        var directByCollection = counts.Where(c => c.GroupId == null).ToDictionary(c => c.CollectionId, c => c.N);
        var directByGroup = counts.Where(c => c.GroupId != null).ToDictionary(c => c.GroupId!.Value, c => c.N);

        var canonNodes = canons.ToDictionary(c => c.Id, c => new TreeNode { Type = TreeNodeType.Canon, Id = c.Id, Code = c.Code, NameKo = c.NameKo, NameOriginal = c.NameOriginal });
        var divNodes = divisions.ToDictionary(d => d.Id, d => new TreeNode { Type = TreeNodeType.Division, Id = d.Id, Code = d.Code, NameKo = d.NameKo, NameOriginal = d.NameOriginal, Kind = d.Kind.ToString() });
        var colNodes = collections.ToDictionary(c => c.Id, c => new TreeNode { Type = TreeNodeType.Collection, Id = c.Id, Code = c.Code, NameKo = c.NameKo, NameOriginal = c.NameOriginal, Kind = c.Kind?.ToString() });
        var grpNodes = groups.ToDictionary(g => g.Id, g => new TreeNode { Type = TreeNodeType.Group, Id = g.Id, Code = g.Code, NameKo = g.NameKo, NameOriginal = g.NameOriginal, Kind = g.Kind?.ToString() });

        foreach (var g in groups)
        {
            var node = grpNodes[g.Id];
            if (g.ParentId is { } p && grpNodes.TryGetValue(p, out var parent)) parent.Children.Add(node);
            else if (colNodes.TryGetValue(g.CollectionId, out var col)) col.Children.Add(node);
        }

        foreach (var c in collections)
            if (divNodes.TryGetValue(c.DivisionId, out var div)) div.Children.Add(colNodes[c.Id]);

        foreach (var d in divisions)
        {
            var node = divNodes[d.Id];
            if (d.ParentId is { } p && divNodes.TryGetValue(p, out var parent)) parent.Children.Add(node);
            else if (canonNodes.TryGetValue(d.CanonId, out var canon)) canon.Children.Add(node);
        }

        var roots = canonNodes.Values.ToList();
        foreach (var root in roots)
            Rollup(root, n => n.Type switch
            {
                TreeNodeType.Collection => directByCollection.GetValueOrDefault(n.Id),
                TreeNodeType.Group => directByGroup.GetValueOrDefault(n.Id),
                _ => 0,
            });
        return roots;
    }

    private static int Rollup(TreeNode node, Func<TreeNode, int> direct)
    {
        var total = direct(node);
        foreach (var child in node.Children) total += Rollup(child, direct);
        node.WorkCount = total;
        return total;
    }

    // ───────────────────────── 읽기 ─────────────────────────

    private IQueryable<CanonEdition> VisibleEditions(bool asEditor)
        => Db.Set<CanonEdition>().AsNoTracking().Alive().Where(e => asEditor || e.Status == EditionStatus.Published);

    /// <summary>판본을 읽는 순서대로 커서 페이지로 돌려준다. (SortOrder 는 가져오기 때 1..N 으로 부여됨)</summary>
    public async Task<SegmentPage> GetSegmentsAsync(long editionId, int afterSortOrder, int take, bool asEditor, CancellationToken ct)
    {
        var edition = await VisibleEditions(asEditor).FirstOrDefaultAsync(e => e.Id == editionId, ct)
            ?? throw Rule.NotFound("판본", editionId);

        take = Math.Clamp(take, 1, 200);
        var rows = await Db.Set<CanonSegment>().AsNoTracking().Alive()
            .Where(s => s.EditionId == editionId && s.SortOrder > afterSortOrder)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Take(take + 1)
            .Select(s => new SegmentView(s.Id, s.SegmentRef, s.SortOrder, s.Kind, s.PageRef, s.Text))
            .ToListAsync(ct);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(take);
        return new SegmentPage(edition.Id, rows, hasMore ? rows[^1].SortOrder : null);
    }

    /// <summary>
    /// 같은 경의 여러 판본(원문·번역)을 SegmentRef 로 맞춰 나란히 돌려준다.
    /// 기준 판본의 한 페이지를 읽고, 나머지 판본에서는 같은 ref 의 세그먼트만 가져온다.
    /// </summary>
    public async Task<IReadOnlyList<AlignedRow>> GetAlignedAsync(
        long baseEditionId, IReadOnlyList<long> otherEditionIds, int afterSortOrder, int take, bool asEditor, CancellationToken ct)
    {
        var all = otherEditionIds.Append(baseEditionId).Distinct().ToList();
        var editions = await VisibleEditions(asEditor).Where(e => all.Contains(e.Id)).Select(e => new { e.Id, e.WorkId }).ToListAsync(ct);
        if (editions.Count != all.Count) throw Rule.NotFound("판본", baseEditionId);
        if (editions.Select(e => e.WorkId).Distinct().Count() != 1) throw Rule.Invalid("정렬 읽기는 같은 경의 판본끼리만 가능하네. 다른 경은 병행경으로 보게.");

        var page = await GetSegmentsAsync(baseEditionId, afterSortOrder, take, asEditor, ct);
        var refs = page.Items.Select(i => i.Ref).ToList();

        var others = otherEditionIds.Distinct().Where(id => id != baseEditionId).ToList();
        List<RefText> texts = [];
        if (others.Count > 0)
        {
            texts = await Db.Set<CanonSegment>().AsNoTracking().Alive()
                .Where(s => others.Contains(s.EditionId) && refs.Contains(s.SegmentRef))
                .Select(s => new RefText(s.EditionId, s.SegmentRef, s.Text))
                .ToListAsync(ct);
        }

        var byRef = texts.ToLookup(t => t.Ref);
        return page.Items.Select(item =>
        {
            var map = new Dictionary<long, string> { [baseEditionId] = item.Text };
            foreach (var t in byRef[item.Ref]) map[t.EditionId] = t.Text;
            return new AlignedRow(item.Ref, item.SortOrder, map);
        }).ToList();
    }

    // ───────────────────────── 검색 ─────────────────────────

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(SearchRequest r, CancellationToken ct)
    {
        var query = ScriptureText.Clean(r.Query);
        if (query.Length == 0) return [];
        var take = Math.Clamp(r.Take, 1, 100);

        return r.Mode switch
        {
            SearchMode.Text => await TextSearchAsync(r, query, take, ct),
            SearchMode.Semantic => await SemanticSearchAsync(r, query, take, ct),
            _ => await HybridSearchAsync(r, query, take, ct),
        };
    }

    /// <summary>공개 판본의 살아 있는 세그먼트만, 언어·집성·경 필터를 적용한 기본 쿼리.</summary>
    private IQueryable<CanonSegment> SearchBase(SearchRequest r)
    {
        var q = Db.Set<CanonSegment>().AsNoTracking().Alive()
            .Where(s => s.Edition.DeletedAt == null && s.Edition.Status == EditionStatus.Published);

        if (r.LanguageCode is { } lang && ScriptureText.TryNormalizeLanguage(lang, out var l))
            q = q.Where(s => s.Edition.LanguageCode == l);
        if (r.WorkId is { } w) q = q.Where(s => s.Edition.WorkId == w);
        if (r.CollectionId is { } c) q = q.Where(s => s.Edition.Work.CollectionId == c);
        return q;
    }

    /// <summary>text_normalized 의 trigram GIN 인덱스를 타는 부분 일치 + 단어 유사도 순위.</summary>
    private async Task<IReadOnlyList<SearchHit>> TextSearchAsync(SearchRequest r, string query, int take, CancellationToken ct)
    {
        var norm = ScriptureText.Normalize(query);
        if (norm.Length == 0) return [];
        var pattern = "%" + ScriptureText.EscapeLike(norm) + "%";

        var rows = await SearchBase(r)
            .Where(s => EF.Functions.ILike(s.TextNormalized!, pattern))
            .Select(s => new
            {
                s.Id, s.SegmentRef, s.Text, s.EditionId, s.Edition.LanguageCode,
                s.Edition.WorkId, Title = s.Edition.Work.TitleKo,
                Score = EF.Functions.TrigramsWordSimilarity(norm, s.TextNormalized!),
            })
            .OrderByDescending(x => x.Score).ThenBy(x => x.Id)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(x => new SearchHit(x.Id, x.SegmentRef, x.Text, x.EditionId, x.LanguageCode, x.WorkId, x.Title, x.Score)).ToList();
    }

    /// <summary>코사인 거리 기준 의미 검색(HNSW). 필터를 걸면 후보가 줄 수 있어 넉넉히 가져온 뒤 자른다.</summary>
    private async Task<IReadOnlyList<SearchHit>> SemanticSearchAsync(SearchRequest r, string query, int take, CancellationToken ct)
    {
        var qv = new Pgvector.Vector(await ollama.GetEmbeddingAsync(query, ct));

        var rows = await SearchBase(r)
            .Where(s => s.Embedding != null)
            .OrderBy(s => s.Embedding!.CosineDistance(qv))
            .Select(s => new
            {
                s.Id, s.SegmentRef, s.Text, s.EditionId, s.Edition.LanguageCode,
                s.Edition.WorkId, Title = s.Edition.Work.TitleKo,
                Distance = s.Embedding!.CosineDistance(qv),
            })
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(x => new SearchHit(x.Id, x.SegmentRef, x.Text, x.EditionId, x.LanguageCode, x.WorkId, x.Title, 1.0 - x.Distance)).ToList();
    }

    /// <summary>두 순위를 Reciprocal Rank Fusion 으로 합친다. 임베딩 서버가 죽어 있으면 전문 검색만으로 응답한다.</summary>
    private async Task<IReadOnlyList<SearchHit>> HybridSearchAsync(SearchRequest r, string query, int take, CancellationToken ct)
    {
        var pool = Math.Min(take * 3, 100);
        var text = await TextSearchAsync(r, query, pool, ct);

        IReadOnlyList<SearchHit> semantic = [];
        try
        {
            semantic = await SemanticSearchAsync(r, query, pool, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "의미 검색에 실패하여 전문 검색 결과만 사용하네.");
        }

        var fused = new Dictionary<long, (SearchHit Hit, double Score)>();
        void Add(IReadOnlyList<SearchHit> list)
        {
            for (var rank = 0; rank < list.Count; rank++)
            {
                var hit = list[rank];
                var add = 1.0 / (RrfK + rank + 1);
                fused[hit.SegmentId] = fused.TryGetValue(hit.SegmentId, out var cur) ? (cur.Hit, cur.Score + add) : (hit, add);
            }
        }

        Add(text);
        Add(semantic);

        return fused.Values
            .OrderByDescending(x => x.Score)
            .Take(take)
            .Select(x => x.Hit with { Score = x.Score })
            .ToList();
    }
}
