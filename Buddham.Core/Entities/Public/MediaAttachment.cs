using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Public;

namespace Buddham.Core.Entities.Public;

public class MediaAttachment : EntityBase, IMediaTarget
{
    public long TargetId { get; set; }
    public static string TargetColumnPropertyName => nameof(TargetId);

    /// <summary>
    /// 해당타입
    ///  "PitakaScripture", "DharmaTerm", "Guide" 등
    /// </summary>
    public required string TargetType { get; set; }

    /// <summary>
    /// 실제 컬럼
    /// 모든 미디어 공통, 실제 컬럼
    /// 자주 조회/정렬/필터 대상이라 진짜 컬럼이어야 함
    /// enum: File, Video, Youtube, Tanka, ChatLog, AiChatLog
    /// </summary>
    public required MediaType Type { get; set; }

    public required string Title { get; set; }
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int SortOrder { get; set; }
    public Guid UserId { get; set; }
    public string? AuthorPseudonym { get; set; }

    /// <summary>
    /// 메디아 타입 
    /// jsonb
    /// 타입별로 들쭉날쭉한 속성만 JSONB로 (여기가 진짜 JSONB 쓸 자리) ──
    /// 예: Youtube면 {"videoId": "xxx", "startSeconds": 120, "channelName": "..."}
    ///     Tanka면    {"artist": "...", "era": "조선", "materials": "비단에 채색"}
    ///     ChatLog면  {"sessionId": 123, "messageCount": 45}
    /// </summary>
    public string? Metadata { get; set; } // jsonb
}