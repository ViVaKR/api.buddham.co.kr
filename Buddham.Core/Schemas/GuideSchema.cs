using Buddham.Core.EnumTypes;

namespace Buddham.Core.Schemas;

public static class GuideSchema
{
    public record Response
    {
        public long Id { get; set; }
        public required GuideCategory GuideCategory { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDelete { get; set; }
        public Guid UserId { get; set; }
    }

    public record Request
    {
        public long? Id { get; set; }
        public required GuideCategory GuideCategory { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
    }
}
