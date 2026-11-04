using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 추상적 작품단위
/// 언어•판본과 무관한 "그 경" 자체
/// </summary>
public class CanonWork : EntityBase
{
    public long CollectionId { get; set; }
    public CanonCollection Collection { get; set; } = null!;

    public long? GroupId { get; set; }
    public CanonGroup? Group { get; set; }

    public WorkKind Kind { get; set; }
}
