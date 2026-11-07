namespace Buddham.Core.Common.Options;

public sealed record EntityCacheOptions
{
    /// <summary>키 접두사. 응답 모양이 바뀌어 배포할 때 버전을 올린다. 예: "guide:v1"</summary>
    public required string Prefix { get; init; }

    /// <summary>엔티티 전체 무효화용 태그. 예: "guides"</summary>
    public required string Tag { get; init; }

    /// <summary>로그 표기용 이름.</summary>
    public required string EntityName { get; init; }

    /// <summary>L2(Redis) TTL.</summary>
    public TimeSpan Expiration { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// L1(파드 메모리) TTL. L1 은 파드별이라 다른 파드에서는 즉시 무효화되지 않으므로,
    /// 이 값이 곧 "다른 파드에서 보일 수 있는 최대 지연"이다.
    /// </summary>
    public TimeSpan LocalExpiration { get; init; } = TimeSpan.FromSeconds(30);
}
