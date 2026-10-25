using Buddham.Core.Common.Primitives;

namespace Buddham.Core.Entities.Public;

/// <summary>
/// 사경수행
/// </summary>
public class SutraCopying : EntityBase
{
    /// <summary>
    /// 사경 경전제목
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 사경 경전단락
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// 사경 경전본문
    /// </summary>
    public required string Sutra { get; set; }

    /// <summary>
    /// 작성자 아이디
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// 작성자 필명
    /// </summary>
    public string? Pseudonym { get; set; }

    /// <summary>
    /// 전체 텍스트 검색
    /// </summary>
    public string? SearchVector { get; set; }

    /// <summary>
    /// 의미론 검색
    /// </summary>
    public float[]? Embedding { get; set; }
}
