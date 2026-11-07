using System.Collections.Concurrent;
using Buddham.Core.Common.Options;
using Buddham.Core.Interfaces.Cache;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Caching;

/// <summary>
/// 엔티티 캐시 팩토리 (Singleton).
///  1) Prefix 당 EntityCache 인스턴스를 하나만 만들어 재사용한다
///     (서비스는 Scoped 라 요청마다 Create 가 호출되므로, 매번 새로 만들지 않는다).
///  2) Prefix/Tag 가 서로 다른 엔티티에서 중복 사용되면 즉시 예외를 던진다.
///     (서비스를 복붙하고 Prefix 를 안 바꾸면 다른 엔티티의 캐시 데이터를 읽는 사고가 난다)
/// </summary>
public sealed class EntityCacheFactory(HybridCache cache, ILoggerFactory loggerFactory)
    : IEntityCacheFactory
{
    private readonly ConcurrentDictionary<string, (EntityCacheOptions Options, IEntityCache Cache)> _byPrefix = new();
    private readonly ConcurrentDictionary<string, string> _prefixByTag = new();

    public IEntityCache Create(EntityCacheOptions options)
    {
        var entry = _byPrefix.GetOrAdd(options.Prefix, _ =>
            (options, new EntityCache(cache, loggerFactory.CreateLogger($"EntityCache.{options.EntityName}"), options)));

        if (entry.Options.EntityName != options.EntityName || entry.Options.Tag != options.Tag)
            throw new InvalidOperationException(
                $"캐시 Prefix '{options.Prefix}' 가 '{entry.Options.EntityName}' 와 '{options.EntityName}' 에서 중복 사용되고 있습니다. " +
                "엔티티마다 고유한 Prefix 를 써야 합니다 (EntityCacheOptionsFactory.For<TEntity> 권장).");

        var owner = _prefixByTag.GetOrAdd(options.Tag, options.Prefix);
        if (owner != options.Prefix)
            throw new InvalidOperationException(
                $"캐시 Tag '{options.Tag}' 가 Prefix '{owner}' 와 '{options.Prefix}' 에서 중복 사용되고 있습니다. " +
                "같은 Tag 는 서로의 캐시를 함께 무효화합니다.");

        return entry.Cache;
    }
}
