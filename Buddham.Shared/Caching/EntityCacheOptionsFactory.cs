using Buddham.Core.Common.Options;

namespace Buddham.Shared.Caching;

/// <summary>
/// 캐시 정책을 엔티티 타입에서 규약으로 만든다. 문자열을 손으로 쓰지 않으므로 복붙 실수가 없다.
/// 예) For&lt;Guide&gt;("법문 가이드", version: 2) → Prefix "guide:v2", Tag "entity:guide"
/// </summary>
public static class EntityCacheOptionsFactory
{
    /// <param name="entityName">로그 표기용 이름.</param>
    /// <param name="version">응답 모양/캐시 타입이 바뀌어 배포할 때 올린다.</param>
    public static EntityCacheOptions For<TEntity>(string entityName, int version = 1)
    {
        var name = typeof(TEntity).Name.ToLowerInvariant();

        return new EntityCacheOptions
        {
            Prefix = $"{name}:v{version}",
            Tag = $"entity:{name}",
            EntityName = entityName
        };
    }
}
