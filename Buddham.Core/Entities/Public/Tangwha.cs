using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;
namespace Buddham.Core.Entities.Public;

public class Tangwha : EntityBase
{
    public string Title { get; set; } = null!;
    public string? ChineseTitle { get; set; }

    // 불보살|나한|신중|산신|기타
    public BuddhistCategory Category { get; set; } = BuddhistCategory.Bulbosal;
    public TangwhaTier Tier { get; set; }
    public string? Era { get; set; }
    public string? Temple { get; set; }
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ImageUrl { get; set; }
}
