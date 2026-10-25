using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Chat;
using Buddham.Core.Schemas;
using ChatMessage = Buddham.Core.Entities.Ai.ChatMessage;

namespace Buddham.Core.Interfaces.Chat;

public interface IChatMessageService
{
    // 메시지 관련
    Task<ChatMessage> SaveMessageAsync(ChatSchema.ChatMessageRequest request);
    Task<CursorPagedResult<ChatSchema.ChatMessageResponse>> GetRoomMessagesAsync(CursorPagedQuery pagingQuery);
    Task<IEnumerable<ChatSchema.ChatMessageResponse>> GetRecentMessagesAsync(Guid roomId, DateTime since);

    Task<bool> DeleteMessageAsync(ChatSchema.DeleteMessageRequest request);

    // ==================== 참여자 관리 ====================
    // ✅ 기존 AddParticipantAsync 제거하고 이걸로 교체함.
    Task<RoomParticipant?> UpsertParticipantAsync(ChatSchema.UpsertParticipantRequest request);

    Task<bool> RemoveParticipantAsync(ChatSchema.RemoveParticipantRequest request);
    Task<bool> SetUserOfflineAsync(Guid userId, string connectionId);
    Task<List<ChatSchema.RoomParticipantResponse>> GetRoomParticipantsAsync(Guid roomId);
    Task<int> GetOnlineParticipantCountAsync(Guid roomId);

    // 통계/유틸리티
    Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId);

    // ✅ 추가: ConnectionId로 참여 정보 조회
    Task<IEnumerable<ChatSchema.RoomParticipantResponse>> GetParticipationsByConnectionIdAsync(string connectionId);

    // ==================== 통계/유틸리티 ====================
    Task<Dictionary<Guid, int>> GetRoomMessageCountsAsync(List<Guid> roomIds);

    // ConnectionId가 종료되었을 때 참여했던 모든 방 목록을 반환하면서 DB 상태를 비활성화/삭제하는 메서드
    Task<IEnumerable<ChatSchema.RoomParticipantResponse>> RemoveAllParticipationsByConnectionIdAsync(string connectionId);
}
