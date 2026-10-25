using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Canon;

public class CanonCollection : EntityBase
{
    public required string NameKo { get; set; } // 고려대장경, 대정신수대장경
    public string? NameOriginal { get; set; } // 高麗大藏經, 大正新脩大藏經
    public string? Abbreviation { get; set; } // K, T, N, P (표준 약호)
    public string? Description { get; set; }
    public string? Tradition { get; set; } // 한역 / 빠알리어 / 티벳역 등
    public int? PublishedYear { get; set; }
    public int SortOrder { get; set; }

    public virtual ICollection<CanonWork> Works { get; set; } = [];
}
