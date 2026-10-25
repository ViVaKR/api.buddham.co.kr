namespace Buddham.Core.Common.Primitives;

public abstract class CommentBase
{
    public long Id { get; set; }
    public long? RootId { get; set; }
    public long? ParentId { get; set; }
    public string? MentionedUserName { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Pseudonym { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public int HapjangCount { get; set; }
    public int LotusCount { get; set; }
    public int MeritCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
