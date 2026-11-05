using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Scriptures;

/// <summary>
/// 출전•판본 제공처
/// VRI, PTS, CBETA,동국 역경원 등
/// </summary>
public class CanonSource : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public string? SourceUrl { get; set; }
    public string? LicenseName { get; set; }
    public string? LicenseUrl { get; set; }
    public string? Version { get; set; }
    public string? Note { get; set; }
    public ICollection<CanonEdition> Editions { get; set; } = [];
}
