using Microsoft.Extensions.DependencyInjection;

namespace Buddham.Shared.Services.Scripture;

public static class ScriptureServiceCollectionExtensions
{
    /// <summary>
    /// 경전(scripture 스키마) 비즈니스 서비스 등록.
    /// 임베딩은 기존 IOllamaService(bge-m3)를 그대로 쓴다.
    /// 규칙 예외는 서비스 경계(ScriptureResults.RunAsync)에서 Result 로 바뀌므로 별도 예외 핸들러는 필요 없다.
    /// ServiceCollectionExtensions.AddBusinessService 안에서 호출하면 된다.
    /// </summary>
    public static IServiceCollection AddScriptureServices(this IServiceCollection services)
    {
        services.AddScoped<HierarchyService>();
        services.AddScoped<WorkService>();
        services.AddScoped<EditionService>();
        services.AddScoped<SegmentEmbeddingService>();
        services.AddScoped<ScriptureQueryService>();
        services.AddScoped<ParallelService>();
        services.AddScoped<TopicService>();
        services.AddScoped<LekhanaService>();

        return services;
    }
}
