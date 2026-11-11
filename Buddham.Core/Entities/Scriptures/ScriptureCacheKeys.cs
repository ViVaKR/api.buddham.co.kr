namespace Buddham.Core.Scriptures;

/// <summary>Redis 키 접두어. 변경 서비스가 RemoveByPrefixAsync 로 한 번에 무효화한다.</summary>
public static class ScriptureCacheKeys
{
    public const string Prefix = "Scripture:";

    /// <summary>메뉴용 전체 트리(판본계통 → 부 → 집성 → 그룹)</summary>
    public const string Tree = Prefix + "tree";
}
