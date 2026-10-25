using System.Reflection;
using Buddham.Shared.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Buddham.Shared.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseCommonHealthChecks(this WebApplication app)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var serviceName = entryAssembly?.GetName().Name ?? "Unknown.Buddham.Service";
        var startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

        app.MapHealthChecks(ApiRoutes.Statics.Healthz, new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var isHealthy = report.Status == HealthStatus.Healthy;

                var response = new
                {
                    status = isHealthy ? "healthy" : "unhealthy",
                    service = serviceName,
                    timestamp = DateTime.UtcNow,
                    environment = app.Environment.EnvironmentName,
                    uptime = DateTime.UtcNow - startTime,
                    details = report.Entries.Select(e => new { key = e.Key, status = e.Value.Status.ToString() })
                };

                context.Response.StatusCode = isHealthy ? 200 : 503;
                await context.Response.WriteAsJsonAsync(response);
            }
        });

        return app;
    }
}