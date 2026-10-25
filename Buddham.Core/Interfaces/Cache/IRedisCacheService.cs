using Buddham.Core.Common.Results;

namespace Buddham.Core.Interfaces.Cache;

public interface IRedisCacheService
{
    Task<CacheResult<T>> GetOrSetCacheAsync<T>(
        string key, 
        Func<Task<T>> factory,
        TimeSpan? expiration = null);

    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix); 
}
