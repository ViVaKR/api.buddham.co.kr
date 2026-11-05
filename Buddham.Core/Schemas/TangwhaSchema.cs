using Buddham.Core.EnumTypes;

namespace Buddham.Core.Schemas;

public class TangwhaSchema
{
    public record Response
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string? ChineseTitle { get; set; }
        public BuddhistCategory Category { get; set; } = BuddhistCategory.Bulbosal;
        public TangwhaTier Tier { get; set; }
        public string? Era { get; set; }
        public string? Temple { get; set; }
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ImageUrl { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDelete { get; set; }
    }
    public record Request
    {
        public long? Id { get; set; }
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
}
