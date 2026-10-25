using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Buddham.Shared.Extensions;

public static class DataProtectionExtensions
{
    /// <summary>
    /// K8s 다중 파드 분산 환경을 위한 DataProtection 키 저장소 구축 (체 게바라 Redis 무기)
    /// </summary>
    public static IDataProtectionBuilder AddDataProtectionsWithRedis(
        this IServiceCollection services,
        string applicationName,
        string redisConnectionString)
    {
        // 💡 1. 체 게바라 Redis 통신망 연결
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);

        return services
            .AddDataProtection()
            .SetApplicationName(applicationName)
            // 🌟 로컬 파일이 아닌, 중앙 집중형 Redis에 키를 저장하여 다중 파드 완벽 공유!
            .PersistKeysToStackExchangeRedis(redis, "buddham:data-protection:keys");
    }

    /// <summary>
    /// 분산 환경을 고려한 Buddham 데이터 보호 시스템 구축
    /// </summary>
    public static IDataProtectionBuilder AddDataProtections(this IServiceCollection services, string applicationName)
    {
        // 💡 K8s 배포 시 외부 볼륨(PV) 매핑이 용이하도록 범용적인 경로 선점 추천
        // 예: /app/secrets/dataprotection-keys
        var keysFolder = new DirectoryInfo("/root/.aspnet/DataProtection-Keys");

        var builder = services
            .AddDataProtection()
            .PersistKeysToFileSystem(keysFolder)
            .SetApplicationName(applicationName);

        // TODO: 나중에 K8s 다중 파드 띄울 때 Redis로 교체하려면 아래 주석 해제!
        // builder.PersistKeysToStackExchangeRedis(redisConnection, "DataProtection-Keys");

        return builder;
    }
}
