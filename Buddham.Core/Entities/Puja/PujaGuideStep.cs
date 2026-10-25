using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Puja;

public class PujaGuideStep : EntityBase
{
    public long GuideId { get; set; }
    public int StepNumber { get; set; }
    public required string Title { get; set; } // "첫 번째 절"
    public required string Description { get; set; } // 상세 설명 (동작, 마음가짐 등)

    public virtual PujaGuide Guide { get; set; } = null!;
}