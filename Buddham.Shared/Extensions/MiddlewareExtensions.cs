using System.Security.Claims;
using Buddham.Core.Interfaces.Log;
using Buddham.Shared.Constants;
using Buddham.Shared.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseBuddhamMiddlewares(this IApplicationBuilder app)
    {
        app.UseForwardedHeaders(); // 0 순위
        app.UseMiddleware<GlobalResponseMiddleware>(); // 1 순위
        app.UseExceptionHandler(); // 2 순위
        app.UseDefaultFiles();

        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        if (!env.IsDevelopment())
        {
            foreach (var item in StaticFolder.GetStaticWebFolders())
                app.MapCustomStaticFiles(item.Key, item.Value);
        }

        app.UseRouting();
        app.UseCors(AppInfrastructure.CorsPolicy);
        app.UseRateLimiter(); // 인증 트래픽 제한
        app.UseAuthentication(); // 1. 신원확인 (너 누구냐?)
        app.UseAuthorization(); // 2. 권한 확인 (너 자격 있냐?)

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 5. 엔드포인트
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        app.UseAuditLog();
        return app;
    }

    /// <summary>
    /// 로그 서비스
    /// </summary>
    public class AuditLogMiddleware(RequestDelegate next)
    {
        private static readonly string[] ExcludePaths =
        [
            "/healthz",
            "/health",
            "/favicon.ico",
            "/swagger",
            "/metrics",
        ];

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLog, ILogger<AuditLogMiddleware> logger)
        {
            var publicIp = context.GetClientIp();
            var path = context.Request.Path.Value ?? "";
            if (ExcludePaths.Any(p => path.StartsWith(p)))
            {
                await next(context);
                return;
            }

            await next(context);

            var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userEmail = context.User.FindFirstValue(JwtRegisteredClaimNames.Email)
                            ?? context.User.FindFirstValue(ClaimTypes.Email);
            if (userId is null || userEmail is null) return;

            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var requestPath = context.Request.Path;
            var userAgent = context.Request.Headers.UserAgent.ToString();

            // ✅ 새 스코프로 안전하게 Fire-and-forget
            var scopeFactory = context.RequestServices
                .GetRequiredService<IServiceScopeFactory>();

            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var auditLogService = scope.ServiceProvider
                        .GetRequiredService<IAuditLogService>();

                    var isSuccess = statusCode < 400;
                    var actionType = GetActionType(method, requestPath);
                    var category = GetCategory(requestPath);
                    // TODO: 필요한 경우 DB나 메모리에서 OldValue/NewValue를 가져오는 로직 추가
                    string? oldValueJson = null;
                    string? newValueJson = null;

                    // 로그 메시지 생성 (Vector 임베딩의 재료)
                    var logMessage = isSuccess ? $"{actionType} 성공" : $"실패: {statusCode}";

                    await auditLogService.LogAsync(
                        Guid.Parse(userId),
                        userEmail,
                        actionType,
                        category,
                        logMessage,
                        ipAddress: publicIp,
                        userAgent: userAgent,
                        requestPath: requestPath, // 위험군
                        httpMethod: method, // 위험군
                        oldValue: oldValueJson, // JSON 데이터 추가!
                        newValue: newValueJson,
                        isSuccess: isSuccess
                    );
                    logger.LogInformation("[AuditLog] 저장 완료 → {ActionType} / {Category}", actionType, category);
                }
                catch (Exception ex) // 👈 핵심! 예외 내용 보기
                {
                    logger.LogError(ex, "[AuditLog] ❌ 저장 실패 → {ErrorType}: {ErrorMessage}", ex.GetType().Name, ex.Message);
                }
            });
        }

        // 경로에서 ActionType 추출
        private static string GetActionType(string method, PathString path)
        {
            var p = path.Value?.ToLower() ?? "";
            return (method, p) switch
            {
                ("POST", var x) when x.Contains("sign-in") => AuditActions.SignIn,
                ("POST", var x) when x.Contains("sign-out") => AuditActions.SignOut,
                ("POST", var x) when x.Contains("sign-up") => AuditActions.SignUp,
                ("PUT", var x) when x.Contains("change-password") => AuditActions.ChangePassword,
                ("DELETE", _) => AuditActions.DeleteUser,
                ("PUT", _) => AuditActions.UpdateUser,
                _ => $"{method}:{path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Root"}"
            };
        }

        private static string GetCategory(PathString path)
        {

            var p = path.Value?.ToLower() ?? "";
            if (p.Contains("auth")) return AuditCategories.Auth;
            if (p.Contains("account")) return AuditCategories.Account;
            return p.Contains("email") ? AuditCategories.Email : AuditCategories.System;
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max];

    }

    public static IApplicationBuilder UseAuditLog(this IApplicationBuilder app) => app.UseMiddleware<AuditLogMiddleware>();
}
