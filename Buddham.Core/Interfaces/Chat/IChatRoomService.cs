using Buddham.Core.Entities.Chat;

namespace Buddham.Core.Interfaces.Chat;

public interface IChatRoomService
{
    Task<ChatRoom?> GetRoomByIdAsync(Guid roomId);
    Task<List<ChatRoom>> GetActiveRoomsAsync();
    Task<ChatRoom> CreateRoomAsync(string roomName, string? description, string ownerId, string ownerName);
    Task<bool> DeleteRoomAsync(Guid roomId, string userId, bool isAdmin);
    Task<bool> IsRoomOwnerAsync(Guid roomId, string userId);
}