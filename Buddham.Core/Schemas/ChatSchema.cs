namespace Buddham.Core.Schemas;

public static class ChatSchema
{
    public record ChatMessageResponse
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? Avatar { get; set; }
        public required string Content { get; set; }
        public required DateTime SentAt { get; set; }
    }

    public record RoomParticipantResponse
    {
        public Guid Id { get; set; }
        public required Guid RoomId { get; set; }
        public required Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? Avatar { get; set; }
        public string? ConnectionId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActiveAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public bool IsOnline { get; set; }
    }

    public record ChatMessageRequest
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }

        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? Avatar { get; set; }
        public required string Content { get; set; }
        public required DateTime SentAt { get; set; }
    }

    public record DeleteMessageRequest
    {
        public Guid MessageId { get; set; }
        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }
    }

    public record UpsertParticipantRequest
    {
        public Guid RoomId { get; set; }
        public Guid Userid { get; set; }
        public string? UserName { get; set; }
        public string? Avatar { get; set; }
        public required string ConnectionId { get; set; }
    }

    public record RemoveParticipantRequest
    {
        public Guid RoomId { get; set; }
        public Guid Userid { get; set; }
        public required string ConnectionId { get; set; }
    }

    public record Request
    {
        public long SessionId { get; set; }
        public string? Question { get; set; }
    }
}
