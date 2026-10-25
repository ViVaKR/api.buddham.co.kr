using Buddham.Core.EnumTypes;

namespace Buddham.Core.Schemas;

public static class CommentSchema
{
    public record Response
    {
        public long Id { get; set; }
        public long? RootId { get; set; }
        public long? ParentId { get; set; }
        public string? MentionedUserName { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Pseudonym { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public int LikeCount { get; set; }
        public int HapjangCount { get; set; }
        public int LotusCount { get; set; }
        public int MeritCount { get; set; }
        public ReactionType CurrentReaction { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public record Request
    {
        public long? RootId { get; set; }
        public long? ParentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? MentionedUserName { get; set; }
        public long? TargetId { get; set; }
    }
}
