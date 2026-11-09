using Buddham.Core.Entities.Scriptures;
using Buddham.Core.EnumTypes;
using Buddham.Core.Scriptures;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Scripture;

public sealed record StartSessionInput(long EditionId, string? Title, string? Vow);

public sealed record SessionProgress(
    long SessionId, LekhanaStatus Status, int Total, int Copied, double Percent, long? NextSegmentId);

public sealed record EntryResult(long EntryId, double? Accuracy, SessionProgress Progress);

public sealed record EntryView(
    long EntryId, long SegmentId, string SegmentRef, int SortOrder, string OriginalText,
    string? TypedText, string? ImageUrl, DateTime? CopiedAt, double? Accuracy);

/// <summary>
/// 사경 = 원문을 옮겨 쓰는 수행. 원문은 복사하지 않고 Segment 를 참조만 한다.
/// 사용자(UserId)는 다른 스키마의 값이라 FK 가 없으므로, 모든 메서드가 소유자를 서비스 계층에서 확인한다.
/// </summary>
public sealed class LekhanaService(ScriptureDbContext db) : ScriptureServiceBase(db)
{
    // ───────────────────────── 세션 ─────────────────────────

    /// <summary>같은 판본의 진행 중·멈춤 세션이 있으면 새로 만들지 않고 이어서 쓰게 돌려준다(멈춤이면 재개).</summary>
    public async Task<(LekhanaSession Session, bool Resumed)> StartAsync(Guid userId, StartSessionInput i, CancellationToken ct)
    {
        var edition = await FindAsync<CanonEdition>(i.EditionId, "판본", ct);
        if (edition.Status != EditionStatus.Published) throw Rule.Invalid("공개된 판본만 사경할 수 있네.");
        if (edition.SegmentCount == 0) throw Rule.Invalid("세그먼트가 없는 판본이네.");

        var active = await Db.Set<LekhanaSession>().Alive()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.EditionId == i.EditionId
                                      && (s.Status == LekhanaStatus.InProgress || s.Status == LekhanaStatus.Paused), ct);
        if (active is not null)
        {
            if (active.Status == LekhanaStatus.Paused)
            {
                active.Status = LekhanaStatus.InProgress;
                Touch(active);
                await SaveAsync(ct);
            }
            return (active, true);
        }

        var session = new LekhanaSession
        {
            UserId = userId,
            EditionId = i.EditionId,
            Title = OptionalText(i.Title, "제목", 300),
            Vow = ScriptureText.Blank(i.Vow),
            Status = LekhanaStatus.InProgress,
            StartedAt = Now,
        };
        Db.Add(session);
        await SaveAsync(ct);
        return (session, false);
    }

    public async Task<IReadOnlyList<LekhanaSession>> ListMineAsync(Guid userId, LekhanaStatus? status, CancellationToken ct)
    {
        var q = Db.Set<LekhanaSession>().AsNoTracking().Alive().Where(s => s.UserId == userId);
        if (status is { } st) q = q.Where(s => s.Status == st);
        return await q.OrderByDescending(s => s.StartedAt).Take(100).ToListAsync(ct);
    }

    public async Task<SessionProgress> GetProgressAsync(Guid userId, long sessionId, CancellationToken ct)
    {
        var session = await OwnedSessionAsync(userId, sessionId, requireActive: false, ct);
        return await ProgressAsync(session, ct);
    }

    public async Task CompleteAsync(Guid userId, long sessionId, CancellationToken ct)
    {
        var session = await OwnedSessionAsync(userId, sessionId, requireActive: true, ct, allowPaused: true);
        if (!await AnyAsync<LekhanaEntry>(e => e.SessionId == sessionId && e.CopiedAt != null, ct))
            throw Rule.Invalid("옮겨 쓴 기록이 하나도 없어 회향할 수 없네.");

        session.Status = LekhanaStatus.Completed;
        session.CompletedAt = Now;
        Touch(session);
        await SaveAsync(ct);
    }

    /// <summary>잠시 멈춘다. 기록은 그대로 남고 언제든 재개할 수 있다.</summary>
    public async Task PauseAsync(Guid userId, long sessionId, CancellationToken ct)
    {
        var session = await OwnedSessionAsync(userId, sessionId, requireActive: true, ct);
        session.Status = LekhanaStatus.Paused;
        Touch(session);
        await SaveAsync(ct);
    }

    public async Task ResumeAsync(Guid userId, long sessionId, CancellationToken ct)
    {
        var session = await OwnedSessionAsync(userId, sessionId, requireActive: false, ct);
        if (session.Status == LekhanaStatus.Completed) throw Rule.Conflict("이미 회향한 사경이네.");
        if (session.Status == LekhanaStatus.InProgress) return;

        session.Status = LekhanaStatus.InProgress;
        Touch(session);
        await SaveAsync(ct);
    }

    // ───────────────────────── 옮겨 쓰기 ─────────────────────────

    /// <summary>마지막으로 옮겨 쓴 세그먼트 다음 세그먼트. 끝까지 썼다면 null.</summary>
    public async Task<SegmentView?> NextSegmentAsync(Guid userId, long sessionId, CancellationToken ct)
    {
        var session = await OwnedSessionAsync(userId, sessionId, requireActive: true, ct);
        return await FindNextAsync(session, ct);
    }

    /// <summary>
    /// 한 세그먼트를 옮겨 쓴 결과를 기록한다. 같은 세그먼트를 다시 제출하면 덮어쓴다.
    /// 정확도는 저장하지 않고 원문과 비교해 즉석 계산한다. (원문이 정정되어도 기록은 사용자가 쓴 그대로 남는다)
    /// </summary>
    public async Task<EntryResult> SubmitAsync(
        Guid userId, long sessionId, long segmentId, string? typedText, string? imageUrl, CancellationToken ct)
    {
        var session = await OwnedSessionAsync(userId, sessionId, requireActive: true, ct);

        var typed = ScriptureText.Blank(typedText);
        var image = OptionalText(imageUrl, "이미지 주소", 1000);
        if (typed is null && image is null) throw Rule.Invalid("옮겨 쓴 글이나 사경 이미지 중 하나는 있어야 하네.");

        var segment = await Db.Set<CanonSegment>().AsNoTracking().Alive()
            .FirstOrDefaultAsync(s => s.Id == segmentId && s.EditionId == session.EditionId, ct)
            ?? throw Rule.Invalid("이 세션의 판본에 속한 세그먼트가 아니네.");

        var entry = await Db.Set<LekhanaEntry>().Alive()
            .FirstOrDefaultAsync(e => e.SessionId == sessionId && e.SegmentId == segmentId, ct);

        if (entry is null)
        {
            entry = new LekhanaEntry { SessionId = sessionId, SegmentId = segmentId, SortOrder = segment.SortOrder };
            Db.Add(entry);
        }
        else
        {
            Touch(entry);
        }

        entry.TypedText = typed;
        entry.ImageUrl = image;
        entry.CopiedAt = Now;

        var accuracy = typed is null ? (double?)null : ScriptureText.CopyAccuracy(segment.Text, typed);

        await SaveAsync(ct);

        // 마지막 세그먼트까지 모두 옮겨 썼으면 자동 회향
        var progress = await ProgressAsync(session, ct);
        if (progress.Total > 0 && progress.Copied >= progress.Total)
        {
            session.Status = LekhanaStatus.Completed;
            session.CompletedAt = Now;
            Touch(session);
            await SaveAsync(ct);
            progress = progress with { Status = LekhanaStatus.Completed, Percent = 100, NextSegmentId = null };
        }

        return new EntryResult(entry.Id, accuracy, progress);
    }

    /// <summary>세션의 기록을 원문과 나란히 돌려준다(복습·검수 화면).</summary>
    public async Task<IReadOnlyList<EntryView>> GetEntriesAsync(Guid userId, long sessionId, CancellationToken ct)
    {
        await OwnedSessionAsync(userId, sessionId, requireActive: false, ct);

        var rows = await Db.Set<LekhanaEntry>().AsNoTracking().Alive()
            .Where(e => e.SessionId == sessionId)
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Id)
            .Select(e => new
            {
                e.Id, e.SegmentId, Ref = e.Segment.SegmentRef, e.SortOrder, Original = e.Segment.Text,
                e.TypedText, e.ImageUrl, e.CopiedAt,
            })
            .ToListAsync(ct);

        return rows.Select(r => new EntryView(
            r.Id, r.SegmentId, r.Ref, r.SortOrder, r.Original, r.TypedText, r.ImageUrl, r.CopiedAt,
            r.TypedText is null ? null : ScriptureText.CopyAccuracy(r.Original, r.TypedText))).ToList();
    }

    // ───────────────────────── 내부 ─────────────────────────

    /// <summary>남의 세션은 존재 여부도 알려주지 않도록 NotFound 로 응답한다.</summary>
    private async Task<LekhanaSession> OwnedSessionAsync(
        Guid userId, long sessionId, bool requireActive, CancellationToken ct, bool allowPaused = false)
    {
        var session = await Db.Set<LekhanaSession>().Alive()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
            ?? throw Rule.NotFound("사경 세션", sessionId);

        if (!requireActive) return session;

        if (session.Status == LekhanaStatus.Completed) throw Rule.Conflict("이미 회향한 사경이네.");
        if (session.Status == LekhanaStatus.Paused && !allowPaused) throw Rule.Conflict("멈춘 사경이네. 먼저 이어 쓰기를 하게.");
        return session;
    }

    private async Task<SegmentView?> FindNextAsync(LekhanaSession session, CancellationToken ct)
    {
        var lastOrder = await Db.Set<LekhanaEntry>().AsNoTracking().Alive()
            .Where(e => e.SessionId == session.Id && e.CopiedAt != null)
            .MaxAsync(e => (int?)e.SortOrder, ct) ?? 0;

        return await Db.Set<CanonSegment>().AsNoTracking().Alive()
            .Where(s => s.EditionId == session.EditionId && s.SortOrder > lastOrder)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Select(s => new SegmentView(s.Id, s.SegmentRef, s.SortOrder, s.Kind, s.PageRef, s.Text))
            .FirstOrDefaultAsync(ct);
    }

    private async Task<SessionProgress> ProgressAsync(LekhanaSession session, CancellationToken ct)
    {
        var total = await Db.Set<CanonEdition>().AsNoTracking()
            .Where(e => e.Id == session.EditionId).Select(e => e.SegmentCount).FirstAsync(ct);

        var copied = await Db.Set<LekhanaEntry>().AsNoTracking().Alive()
            .CountAsync(e => e.SessionId == session.Id && e.CopiedAt != null, ct);

        var next = session.Status == LekhanaStatus.InProgress ? (await FindNextAsync(session, ct))?.Id : null;
        var percent = total == 0 ? 0 : Math.Round(100.0 * copied / total, 1);
        return new SessionProgress(session.Id, session.Status, total, copied, percent, next);
    }
}
