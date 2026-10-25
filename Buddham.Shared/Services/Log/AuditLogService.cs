using System.Text.Json;
using Buddham.Core.Common.Options;
using Buddham.Core.Entities.Log;
using Buddham.Core.Interfaces.Log;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Shared.Data;

namespace Buddham.Shared.Services.Log;

public class AuditLogService(LogDbContext logDb, IOllamaService ollamaService): IAuditLogService
{
     public async Task LogAsync(
        Guid userId,
        string userEmail,
        string actionType,
        string category,
        string? message,
        object? oldValue,
        object? newValue,
        string? ipAddress,
        string? userAgent = null,
        string? requestPath = null,
        string? httpMethod = null,
        bool isSuccess = true)
    {
        var log = new AuditLog
        {
            UserId = userId,
            UserEmail = userEmail,
            ActionType = actionType,
            Category = category,
            IsSuccess = isSuccess,
            Message = message,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestPath = requestPath,
            HttpMethod = httpMethod,
            CreatedAt = DateTime.UtcNow,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, AppJsonOptions.Default),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue, AppJsonOptions.Default),
        };

        var embeddingSource = $"[{category}] {userEmail}님이 {actionType} 수행: {message ?? "작업 완료"}";
        try
        {
            var vectorArray = await ollamaService.GetEmbeddingAsync(embeddingSource, CancellationToken.None);
            // log.Embedding = new Pgvector.Vector(vectorArray);
            log.Embedding = vectorArray;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLog-AI] ❌ 벡터 생성 실패: {ex.Message}");
        }

        await logDb.AuditLogs.AddAsync(log);
        await logDb.SaveChangesAsync(); 
    }

    public Task LogFailureAsync(
        Guid userId, string userEmail, string actionType, string category,
        string message, string? ipAddress = null, string? userAgent = null,
        string? requestPath = null, string? httpMethod = null)
        => LogAsync(userId, userEmail, actionType, category, // ✅ LogAsync에 위임
            message: message,
            oldValue: null,
            newValue: null,
            ipAddress: ipAddress,
            userAgent: userAgent,
            requestPath: requestPath,
            httpMethod: httpMethod,
            isSuccess: false);
}
