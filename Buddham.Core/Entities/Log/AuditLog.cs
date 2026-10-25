namespace Buddham.Core.Entities.Log;

/// <summary>
/// 감사로그
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public string? Category { get; set; }
    public required Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public required string ActionType { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? OldValue { get; set; } // JSON 직렬화
    public string? NewValue { get; set; } // JSON 직렬화
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // public Vector? Embedding { get; set; }
    public float[]? Embedding { get; set; }
    
    // public NpgsqlTsVector? SearchVector { get; set; }
    public string? SearchVector { get; set; }
}
