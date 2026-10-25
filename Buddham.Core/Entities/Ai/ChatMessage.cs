using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Ai;

public class ChatMessage : EntityBase
{
    public long ChatSessionId { get; set; }
    public string? Role { get; set; }
    public string? Content { get; set; }
    public string? ThinkingContent { get; set; }
    public string? SearchVector { get; set; }
    public float[]? Embedding { get; set; }
    public ChatSession? ChatSession { get; set; }
}
