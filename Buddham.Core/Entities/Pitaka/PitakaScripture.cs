using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Pitaka;

public class PitakaScripture : EntityBase
{
    public long CategoryId { get; set; }
    public required string TitleKo { get; set; }
    public string? TitlePali { get; set; }
    public required string Content { get; set; }
    public string? Commentary { get; set; }
    public Guid UserId { get; set; }
    public string? AuthorPseudonym { get; set; }
    public string? SearchVector { get; set; }
    public float[]? Embedding { get; set; }
    public virtual PitakaCategory Category { get; set; } = null!;
}