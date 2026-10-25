using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Chat;

public class RoomParticipant : GuidEntityBase
{
    public required Guid RoomId { get; set; }
    public required Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? Avatar { get; set; }
    public string? ConnectionId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool IsOnline { get; set; }
    public ChatRoom? Room { get; set; }
}