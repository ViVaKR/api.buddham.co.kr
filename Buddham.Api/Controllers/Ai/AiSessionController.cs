using System.Text;
using System.Text.Json;
using Buddham.Core.Entities.Ai;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Core.Schemas;
using Buddham.Shared.Data;
using Buddham.Shared.Infrastructure;
using Buddham.Shared.Protos;
using Buddham.Shared.Services.OllamaService;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatMessage = Buddham.Core.Entities.Ai.ChatMessage;
using ProtoChatMessage = Buddham.Shared.Protos.ProtoChatMessage;

namespace Buddham.Api.Controllers.Ai;

[ApiController]
[Route("[controller]")]
public class AiSessionController(
    AiEngine.AiEngineClient aiClient,
    IOllamaService ollamaService,
    UserManager<ApplicationUser> userManager,
    AiDbContext context
) : BaseController
{
    // 🌟 모델에게 넘길 history 최대 메시지 수 (최근 N턴 = N/2턴)
    // history 키워드를 강하게 답습하는 경향이 있어, 컨텍스트를 보수적으로 제한.
    // 추후 데이터센터 배포 + 법등 페르소나 복귀 시 조정 검토.
    private const int MaxHistoryMessages = 12;

    // ───────────────────────────────────────────
    // ① 세션 목록 조회
    // ───────────────────────────────────────────
    [HttpGet("Sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var sessions = await context.ChatSessions
            .Where(x => x.UserId == UserId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.CreatedAt,
                x.UpdatedAt,
                // 마지막 메시지 미리보기
                Preview = x.Messages
                    .Where(m => m.Role == "assistant")
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content!.Substring(0, Math.Min(60, m.Content.Length)))
                    .FirstOrDefault() ?? ""
            })
            .ToListAsync(ct);

        return Ok(sessions);
    }

    // ───────────────────────────────────────────
    // ② 특정 세션 메시지 불러오기
    // ───────────────────────────────────────────
    [HttpGet("Sessions/{sessionId:long}")]
    public async Task<IActionResult> GetSession(long sessionId, CancellationToken ct)
    {
        var session = await context.ChatSessions
            .Where(x => x.Id == sessionId
                        && x.UserId == UserId)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.CreatedAt,
                Messages = x.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.Role,
                        m.Content,
                        m.ThinkingContent,
                        m.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (session is null) return NotFound();
        return Ok(session);
    }

    // ───────────────────────────────────────────
    // ③ 새 세션 생성
    // ───────────────────────────────────────────
    [HttpPost("Sessions")]
    public async Task<IActionResult> CreateSession(CancellationToken ct)
    {
        var session = new ChatSession
        {
            UserId = UserId,
            Title = "새 대화",
            UpdatedAt = null,
            DeletedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        context.ChatSessions.Add(session);
        await context.SaveChangesAsync(ct);

        return Ok(new { session.Id, session.Title, session.CreatedAt });
    }

    [Authorize]
    [HttpPost("AiChat")]
    public async Task AiChat([FromBody] ChatSchema.Request request, CancellationToken ct)
    {
        // 1. 브라우저(Angular)에게 "지금부터 글자 쪼개서 보낸다(SSE)!" 라고 선언하는 규격 헤더!
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // nginx 버퍼링 끄기 ✅

        // 세션 소유자 확인
        var session = await context.ChatSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId && x.UserId == UserId, ct);

        if (session is null)
        {
            Response.StatusCode = 404;
            return;
        }

        // 🌟 기존 히스토리 로드 — 최근 N개 메시지만
        var rawHistory = await context.ChatMessages
            .Where(x => x.ChatSessionId == request.SessionId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxHistoryMessages)
            .ToListAsync(ct);

        var history = rawHistory
            .OrderBy(x => x.CreatedAt)
            .Select(x => new OllamaChatMessage(x.Role!, x.Content!, null))
            .ToList();

        // 세션 전체 메시지 수 (첫 질문 여부 판단용)
        var totalMessageCount = await context.ChatMessages
            .CountAsync(x => x.ChatSessionId == request.SessionId, ct);

        // SSE 헤더
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        // 연결 유지용 첫 번째 flush
        await Response.WriteAsync(": keep-alive\n\n", ct);
        await Response.Body.FlushAsync(ct);

        // DB 작업 - 사용자 메시지 먼저 저장
        var userMessage = new ChatMessage
        {
            ChatSessionId = request.SessionId,
            Role = "user",
            Content = request.Question,
            CreatedAt = DateTime.UtcNow,
        };
        var user = userManager.Users.FirstOrDefault(x => x.Id == UserId);
        var embeddingSource = $"[{userMessage.Role}] {user?.Pseudonym ?? "법우"}님께서 수행: {userMessage.Content}";
        var vectorArray = await ollamaService.GetEmbeddingAsync(embeddingSource, ct);
        userMessage.Embedding = vectorArray;
        Console.WriteLine($"1. {userMessage}");
        context.ChatMessages.Add(userMessage);
        await context.SaveChangesAsync(ct);

        var fullContent = new StringBuilder();
        var thinkContent = new StringBuilder();
        var wasCancelledOrFailed = false;

        try
        {
            var grpcRequest = new AiRequest
            {
                Prompt = request.Question,
                UserId = UserId.ToString()
            };

            grpcRequest.History.AddRange(
                history.Select(h => new ProtoChatMessage
                {
                    Role = h.Role,
                    Content = h.Content
                })
            );

            await foreach (var grpcEvt in aiClient.AskStream(grpcRequest, cancellationToken: ct)
                               .ResponseStream.ReadAllAsync(ct))
            {
                await Response.WriteAsync($"event: {grpcEvt.Type}\n", ct);
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(grpcEvt.Data)}\n\n", ct);
                await Response.Body.FlushAsync(ct);

                switch (grpcEvt.Type)
                {
                    case "token": fullContent.Append(grpcEvt.Data); break;
                    case "thinking": thinkContent.Append(grpcEvt.Data); break;
                }
            }

            await Response.WriteAsync("event: done\n", ct);
            await Response.WriteAsync("data: \"[DONE]\"\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            wasCancelledOrFailed = true;
        }
        catch (RpcException)
        {
            wasCancelledOrFailed = true;
        }

        // 🌟 끊겼더라도 지금까지 받은 내용이 있으면 저장
        var saveCt = CancellationToken.None;

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = request.SessionId,
            Role = "assistant",
            Content = fullContent.ToString(),
            ThinkingContent = thinkContent.Length > 0
                ? thinkContent.ToString()
                : null,
            CreatedAt = DateTime.UtcNow,
        };

        if (wasCancelledOrFailed)
        {
            Console.WriteLine(
                $"[AiChat] 세션 {request.SessionId} 스트림 중단됨. 부분 응답 길이: {fullContent.Length}자");
        }

        // 🌟 핵심 변경: 임베딩 실패해도 메시지는 반드시 저장!
        try
        {
            if (!string.IsNullOrEmpty(assistantMessage.Content))
            {
                // 임베딩 시도 — 실패하면 null로 저장
                try
                {
                    var assistantEmbeddingSource =
                        $"[assistant] 법등스님 답변: {assistantMessage.Content}";
                    assistantMessage.Embedding =
                        await ollamaService.GetEmbeddingAsync(assistantEmbeddingSource, saveCt);
                }
                catch (Exception embEx)
                {
                    Console.WriteLine($"[AiChat] ⚠️ 임베딩 실패 (저장은 계속): {embEx.Message}");
                    assistantMessage.Embedding = null; // 임베딩 없이 저장
                }

                context.ChatMessages.Add(assistantMessage);
                Console.WriteLine($"[AiChat] ✅ assistant 메시지 Add 완료. 길이: {assistantMessage.Content.Length}자");
            }
            else if (wasCancelledOrFailed)
            {
                assistantMessage.Content = "[응답이 중단되었습니다]";
                context.ChatMessages.Add(assistantMessage);
            }
            else
            {
                Console.WriteLine($"[AiChat] ⚠️ fullContent가 비어있고 실패도 아님. 뭔가 이상함!");
            }

            if (totalMessageCount == 0)
            {
                session.Title = request.Question?.Length > 40 ? request.Question[..40] + "..." : request.Question;
                try
                {
                    session.Embedding = await ollamaService
                        .GetEmbeddingAsync($"대화 주제: {session.Title}", saveCt);
                }
                catch (Exception embEx)
                {
                    Console.WriteLine($"[AiChat] ⚠️ 세션 임베딩 실패: {embEx.Message}");
                }
            }

            session.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(saveCt);
            Console.WriteLine($"[AiChat] ✅ SaveChanges 완료! 세션: {request.SessionId}");
        }
        catch (Exception saveEx)
        {
            Console.WriteLine($"[AiChat] ❌ 저장 실패: {saveEx.Message}");
            Console.WriteLine($"[AiChat] ❌ StackTrace: {saveEx.StackTrace}");
        }
    }

    // ───────────────────────────────────────────
    // ⑤ 세션 제목 수정
    // ───────────────────────────────────────────
    [HttpPatch("Sessions/{sessionId:long}/Title")]
    public async Task<IActionResult> UpdateTitle(
        long sessionId,
        [FromBody] string title,
        CancellationToken ct)
    {
        var session = await context.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId
                                      && x.UserId == UserId, ct);

        if (session is null) return NotFound();

        session.Title = title.Length > 200 ? title[..200] : title;
        await context.SaveChangesAsync(ct);

        return Ok(new { session.Id, session.Title });
    }

    // ───────────────────────────────────────────
    // ⑥ 세션 삭제 (소프트 삭제)
    // ───────────────────────────────────────────
    [HttpDelete("Sessions/{sessionId:long}")]
    public async Task<IActionResult> DeleteSession(long sessionId, CancellationToken ct)
    {
        var session = await context.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == UserId, ct);

        if (session is null) return NotFound();

        session.DeletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return NoContent();
    }
}
