using Buddham.Core.Common;
using Buddham.Core.Common.Results;
using Buddham.Shared.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Buddham.Shared.Middlewares;

/// <summary>
/// 전역 오류 처리
/// IExceptionHandler 는 보조안전망
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        // GlobalResponseMiddleware 가 이미 처리 중인 경우 중복 응답 방지
        if (httpContext.Response.HasStarted) return true;

        var (code, message) = exception switch
        {
            BuddhamException bex => (bex.Code, bex.Message),
            // 1. 토큰 디코딩 실패 (Base64 형식 오류)
            FormatException => (RsCode.BadRequest, "잘못된 토큰 형식입니다. 잠시 후 다시 요청부탁합니다."),

            // 2. Exception 중 메시지에 "Ivalid token" 이 포함된 경우 (패턴 매칭)
            not null when exception.Message.Contains("Invalid token")
                => (RsCode.BadRequest, "토큰이 만료되었거나 유효하지 않습니다."),

            UnauthorizedAccessException => (RsCode.Unauthorized, "인증이 유효하지 않습니다."),
            KeyNotFoundException => (RsCode.NotFound, "요청하신 데이터를 찾을 수 없습니다."),
            InvalidOperationException => (RsCode.Conflict, "현재 상태에서는 처리할 수 없습니다."),
            FileNotFoundException => (RsCode.NotFound, "파일을 찾을 수 없습니다."),
            _ => (RsCode.Error, "서버 내부 오류가 발생했습니다.")
        };

        var devDetail = env.IsDevelopment() ? $" [{exception?.Message}]" : string.Empty;
        logger.LogError(exception, "[{TraceId}] 예외 발생: {Message}",
            httpContext.TraceIdentifier, message);

        httpContext.Response.StatusCode = (int)code;
        httpContext.Response.ContentType = ContentTypes.Json; //  "application/json";

        var result = Result<object>.Failure(code, message + devDetail)
            .WithTraceId(httpContext.TraceIdentifier);

        await httpContext.Response.WriteAsJsonAsync(result, ct);

        return true;
    }
}
