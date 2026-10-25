using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Upaya;

public class UpayaMedia : EntityBase
{
    public long CategoryId { get; set; }
    public UpayaType Type { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    // 자체 호스팅 파일 (Tanka/Audio/Video/Image) — Public.FileData 스칼라 참조 (FK 없음)
    public long? FileDataId { get; set; }

    // 외부 콘텐츠 (Youtube 등)
    public string? ExternalUrl { get; set; }
    public string? YoutubeVideoId { get; set; }
    public string? ChannelName { get; set; }

    public string? ThumbnailUrl { get; set; }
    public int? DurationSeconds { get; set; }

    // 탱화/음성 등 창작자 메타데이터
    public string? Creator { get; set; } // 화가, 법사, 채널명 등
    public string? Era { get; set; } // 시대 (탱화 등)

    public string? Metadata { get; set; } // jsonb — 타입별 부가 속성

    public Guid UserId { get; set; }
    public string? AuthorPseudonym { get; set; }

    public string? SearchVector { get; set; }
    public float[]? Embedding { get; set; }

    public virtual UpayaCategory Category { get; set; } = null!;
}