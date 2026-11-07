using Microsoft.Extensions.Caching.Hybrid;

namespace Buddham.Shared.Caching;

/// <summary>자주 쓰는 HybridCacheEntryOptions 프리셋.</summary>
public static class CacheProfiles
{
    /// <summary>데이터와 무관한 값(예: 검색어 임베딩)용. Redis 에만 저장한다.</summary>
    public static HybridCacheEntryOptions RedisOnly(TimeSpan expiration) => new()
    {
        Expiration = expiration,
        Flags = HybridCacheEntryFlags.DisableLocalCache
    };
}
