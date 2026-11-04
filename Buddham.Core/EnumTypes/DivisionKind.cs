namespace Buddham.Core.EnumTypes;

/// <summary>
/// 삼장•부 층위의 성격
/// 대승•밀교 등 삼장 밖의 구도를 위해 Other 계열을 둡니다.
/// </summary>
public enum DivisionKind
{
    Sutta, // 경장
    Vinaya, // 율장
    Abhidhamma, // 논장
    MahayanaSutra, // 대등경전부(반야, 법화, 화엄 등)
    Esoteric, // 밀교부
    Treatise, // 논서 (중관, 유가 등)
    Other
}
