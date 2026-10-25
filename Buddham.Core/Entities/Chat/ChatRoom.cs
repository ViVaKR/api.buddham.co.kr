using Buddham.Core.Common.Primitives;
using Buddham.Core.Interfaces;

namespace Buddham.Core.Entities.Chat;

public class ChatRoom : GuidEntityBase, IActivatable
{
    public required string RoomName { get; set; }
    public required Guid OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public string? Description { get; set; }
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = [];
    public virtual ICollection<RoomParticipant> RoomParticipants { get; set; } = [];
    public int CurrentPariticipants => RoomParticipants.Count(x => x.IsOnline);
    public bool IsActive { get; set; } = true;
}
