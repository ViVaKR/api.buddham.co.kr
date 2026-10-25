using Buddham.Shared.Protos;
using Buddham.Shared.Services.OllamaService;
using Grpc.Core;

namespace Buddham.Ai.Services;

public class AiStreamService(OllamaChatService chatService) : AiEngine.AiEngineBase
{
    public override async Task AskStream(
        AiRequest request,
        IServerStreamWriter<AiResponse> responseStream,
        ServerCallContext context)
    {
        var history = request.History.Select(h => new OllamaChatMessage(h.Role, h.Content, null)).ToList();
        await foreach (var evt in chatService.GetAiChatResponseStreamAsync(
                           request.Prompt,
                           history,
                           context.CancellationToken))
        {
            if (context.CancellationToken.IsCancellationRequested) break;

            await responseStream.WriteAsync(new AiResponse
            {
                Type = evt.Type,
                Data = evt.Data?.ToString() ?? string.Empty
            });
        }
    }
}
