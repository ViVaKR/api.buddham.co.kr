using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Ai;

public class AiArchive : EntityBase
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public string? Commentary { get; set; }
    public Guid UserId { get; set; }
    public string Reference { get; set; } = "[]";
    public float? SearchVector { get; set; }
    public string? Embedding { get; set; }
}
