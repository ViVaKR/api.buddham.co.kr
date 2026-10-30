using System.Text.Json;
// using Buddham.Shared.Protos.V1;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Ai;

[ApiController]
[Route("[controller]")]
public class AiController(AiEngine.AiEngineClient aiClient, ILogger<AiController> logger)
    : ControllerBase
{
    // 팩토리가 주입해 줄 gRPC 클라이언트 소총!

    // 🌟 [최고 존엄] 앵귤러가 SSE 빨대를 꼽고 "색즉시공" 화두를 던질 고속도로!
    [HttpGet("ask-stream")]
    public async Task AskStream([FromQuery] string prompt, [FromQuery] string userId = "viva-master")
    {
        // 1. 브라우저(Angular)에게 "지금부터 글자 쪼개서 보낸다(SSE)!" 라고 선언하는 규격 헤더!
        // Response.Headers["Content-Type"] = "text/event-stream";
        // Response.Headers["Cache-Control"] = "no-cache";
        // Response.Headers["X-Accel-Buffering"] = "no"; // nginx 버퍼링 끄기 ✅
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        // CORS 헤더 안전하게 유지
        try
        {
            // 2. 내부 네트워크를 통해 AI 워커(gRPC 서버)에게 스트리밍 요청 쾌속 발사!
            using var call = aiClient.AskStream(new AiRequest
            {
                Prompt = prompt,
                UserId = userId
            });

            // 3. AI 워커가 gRPC 파이프로 쏴주는 이진 바이트 글자 조각들을 실시간으로 수신!
            await foreach (var token in call.ResponseStream.ReadAllAsync())
            {
                var jsonToken = JsonSerializer.Serialize(token);
                // SSE 형식
                await Response.WriteAsync($"data: {jsonToken}\n\n");
                await Response.Body.FlushAsync(); // 버퍼에 가두지 말고 실시간으로 즉시 밀어내기!
            }

            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "🚨 AI 워커 gRPC 통신 중 요괴 출현!");
            await Response.WriteAsync($"data: [AI 엔진 통신 오류 발생: {ex.Status.Detail}]\n\n");
            await Response.Body.FlushAsync();
        }
        finally
        {
            logger.LogInformation("🏁 API 본진 - 스트리밍 대장정 종료.");
        }
    }
}
