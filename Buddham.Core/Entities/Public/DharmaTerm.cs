using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Public;

public class DharmaTerm : EntityBase
{
    public required string TermKo { get; set; }
    public string? TermSanskrit { get; set; }
    public string? TermHanja { get; set; }
    public string? TermEnglish { get; set; }
    public required string Definition { get; set; }
    public string? Etymology { get; set; }
    public string? Example { get; set; }

    public Guid UserId { get; set; }
    public string? Pseudonym { get; set; }

    public string? SearchVector { get; set; }
    public float[]? Embedding { get; set; }
}
