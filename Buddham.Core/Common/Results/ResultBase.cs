namespace Buddham.Core.Common.Results;

public abstract record ResultBase
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public RsCode Code { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public object? Meta { get; set; }
    public IDictionary<string, string[]>? ValidationErrors { get; init; }
    public string? TraceId { get; init; }
}
