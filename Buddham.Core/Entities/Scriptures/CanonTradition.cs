using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 전승 계통•부파
/// 상좌부, 설일체유부, 법장부, 대중부, 대승 등
/// 계통도를 위해 자기참조 허용
/// </summary>
public class CanonTradition : EntityBase
{
    public long? ParentId { get; set; }
    public CanonTradition? Parnet { get; set; }

    public string Code { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public string? NameOriginal { get; set; }
    public string? NameRoman { get; set; }
    public string? Description { get; set; }

    public ICollection<CanonTradition> Children { get; set; } = [];
    public ICollection<Canon> Canons { get; set; } = [];
    public ICollection<CanonCollection> Collections { get; set; } = [];
}
