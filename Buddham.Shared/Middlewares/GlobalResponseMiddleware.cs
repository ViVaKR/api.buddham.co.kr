using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Buddham.Shared.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Middlewares;

public class BuddhamException(RsCode code, string message) : Exception(message)
{
    public RsCode Code { get; } = code;
}

public class GlobalResponseMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env,
    ILogger<GlobalResponseMiddleware> logger)
{
    private readonly Lazy<byte[]> _defaultAvatar = new(
    () =>
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        return File.ReadAllBytes(Path.Combine(webRoot, "defaults", "buddha.png"));
    },
    LazyThreadSafetyMode.PublicationOnly);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
            if (context.Response.HasStarted) return;

            // 아바타 404 전용
            if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
                IsReadMethod(context.Request.Method) &&
                context.Request.Path.StartsWithSegments(ApiRoutes.Files.Avatars) &&
                await TryWriteDefaultAvatarAsync(context))
            {
                return;
            }
            // if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
            //     IsReadMethod(context.Request.Method) &&
            //     context.Request.Path.StartsWithSegments(ApiRoutes.Files.Avatars)
            //    )
            // {
            //     await HandleDefaultAvatarAsync(context);
            //     return;
            // }

            // 상태코드 -> Result<T> 표준화
            if (context.Response.StatusCode is
                    StatusCodes.Status400BadRequest or
                    StatusCodes.Status401Unauthorized or
                    StatusCodes.Status403Forbidden or
                    StatusCodes.Status404NotFound &&
                context.Response.ContentType?.Contains(ContentTypes.Json) != true)
            {
                await HandleStandardErrorAsync(context);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static bool IsReadMethod(string method) =>
    HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

    // private async Task HandleDefaultAvatarAsync(HttpContext context)
    // {
    //     context.Response.StatusCode = StatusCodes.Status200OK;
    //     context.Response.ContentType = "image/png";
    //     var path = Path.Combine(env.ContentRootPath, "Avatars", "buddha.png");
    //     await context.Response.SendFileAsync(path);
    // }


    private async Task<bool> TryWriteDefaultAvatarAsync(HttpContext context)
    {
        byte[] bytes;
        try
        {
            bytes = _defaultAvatar.Value;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 기본 이미지가 없어도 500 대신 기존 404 표준 처리로 넘김
            logger.LogWarning(ex, "기본 아바타 파일을 읽을 수 없습니다.");
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "image/png";
        context.Response.ContentLength = bytes.Length;
        // 나중에 실제 아바타를 업로드해도 기본 이미지가 캐시되어 남지 않도록
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Default-Avatar"] = "1"; // 디버깅/클라이언트 구분용

        if (!HttpMethods.IsHead(context.Request.Method))
            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);

        return true;
    }

    private static async Task HandleStandardErrorAsync(HttpContext context)
    {
        var code = (RsCode)context.Response.StatusCode;
        var message = code switch
        {
            RsCode.BadRequest => "잘못된 요청입니다. 입력값을 확인해주세요.",
            RsCode.Unauthorized => "권한이 없거나 인증이 만료되었습니다.",
            RsCode.Forbidden => "접근할 권한이 없습니다.",
            RsCode.NotFound => "요청하신 정보를 찾을 수 없습니다.",
            _ => "예기치 않은 오류가 발생했습니다."
        };

        // Traceid 주입
        var traceId = context.TraceIdentifier;
        var result = Result<object>.Failure(code, message).WithTraceId(traceId);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(result);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (code, message) = ex switch
        {
            BuddhamException kex => (kex.Code, kex.Message),
            UnauthorizedAccessException => (RsCode.Unauthorized, "인증이 유효하지 않습니다."),
            KeyNotFoundException => (RsCode.NotFound, "요청하신 데이터를 찾을 수 없습니다."),
            InvalidOperationException => (RsCode.Conflict, "현재 상태에서는 처리할 수 없는 요청입니다."),
            _ => (RsCode.Error, "서버 내부 오류가 발생했습니다."),
        };
        var devDetail = env.IsDevelopment() ? $" [{ex.Message}]" : string.Empty;
        logger.LogError(ex, "[{TraceId}] 에러발생: {Message}", context.TraceIdentifier, message);
        context.Response.ContentType = ContentTypes.Json; //  "application/json";
        context.Response.StatusCode = (int)code;

        var result = Result<object>.Failure(code, message + devDetail).WithTraceId(context.TraceIdentifier);
        await context.Response.WriteAsJsonAsync(result);
    }
}
