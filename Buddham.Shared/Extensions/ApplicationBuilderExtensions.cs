using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Buddham.Shared.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder MapCustomStaticFiles(this IApplicationBuilder app, string requestPath, string physicalPath)
    {
        // 🏢 DI 보급창고에서 로거팩토리를 꺼내와서 이Extension 전용 로거 장군을 소환!
        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Buddham.API.Extensions.MapCustomStaticFiles");
        try
        {
            // 🛡️ 409 빌런 소탕을 위한 예외 방어막 장착!
            if (!Directory.Exists(physicalPath))
            {
                Directory.CreateDirectory(physicalPath);
                logger.LogInformation("📁 새 영토 생성 완료: {PhysicalPath}", physicalPath);
            }
        }
        catch (Exception ex)
        {
            // 폴더 생성에 실패해도 409를 뿜으며 죽지 않고, 경고 로그만 찍은 뒤 유연하게 전진!
            logger.LogWarning("⚠️ 정적 폴더 생성 중 일시적 결계 발생 (권한 등): {PhysicalPath}. 오류: {ExMessage}", physicalPath, ex.Message);
        }

        // 폴더 존재 여부와 상관없이 매핑 진행 (추후 볼륨이 연결되면 동기화됨)
        var options = new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalPath),
            RequestPath = requestPath,
            OnPrepareResponse = ctx =>
            {
                var ext = Path.GetExtension(ctx.File.Name).ToLowerInvariant();

                // 🏢 문자열 딕셔너리가 아닌, '타입이 지정된 명품 헤더 창고'를 개방!
                var typedHeaders = ctx.Context.Response.GetTypedHeaders();

                // 🏎️ 패턴 매칭으로 캐시 제어 오브젝트를 다이렉트로 구워내기!
                typedHeaders.CacheControl = ext switch
                {
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".svg" or ".ico" or ".bmp"
                        => new CacheControlHeaderValue
                        {
                            Public = true,
                            MaxAge = TimeSpan.FromDays(365),
                            Extensions = { new NameValueHeaderValue("immutable") }
                        },

                    ".mp3" or ".aac" or ".m4a" or ".ogg" or ".wav"
                        => new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromDays(30) },

                    ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx"
                        => new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromDays(7) },

                    _ => new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromDays(1) }
                };

                ctx.Context.Response.Headers.AcceptRanges = "bytes";
            }
        };

        app.UseStaticFiles(options);

        logger.LogInformation("✅ 정적 파일 매핑 완료: {RequestPath} → {PhysicalPath}", requestPath, physicalPath);
        return app;
    }
}