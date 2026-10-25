using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Ai;

public class AiChatMessage : GuidEntityBase
{
    public long SessionId { get; set; }
    public required string Role { get; set; }
    public string? Content { get; set; }
}
