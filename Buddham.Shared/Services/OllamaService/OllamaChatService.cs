using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Buddham.Core.Common.Options;
using Microsoft.Extensions.Options;

namespace Buddham.Shared.Services.OllamaService;

public class OllamaChatService(HttpClient httpClient, IOptions<OllamaOptions> options)
{
    private readonly OllamaOptions _options = options.Value;

    private static readonly JsonSerializerOptions OllamaJsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    // ─────────────────────────────────────────────
    // api/generate 용 (구형 — 하위 호환 유지)
    // ─────────────────────────────────────────────
    public async IAsyncEnumerable<string> GetAiResponseStreamAsync(
        string question,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        const string systemPrompt = "너는 'buddham.co.kr' 프로젝트를 리딩하는 수석 풀스택 개발자야...";
        var fullPrompt = $"{systemPrompt}\n\n질문: {question}\n답변:";

        var requestPayload = new
        {
            model = _options.Model,
            prompt = fullPrompt,
            stream = true,
            options = new { temperature = 0.2, num_ctx = 32768, top_p = 0.90 }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "api/generate")
        {
            Content = JsonContent.Create(requestPayload)
        };

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (ct.IsCancellationRequested) break;
            if (string.IsNullOrEmpty(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaGenerateResponse>(line, OllamaJsonOpts);
            if (chunk is null) continue;

            if (!string.IsNullOrEmpty(chunk.Response))
                yield return chunk.Response;

            if (chunk.Done) break;
        }
    }

    // ─────────────────────────────────────────────
    // api/chat 단순 string 스트림 (구형 — 하위 호환 유지)
    // ─────────────────────────────────────────────
    public async IAsyncEnumerable<string> GetResponseStreamAsync(
        string question,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        const string systemPrompt = "당신은 지혜롭고 자애로운 불교 승려 '법등'입니다. 항상 격식 있는 하십시오체를 사용하고 깊이 있는 통찰을 제공하세요.";
        var fullPrompt = $"{systemPrompt}\n\n질문: {question}\n답변:";

        var requestPayload = new
        {
            model = _options.Model,
            prompt = fullPrompt,
            stream = true,
            options = new { temperature = 0.2, num_ctx = 32768, top_p = 0.90 }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(requestPayload)
        };

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (ct.IsCancellationRequested) break;
            if (string.IsNullOrEmpty(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, OllamaJsonOpts);
            if (chunk is null) continue;

            var content = chunk.Message?.Content;
            if (!string.IsNullOrEmpty(content))
                yield return content;

            if (chunk.Done) break;
        }
    }

    // ─────────────────────────────────────────────
    // ✅ 핵심: SSE 스트리밍 (히스토리 + thinking 파싱)
    // ─────────────────────────────────────────────
    public async IAsyncEnumerable<SseEvent> GetAiChatResponseStreamAsync(
        string question,
        IReadOnlyList<OllamaChatMessage>? history = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = new List<object>(history ?? []) { new { role = "user", content = question } };

        var requestPayload = new
        {
            model = _options.Model,
            messages,
            stream = true,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(requestPayload)
        };

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var thinkBuffer = new StringBuilder();
        var inThink = false;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (ct.IsCancellationRequested) break;
            if (string.IsNullOrEmpty(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, OllamaJsonOpts);
            if (chunk is null) continue;

            // ✅ thinking 필드 직접 활용 (마지막 done 청크에만 포함됨)
            if (chunk.Done && !string.IsNullOrEmpty(chunk.Message?.Thinking))
            {
                yield return new SseEvent("thinking", chunk.Message.Thinking);
            }

            var content = chunk.Message?.Content;

            if (!string.IsNullOrEmpty(content))
            {
                thinkBuffer.Append(content);
                var buffered = thinkBuffer.ToString();

                if (!inThink && buffered.Contains("<think>"))
                {
                    var beforeThink = buffered[..buffered.IndexOf("<think>", StringComparison.Ordinal)];
                    if (!string.IsNullOrEmpty(beforeThink))
                        yield return new SseEvent("token", beforeThink);

                    inThink = true;
                    thinkBuffer.Clear();
                    thinkBuffer.Append(buffered[(buffered.IndexOf("<think>", StringComparison.Ordinal) + 7)..]);
                    buffered = thinkBuffer.ToString();
                }

                switch (inThink)
                {
                    case true when buffered.Contains("</think>"):
                        {
                            var thoughtContent = buffered[..buffered.IndexOf("</think>", StringComparison.Ordinal)];
                            var afterThink = buffered[(buffered.IndexOf("</think>", StringComparison.Ordinal) + 8)..];

                            // thinking 필드가 없을 때만 content 파싱 결과 사용
                            if (string.IsNullOrEmpty(chunk.Message?.Thinking))
                                yield return new SseEvent("thinking", thoughtContent);

                            inThink = false;
                            thinkBuffer.Clear();

                            if (!string.IsNullOrEmpty(afterThink))
                            {
                                yield return new SseEvent("token", afterThink);
                                thinkBuffer.Clear();
                            }

                            break;
                        }
                    case false when thinkBuffer.Length > 0:
                        yield return new SseEvent("token", thinkBuffer.ToString());
                        thinkBuffer.Clear();
                        break;
                }
            }

            if (!chunk.Done) continue;
            yield return new SseEvent("done", new { reason = chunk.DoneReason ?? "stop" });
            break;
        }
    }
}

// ─────────────────────────────────────────────
// Records
// ─────────────────────────────────────────────
public record OllamaChatMessage(
    string Role,
    string Content,
    string? Thinking
);

public record OllamaChatResponse(
    OllamaChatMessage? Message,
    bool Done,
    string? DoneReason
);

public record OllamaGenerateResponse(
    string Response,
    bool Done
);

public record SseEvent(string Type, object? Data = null);
