using Buddham.Core.Entities.Chat;
using Buddham.Core.Interfaces.Chat;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Chat;

public class ChatRoomService(ChatDbContext context) : IChatRoomService
{
    public async Task<ChatRoom?> GetRoomByIdAsync(Guid roomId)
    {
        return await context.ChatRooms.Include(x => x.RoomParticipants.Where(x => x.IsOnline))
            .FirstOrDefaultAsync(x => x.Id == roomId && x.IsActive);
    }

    public async Task<List<ChatRoom>> GetActiveRoomsAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<ChatRoom> CreateRoomAsync(string roomName, string? description, string ownerId, string ownerName)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteRoomAsync(Guid roomId, string userId, bool isAdmin)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsRoomOwnerAsync(Guid roomId, string userId)
    {
        throw new NotImplementedException();
    }
}