using Buddham.Core.Common.Primitives;
using Buddham.Core.Interfaces.Public;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 발화자
/// 붓다, 사리뿟따, 아난다 등
/// </summary>
public class CanonSpeaker : EntityBase, ICodedName
{
    public string Code { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public string? NameOriginal { get; set; }
    public string? NameRoman { get; set; }
    public string? Description { get; set; }
    public ICollection<CanonWork> Works { get; set; } = [];
}
