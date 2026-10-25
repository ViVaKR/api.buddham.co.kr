using System.Security.Claims;
using Buddham.Core.Interfaces.Chat;
using Buddham.Core.Schemas;
using Buddham.Shared.Constants;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Buddham.Chat.Hubs;

public class ChatHub(IChatMessageService chatMessageService, IChatRoomService chatRoomService) : Hub
{
    #region 헬퍼

    private string GetUserIdSafe() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                      ?? Context.User?.FindFirst("sub")?.Value
                                      ?? "UNKNOWN_USER_ID";

    private string GetUserAvatar() => Context.User?.FindFirst(JwtRegisteredClaimNames.Picture)?.Value ?? "buddha.png";

    private string GetDisplayName() => Context.User?.FindFirst(JwtRegisteredClaimNames.PreferredUsername)?.Value ?? "UNKNOWN_USER_NAME";

    #endregion

    #region 연결 라이프사이클

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync(ChatMethod.Connected, Context.ConnectionId);

        // 연결 후 활성화된 방 목록 푸시
        await BroadcastRoomList();

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var participations = await chatMessageService.GetParticipationsByConnectionIdAsync(Context.ConnectionId);
            foreach (var p in participations)
            {
                var deleteRequest = new ChatSchema.RemoveParticipantRequest
                {
                    RoomId = p.RoomId,
                    Userid = p.UserId,
                    ConnectionId = Context.ConnectionId
                };
                await chatMessageService.RemoveParticipantAsync(deleteRequest);
                await Clients.OthersInGroup(p.RoomId.ToString()).SendAsync(ChatMethod.UserLeft, new { p.UserId, p.UserName, p.RoomId });
                await BroadcastParticipants(p.RoomId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatHub Disconnect Error] {ex.Message}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion


    #region 일반 방 및 메시지 처리

    public async Task GetRooms()
    {
        await BroadcastRoomList();
    }

    public async Task JoinRoom(string roomId)
    {
        if (Guid.TryParse(roomId, out var roomGuid))
        {
            var userId = Guid.Parse(GetUserIdSafe());
            var userName = GetDisplayName();
            var avatar = GetUserAvatar();

            var upsertParticipantRequest = new ChatSchema.UpsertParticipantRequest
            {
                RoomId = roomGuid,
                Userid = userId,
                UserName = userName,
                Avatar = avatar,
                ConnectionId = Context.ConnectionId
            };

            // 1. SignalR 그룹에 추가
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // 2. 서비스 단에 참여자 추가 기록
            await chatMessageService.UpsertParticipantAsync(upsertParticipantRequest);

            // 3. 그룹 내 다른 이들에게 입장 알림
            await Clients.Group(roomId).SendAsync(ChatMethod.UserJoined, new { roomId = roomGuid, userId, userName, avatar });
            await Clients.Caller.SendAsync(ChatMethod.JoinedRoom, new { roomId = roomGuid, message = "방에 성공적으로 입장했습니다." });
            // 4. 최신 참여자 목록 브로드캐스트
            await BroadcastParticipants(roomGuid);
        }
    }

    public async Task LeaveRoom(string roomId)
    {
        if (Guid.TryParse(roomId, out var roomGuid))
        {
            var userId = Guid.Parse(GetUserIdSafe());
            var userName = GetDisplayName();
            var deleteRequest = new ChatSchema.RemoveParticipantRequest
            {
                RoomId = roomGuid,
                Userid = userId,
                ConnectionId = Context.ConnectionId
            };
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await chatMessageService.RemoveParticipantAsync(deleteRequest);
            await Clients.Group(roomId).SendAsync(ChatMethod.UserLeft, new { roomId = roomGuid, userId, userName });
            await BroadcastParticipants(roomGuid);
        }
    }

    public async Task SendMessage(ChatSchema.ChatMessageRequest request)
    {
        // 1. DB 또는 캐시 저장소에 메시지 영속화
        await chatMessageService.SaveMessageAsync(request);

        // 2. 해당 그룹(방) 전체에 전송
        await Clients.Group(request.RoomId.ToString()).SendAsync(ChatMethod.ReceiveMessage, request);
    }

    #endregion

    #region 브로드캐스트 헬퍼

    private async Task BroadcastParticipants(Guid roomId)
    {
        var participants = await chatMessageService.GetRoomParticipantsAsync(roomId);
        await Clients.Group(roomId.ToString()).SendAsync(ChatMethod.ReceiveParticipants, new { RoomId = roomId, Participants = participants });
    }

    private async Task BroadcastRoomList()
    {
        var rooms = await chatRoomService.GetActiveRoomsAsync();
        await Clients.Caller.SendAsync(ChatMethod.ReceiveRooms, rooms);
    }

    #endregion
}
