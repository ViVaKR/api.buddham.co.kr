using System.Text.Json;
using Buddham.Core.Common.Options;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces.Cache;
using StackExchange.Redis;

namespace Buddham.Shared.Services.Cache;

public class RedisCacheService(IConnectionMultiplexer redis) : IRedisCacheService
{
    private readonly IDatabase db = redis.GetDatabase();

    /// <summary>
    /// Get Cache
    /// </summary>
    public async Task<CacheResult<T>> GetOrSetCacheAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        // 1. Redis 에서 바이트 형태로 바로 읽기 (성능 향상)
        var cachedData = await db.StringGetAsync(key);
        if (cachedData.HasValue)
        {
            var data = JsonSerializer.Deserialize<T>((ReadOnlySpan<byte>)cachedData, AppJsonOptions.Default);
            var ttl = await db.KeyTimeToLiveAsync(key);
            var expireAt = DateTime.UtcNow + (ttl ?? TimeSpan.Zero);
            var result = new CacheResult<T>(data, expireAt, true);
            return result;
        }

        var newData = await factory();

        if (newData == null) return new CacheResult<T>(newData, DateTime.UtcNow, false);
        // 비동기적 직렬화 저장
        var serialized = JsonSerializer.Serialize(newData, AppJsonOptions.Default);
        await db.StringSetAsync(key, serialized, expiration ?? TimeSpan.FromHours(1));

        return new CacheResult<T>(newData, DateTime.UtcNow, false);
    }

    public async Task RemoveAsync(string key) => await db.KeyDeleteAsync(key);

    /// <summary>
    /// 💡 패턴 기반 삭제
    /// 예: "Scripture:*" 패턴으로 관련 캐시 일괄 처단
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        var endpoints = redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = redis.GetServer(endpoint);
            // ⚠️ 주의: Keys는 대규모 서버에서 느릴 수 있으므로 SCAN을 활용하거나 패턴을 명확히 합니다.
            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0) await db.KeyDeleteAsync(keys);
        }
    }

    /// <summary>
    /// 패턴기반 삭제
    /// </summary>
    public async Task RemoveByPrefixAsync(string prefix)
    {
        var pattern = prefix.EndsWith('*') ? prefix : prefix + "*";
        var endpoints = redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = redis.GetServer(endpoint);
            var keys = server.Keys(pattern: pattern, pageSize: 100).ToArray();
            if (keys.Length > 0) await db.KeyDeleteAsync(keys);
        }
    }
}