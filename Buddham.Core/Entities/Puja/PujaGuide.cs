using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Puja;

public class PujaGuide : EntityBase
{
    public long CategoryId { get; set; }
    public required string Title { get; set; } // 삼배하는 법
    public string? Summary { get; set; }
    public string? Content { get; set; } // 전체 개괄 설명 (단계 진입 전 소개)

    public Guid UserId { get; set; }
    public string? AuthorPseudonym { get; set; }

    public string? SearchVector { get; set; }
    public float[]? Embedding { get; set; }

    public virtual PujaCategory Category { get; set; } = null!;
    public virtual ICollection<PujaGuideStep> Steps { get; set; } = [];
}