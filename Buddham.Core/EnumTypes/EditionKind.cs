namespace Buddham.Core.EnumTypes;

/// <summary>
/// 판본의 성격 원문은 불변 층
/// 번역은 그 위의 별도 층
/// </summary>
public enum EditionKind
{
    Original,
    Translation,
    Transliteration
}
