using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 추상적 작품단위
/// 언어•판본과 무관한 "그 경" 자체
/// </summary>
public class CanonWork : EntityBase
{
    public long CollectionId { get; set; }
    public CanonCollection Collection { get; set; } = null!;

    public long? GroupId { get; set; }
    public CanonGroup? Group { get; set; }

    public WorkKind Kind { get; set; }
    public TextLayer Layer { get; set; } = TextLayer.Root;

    // 이 작품이 주석서일 때 해설 대상 원전
    public long? CommentaryOfWorkId { get; set; }
    public CanonWork? CommentaryOf { get; set; }

    // 내부 안정 키
    // 예: sn56.11
    public string Code { get; set; } = string.Empty;
    public string TitleKo { get; set; } = string.Empty;
    public string? TitleOriginal { get; set; }
    public string? TitleRoman { get; set; }
    public string? Summary { get; set; }

    public long? SpeakerId { get; set; }
    public CanonSpeaker? Speaker { get; set; }

    // 전승상 귀속. 학술적 평가는 AttributionNote
    public AttributionKind Attribution { get; set; } = AttributionKind.Unknown;
    public string? AttributionNote { get; set; }

    public int SortOrder { get; set; }

    public ICollection<CanonWork> Commentaries { get; set; } = [];
    public ICollection<CanonWorkIdentifier> Identifiers { get; set; } = [];
    public ICollection<CanonEdition> Editions { get; set; } = [];
    public ICollection<CanonParallel> ParallelsFrom { get; set; } = [];
    public ICollection<CanonParallel> ParallelsTo { get; set; } = [];
}
