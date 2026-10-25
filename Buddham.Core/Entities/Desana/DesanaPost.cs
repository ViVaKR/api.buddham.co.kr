using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Desana;

public class DesanaPost : EntityBase
{
    public DesanaType Type { get; set; }

    public required string Title { get; set; }
    public required string Content { get; set; }
    public string? Summary { get; set; }

    public Guid UserId { get; set; }
    public string? AuthorPseudonym { get; set; }
    public string? AuthorName { get; set; } // 스님/법사 실명 — 게시 계정과 다를 수 있음

    // 스칼라 참조만 (인드라망 원칙) — 이 법문/주석이 다루는 원전
    public long? PitakaScriptureId { get; set; }

    // 시리즈 자기참조 (화엄경 강해 1강, 2강...)
    public long? ParentId { get; set; }
    public int? EpisodeNumber { get; set; }
    public int SortOrder { get; set; }

    public DesanaStatus Status { get; set; } = DesanaStatus.Draft;
    public DateTime? PublishedAt { get; set; }

    public string? SearchVector { get; set; }
    public float[]? Embedding { get; set; }

    public virtual DesanaPost? Parent { get; set; }
    public virtual ICollection<DesanaPost> Children { get; set; } = [];
}