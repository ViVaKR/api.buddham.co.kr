using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Chat;
using Buddham.Core.Interfaces.Chat;
using Buddham.Core.Schemas;
using ChatMessage = Buddham.Core.Entities.Ai.ChatMessage;

namespace Buddham.Shared.Services.Chat;

public class ChatMessageService : IChatMessageService
{
    public async Task<ChatMessage> SaveMessageAsync(ChatSchema.ChatMessageRequest request)
    {
        throw new NotImplementedException();
    }
    public async Task<CursorPagedResult<ChatSchema.ChatMessageResponse>> GetRoomMessagesAsync(CursorPagedQuery pagingQuery)
    {
        throw new NotImplementedException();
    }
    public async Task<IEnumerable<ChatSchema.ChatMessageResponse>> GetRecentMessagesAsync(Guid roomId, DateTime since)
    {
        throw new NotImplementedException();
    }
    public async Task<bool> DeleteMessageAsync(ChatSchema.DeleteMessageRequest request)
    {
        throw new NotImplementedException();
    }
    public async Task<RoomParticipant?> UpsertParticipantAsync(ChatSchema.UpsertParticipantRequest request)
    {
        throw new NotImplementedException();
    }
    public async Task<bool> RemoveParticipantAsync(ChatSchema.RemoveParticipantRequest request)
    {
        throw new NotImplementedException();
    }
    public async Task<bool> SetUserOfflineAsync(Guid userId, string connectionId)
    {
        throw new NotImplementedException();
    }
    public async Task<List<ChatSchema.RoomParticipantResponse>> GetRoomParticipantsAsync(Guid roomId)
    {
        throw new NotImplementedException();
    }
    public async Task<int> GetOnlineParticipantCountAsync(Guid roomId)
    {
        throw new NotImplementedException();
    }
    public async Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId)
    {
        throw new NotImplementedException();
    }
    public async Task<IEnumerable<ChatSchema.RoomParticipantResponse>> GetParticipationsByConnectionIdAsync(string connectionId)
    {
        throw new NotImplementedException();
    }
    public async Task<Dictionary<Guid, int>> GetRoomMessageCountsAsync(List<Guid> roomIds)
    {
        throw new NotImplementedException();
    }
    public async Task<IEnumerable<ChatSchema.RoomParticipantResponse>> RemoveAllParticipationsByConnectionIdAsync(string connectionId)
    {
        throw new NotImplementedException();
    }
}
