using Buddham.Core.Common.Primitives;
using Buddham.Core.Entities.Auth;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Comment;
using Buddham.Core.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Comment;

public class GenericCommentService<TComment, TReaction>(
    DbContext context
) : ICommentBridge
    where TComment : CommentBase, ICommentTarget, new()
    where TReaction : ReactionBase, new()
{
    private readonly DbSet<TComment> Comments = context.Set<TComment>();
    private readonly DbSet<TReaction> Reactions = context.Set<TReaction>();

    /// <summary>
    /// 댓글 목록조회
    /// </summary>
    public async Task<List<CommentSchema.Response>> GetRepliesAsync(long
        targetId, Guid userId)
    {
        var propertyName = TComment.TargetColumnPropertyName;
        var comments = await Comments
            .AsNoTracking()
            .Where(x => EF.Property<long>(x, propertyName) == targetId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var commentIds = comments.Select(x => x.Id).ToList();
        var reactions = await Reactions
            .AsNoTracking()
            .Where(x => commentIds.Contains(EF.Property<long>(x, "CommentId")) &&
                        EF.Property<Guid>(x, "UserId") == userId)
            .Select(x => new
            {
                CommentId = EF.Property<long>(x, "CommentId"),
                x.ReactionType
            })
            .ToListAsync();

        var reactionDict = reactions.ToDictionary(x => x.CommentId, x => x.ReactionType);

        return [.. comments.Select(x => ToDTO(x, reactionDict))];
    }

    /// <summary>
    /// 구체적 엔티티(TComment) 로 변환
    /// </summary>
    public async Task<CommentSchema.Response> CreateAsync(CommentSchema.Request request, ApplicationUser user)
    {
        var comment = new TComment
        {
            RootId = request.RootId,
            ParentId = request.ParentId,
            Content = request.Content,
            MentionedUserName = request.MentionedUserName
        };

        // 1단계 댓글일 때만 외부에서 넘겨준 TargetId를 세팅합니다.
        if (!request.ParentId.HasValue)
        {
            comment.TargetId = request.TargetId ?? throw new ArgumentException("원댓글 작성 시 TargetId 는 필수 입니다.");
        }

        return await CreateInternalAsync(comment, user);
    }

    /// <summary>
    /// 댓글/대댓글 생성
    /// </summary>
    public async Task<CommentSchema.Response> CreateAsync(TComment comment, ApplicationUser user)
    {
        comment.UserId = user.Id;
        comment.Pseudonym = user.Pseudonym;
        comment.Avatar = user.Avatar;
        comment.CreatedAt = DateTime.UtcNow;

        // 대댓글인 경우 RootId, MentionedUserName 자동 설정
        if (comment.ParentId.HasValue)
        {
            var parent = await Comments
                             .AsNoTracking()
                             .FirstOrDefaultAsync(x => x.Id == comment.ParentId.Value)
                         ?? throw new KeyNotFoundException("부모 댓글이 존재하지 않습니다.");

            comment.RootId = parent.RootId ?? parent.Id;
            // ✅ 이제 문자열 "ScriptureId" 없이도 안전하게 복사 가능!
            // 어떤 댓글 타입이 오더라도 TargetId를 통해 소속 ID를 상속받습니다.
            // ★ [핵심 보완] 부모의 TargetId를 상속받되,
            // 만약 프론트에서 dto로 TargetId(GuideId)를 직접 넘겨줬다면 그것을 우선 적용!
            if (comment.TargetId == 0)
            {
                comment.TargetId = parent.TargetId;
            }
            // comment.TargetId = parent.TargetId;
            comment.MentionedUserName = parent.Pseudonym;
        }
        else
        {
            // 1단계 댓글은 rootId = null!
            comment.RootId = null;
        }

        await Comments.AddAsync(comment);
        await context.SaveChangesAsync();
        return ToDTO(comment, []);
    }

    /// <summary>
    /// 리액션
    /// </summary>
    public async Task<ReactionSchema.Response> ToggleReactionAsync(
        long commentId,
        Guid userId,
        ReactionType newType)
    {
        // ① 기존에 이 중생이 이 댓글에 했던 리액션이 있는지 조회
        var existing = await Reactions.FirstOrDefaultAsync(x =>
            EF.Property<long>(x, "CommentId") == commentId &&
            EF.Property<Guid>(x, "UserId") == userId);

        var finalType = ReactionType.None;

        if (existing is null)
        {
            // ── [인연의 시작: Insert] ──
            var reaction = new TReaction();
            reaction.SetTarget(commentId, userId, newType);
            await Reactions.AddAsync(reaction);

            // 해당 카운트 +1
            await UpdateCommentCountAsync(commentId, newType, 1);
            finalType = newType;
        }
        else if (existing.ReactionType == newType)
        {
            // ── [마음을 거둠: Delete] ──
            Reactions.Remove(existing);

            // 해당 카운트 -1
            await UpdateCommentCountAsync(commentId, newType, -1);
        }
        else
        {
            // ── [마음이 변함: Update] ──
            var oldType = existing.ReactionType;

            // 리액션 타입 변경
            existing.ReactionType = newType;
            // 기존 타입은 -1, 새로운 타입은 +1 (원자적 교체)

            await UpdateCommentCountAsync(commentId, oldType, -1);
            await UpdateCommentCountAsync(commentId, newType, 1);
            finalType = newType;
        }

        // 트랜잭션 커밋
        await context.SaveChangesAsync();

        // 최종 카운트를 DB에서 청정하게 조회하여 반환
        var counts = await Comments
                         .Where(x => x.Id == commentId)
                         .Select(c => new ReactionSchema.Response
                         {
                             CurrentReaction = finalType,
                             HapjangCount = c.HapjangCount,
                             LotusCount = c.LotusCount,
                             MeritCount = c.MeritCount
                         })
                         .FirstOrDefaultAsync()
                     ?? throw new KeyNotFoundException("댓글을 찾을 수 없습니다.");
        return counts;
    }

    /// <summary>
    /// 댓글 삭제
    /// </summary>
    public async Task DeleteAsync(long id, Guid userId, bool isAdmin)
    {
        var comment = await Comments.FindAsync(id) ?? throw new KeyNotFoundException();

        if (comment.UserId != userId && !isAdmin)
            throw new UnauthorizedAccessException();

        // 원 댓글이면 자식 전부 삭제
        if (comment.RootId == null)
            await Comments.Where(x => x.RootId == id || x.Id == id).ExecuteDeleteAsync();
        else
            await Comments.Where(x => x.Id == id).ExecuteDeleteAsync();
    }

    /// <summary>
    /// 생성
    /// </summary>
    private async Task<CommentSchema.Response> CreateInternalAsync(TComment comment, ApplicationUser user)
    {
        comment.UserId = user.Id;
        comment.Pseudonym = user.Pseudonym;
        comment.Avatar = user.Avatar;
        comment.CreatedAt = DateTime.UtcNow;

        if (comment.ParentId.HasValue)
        {
            var parent = await Comments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == comment.ParentId.Value)
                         ?? throw new KeyNotFoundException("부모 댓글이 존재하지 않습니다.");
            comment.RootId = parent.RootId ?? parent.Id;
            comment.TargetId = parent.TargetId;
            comment.MentionedUserName = parent.Pseudonym;
        }
        else
        {
            comment.RootId = null;
        }

        await Comments.AddAsync(comment);
        await context.SaveChangesAsync();
        return ToDTO(comment, []);
    }

    /// <summary>
    /// 동시성 정합성 원자적 카운트 증감
    /// </summary>
    private async Task UpdateCommentCountAsync(long commentId, ReactionType type, int value)
    {
        if (type == ReactionType.None) return;

        await Comments
            .Where(x => x.Id == commentId)
            .ExecuteUpdateAsync(s =>
            {
                _ = type switch
                {
                    ReactionType.Hapjang => s.SetProperty(c => c.HapjangCount,
                        c => Math.Max(0, c.HapjangCount + value)),
                    ReactionType.Lotus => s.SetProperty(c => c.LotusCount, c => Math.Max(0, c.LotusCount + value)),
                    ReactionType.Merit => s.SetProperty(c => c.MeritCount, c => Math.Max(0, c.MeritCount + value)),
                    _ => s
                };
            });
    }

    // 공통 DTO 변환
    private static CommentSchema.Response ToDTO(
        TComment x,
        Dictionary<long, ReactionType> reactions) => new()
        {
            Id = x.Id,
            RootId = x.RootId,
            ParentId = x.ParentId,
            MentionedUserName = x.MentionedUserName,
            Content = x.Content,
            UserId = x.UserId,
            Pseudonym = x.Pseudonym,
            Avatar = x.Avatar,
            CurrentReaction = reactions.GetValueOrDefault(x.Id, ReactionType.None),
            HapjangCount = x.HapjangCount,
            LotusCount = x.LotusCount,
            MeritCount = x.MeritCount,
            CreatedAt = x.CreatedAt,
            ModifiedAt = x.ModifiedAt,
        };
}


/*
// .Where(x => x.TargetId == targetId && x.RootId == null)

 EF.Property<long>   (x,   "GuideId")
   //    ↑               ↑         ↑
   // 컬럼 타입        엔티티    C# 프로퍼티 이름
   // (DB 타입 기준)  인스턴스  (컬럼명 아님! 클래스 멤버명)
 EF.Property<T>는 EF Core한테 "이 문자열 이름의 프로퍼티를 SQL WHERE 절로 번역해줘" 라고 부탁하는 특수 주문이다. 🪄

// 🔥 HashSet → Dictionary 로 교체!
var reactions = await Reactions
   .AsNoTracking()
   .Where(x => commentIds.Contains(EF.Property<long>(x, "CommentId")) &&
               EF.Property<Guid>(x, "UserId") == userId)
    .ToDictionaryAsync(
       x => EF.Property<long>(x, "CommentId"),
        x => x.ReactionType);

// 🔥 핵심: Select 안에서 EF.Property 사용 (SQL 번역 단계)
// 그 다음 ToListAsync 로 메모리로 가져온 뒤 Dictionary 변환!
 */
