using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Buddham.Core.Common;
using Buddham.Core.Common.Options;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces.Cache;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Caching;

public sealed class EntityCache : IEntityCache
{
    private readonly HybridCache _cache;
    private readonly ILogger _logger;
    private readonly EntityCacheOptions _options;
    private readonly HybridCacheEntryOptions _entryOptions;
    private readonly string[] _tags;

    public EntityCache(HybridCache cache, ILogger logger, EntityCacheOptions options)
    {
        _cache = cache;
        _logger = logger;
        _options = options;
        _tags = [options.Tag];
        _entryOptions = new HybridCacheEntryOptions
        {
            Expiration = options.Expiration,
            LocalCacheExpiration = options.LocalExpiration
        };
    }

    public string Key(string kind, params object?[] parts)
    {
        var raw = string.Join('\u001F',
            parts.Select(p => Convert.ToString(p, CultureInfo.InvariantCulture) ?? string.Empty));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return $"{_options.Prefix}:{kind}:{hash}";
    }

    public ValueTask<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken ct)
        => _cache.GetOrCreateAsync(key, factory, _entryOptions, _tags, ct);

    public async ValueTask<T?> GetOrCreateNullableAsync<T>(
        string key, Func<CancellationToken, ValueTask<T?>> factory, CancellationToken ct) where T : class
    {
        var box = await GetOrCreateAsync(
            key, async token => new CacheBox<T>(await factory(token)), ct);
        return box.Value;
    }

    public async Task<(T? Value, Result? Failure)> GetOrCreateIfSuccessAsync<T>(
        string key,
        Func<CancellationToken, Task<(T? Value, Result? Failure)>> factory,
        HybridCacheEntryOptions options,
        CancellationToken ct) where T : class
    {
        try
        {
            var value = await _cache.GetOrCreateAsync(
                key,
                async token =>
                {
                    var (v, f) = await factory(token);
                    // 실패는 예외로 팩토리를 빠져나가야 HybridCache 가 저장하지 않는다.
                    return v ?? throw new CacheFactoryFailureException(
                        f ?? Result.Failure(RsCode.Error, "캐시 값 생성에 실패했습니다."));
                },
                options,
                tags: null,
                cancellationToken: ct);

            return (value, null);
        }
        catch (CacheFactoryFailureException ex)
        {
            return (null, ex.Failure);
        }
    }

    public async Task InvalidateAsync()
    {
        try
        {
            await _cache.RemoveByTagAsync(_options.Tag, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "{Entity} cache invalidation failed. TTL 만료까지 이전 데이터가 보일 수 있습니다.",
                _options.EntityName);
        }
    }

    private sealed class CacheFactoryFailureException(Result failure) : Exception
    {
        public Result Failure { get; } = failure;
    }
}
