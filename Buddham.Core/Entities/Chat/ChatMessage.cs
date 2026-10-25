using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Chat;

public class ChatMessage : GuidEntityBase
{
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? Avatar { get; set; }
    public required string Content { get; set; }
    public float[]? Embedding { get; set; }
    public string? SearchVector { get; set; }
    public ChatRoom? Room { get; set; }
}
