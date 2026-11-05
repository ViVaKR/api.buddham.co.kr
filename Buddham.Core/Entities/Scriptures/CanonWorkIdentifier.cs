using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 판본별 경 번호
/// DN 22, SN 56.11, T99 no.262
/// </summary>
public class CanonWorkIdentifier : EntityBase
{
    public long WorkId { get; set; }
    public CanonWork Work { get; set; } = null!;

    // 체계 코드
    // 예: SC, PTS, VRI, T, CBETA, KBC
    public string Scheme { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
