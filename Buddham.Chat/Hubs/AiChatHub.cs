using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using Buddham.Core.Entities.Ai;
using Buddham.Shared.Data;
using Buddham.Shared.Protos.V1;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Chat.Hubs;

public class AiChatHub(AiDbContext aiContext, AiEngine.AiEngineClient aiClient)
    : Hub
{
    private string GetUserIdSafe() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                      ?? Context.User?.FindFirst("sub")?.Value
                                      ?? "UNKNOWN_USER_ID";

    public async IAsyncEnumerable<string> AskAiStream(long sessionId, string question, [EnumeratorCancellation] CancellationToken ct)
    {
        var userId = GetUserIdSafe();

        // 1. 세션 조회
        var session = await aiContext.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null) yield break;

        // 2. 유저 메시지 저장
        try
        {
            aiContext.ChatMessages.Add(new ChatMessage { ChatSessionId = sessionId, Role = "user", Content = question, CreatedAt = DateTime.UtcNow });
            await aiContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AiChatHub] DB 저장 에러: {ex.Message}");
            yield break;
        }

        // 3. gRPC 용 히스토리 가져오기
        var rawHistory = await aiContext.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(12)
            .ToListAsync(ct);
        var protoHistory = rawHistory
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ProtoChatMessage { Role = x.Role ?? string.Empty, Content = x.Content ?? string.Empty });
        var grpcRequest = new AiRequest { Prompt = question, UserId = userId };
        grpcRequest.History.AddRange(protoHistory);
        // 4. gRPC 스트리밍 시작
        var fullContent = new StringBuilder();
        var thinkingContent = new StringBuilder();

        IAsyncEnumerator<AiResponse>? enumerator;
        try
        {
            enumerator = aiClient.AskStream(grpcRequest, cancellationToken: ct)
                .ResponseStream
                .ReadAllAsync(ct)
                .GetAsyncEnumerator(ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AiChatHub] gRPC 스트림 시작 실패: {ex.Message}");
            yield break;
        }

        while (true)
        {
            AiResponse? grpcEvt;
            try
            {
                if (!await enumerator.MoveNextAsync()) break;
                grpcEvt = enumerator.Current;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiChatHub] 스트림 토큰 수신 에러: {ex.Message}");
                break;
            }

            switch (grpcEvt.Type)
            {
                case "token" when !string.IsNullOrEmpty(grpcEvt.Data):
                    fullContent.Append(grpcEvt.Data);
                    yield return grpcEvt.Data; // 클라이언트에 스트리밍 전송
                    break;
                case "thinking" when !string.IsNullOrEmpty(grpcEvt.Data):
                    thinkingContent.Append(grpcEvt.Data);
                    break;
            }
        }

        // 5. 어시스턴트 답변 최종 DB 저장
        var assistantContent = fullContent.ToString();
        if (!string.IsNullOrEmpty(assistantContent))
        {
            try
            {
                aiContext.ChatMessages.Add(new ChatMessage
                {
                    ChatSessionId = sessionId,
                    Role = "assistant",
                    Content = assistantContent,
                    ThinkingContent = thinkingContent.Length > 0 ? thinkingContent.ToString() : null,
                    CreatedAt = DateTime.UtcNow
                });

                if (string.IsNullOrEmpty(session.Title))
                {
                    session.Title = question.Length > 30 ? question[..30] + "..." : question;
                }

                await aiContext.SaveChangesAsync(ct);
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"[AiChatHub] AI 답변 DB 저장 에러: {dbEx.Message}");
            }
        }
    }
}
