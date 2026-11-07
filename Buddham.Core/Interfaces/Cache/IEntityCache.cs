using Buddham.Core.Common.Results;
using Microsoft.Extensions.Caching.Hybrid;

namespace Buddham.Core.Interfaces.Cache;

public interface IEntityCache
{
    /// <summary>키 생성: 파라미터를 해시해 길이/특수문자 문제를 없앤다 (파드 간 결정적).</summary>
    string Key(string kind, params object?[] parts);

    /// <summary>
    /// Read-through. 같은 키의 동시 미스는 한 번만 factory 로 간다(스탬피드 방지).
    /// factory 가 던진 예외는 캐시되지 않는다. 태그가 붙으므로 InvalidateAsync 로 정리된다.
    /// </summary>
    ValueTask<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken ct);

    /// <summary>
    /// null 가능 결과용. HybridCache 에 null 을 직접 맡기지 않고 래퍼에 담아 저장한다.
    /// (없는 값도 캐시한다. 쓰기 시 태그 무효화로 정리되므로 안전)
    /// </summary>
    ValueTask<T?> GetOrCreateNullableAsync<T>(
        string key, Func<CancellationToken, ValueTask<T?>> factory, CancellationToken ct) where T : class;

    /// <summary>
    /// (값, 실패) 튜플을 돌려주는 팩토리용. 성공한 값만 캐시하고 실패는 캐시하지 않는다.
    /// 엔티티 데이터와 무관하므로 태그 없이 별도 옵션(기본: Redis 전용 24시간)으로 저장한다.
    /// </summary>
    Task<(T? Value, Result? Failure)> GetOrCreateIfSuccessAsync<T>(
        string key,
        Func<CancellationToken, Task<(T? Value, Result? Failure)>> factory,
        HybridCacheEntryOptions options,
        CancellationToken ct) where T : class;

    /// <summary>
    /// 쓰기 성공 후 호출. DB 는 이미 커밋됐으므로 요청이 취소돼도 무효화를 수행하고,
    /// 캐시 장애가 쓰기 결과를 실패로 만들지 않도록 예외는 삼킨다(TTL 이 최후의 안전망).
    /// </summary>
    Task InvalidateAsync();
}
