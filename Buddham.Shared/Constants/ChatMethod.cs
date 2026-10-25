namespace Buddham.Shared.Constants;

public static class ChatMethod
{
    // Connection
    public const string Connected = "Connected";
    public const string Disconnected = "Disconnected";

    // Rooms
    public const string ReceiveRooms = "ReceiveRooms";
    public const string ReceiveRoomInfo = "ReceiveRoomInfo";
    public const string RoomCreated = "RoomCreated";
    public const string RoomDeleted = "RoomDeleted";

    // NickName
    public const string NickNameSet = "NickNameSet";

    // Join/Leave
    public const string JoinedRoom = "JoinedRoom";
    public const string LeftRoom = "LeftRoom";
    public const string UserJoined = "UserJoined";
    public const string UserLeft = "UserLeft";

    // Messages
    public const string ReceiveMessage = "ReceiveMessage";
    public const string ReceiveMessageHistory = "ReceiveMessageHistory";
    public const string ReceiveMessageHistoryByCursor = "ReceiveMessageHistoryByCursor";
    public const string ReceiveShortMessage = "ReceiveShortMessage";

    public const string ReceiveParticipants = "ReceiveParticipants";

    // System
    public const string Error = "Error";
}