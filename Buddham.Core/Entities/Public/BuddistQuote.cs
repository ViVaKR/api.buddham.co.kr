using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Public;

public class BuddistQuote : EntityBase
{
    public required string Quote { get; set; }
    public string? Source { get; set; }
    public Guid UserId { get; set; }
    public float[]? Embedding { get; set; }
    public string? SearchVector { get; set; }
}
