using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 교리 주제 트리(사성제, 연기, 팔정도...)
/// 원문 위에 얹는 색인 츠우이며
/// 텍스트 컨테이너가 아님
/// </summary>
public class DhammaTopic : EntityBase
{
    public long? ParentId { get; set; }
    public DhammaTopic? Parent { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public string? NamePali { get; set; }
    public string? NameSanskrit { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public ICollection<DhammaTopic> Children { get; set; } = [];
    public ICollection<CanonSegmentTopic> Segments { get; set; } = [];
}
