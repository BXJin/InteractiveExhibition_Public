using Exhibition.Shared.Ai;

namespace ExhibitionAiGateway.Application.Abstractions;

public interface IAiChatService
{
    Task<AiChatResponse> CreateReplyAsync(AiChatRequest request, CancellationToken cancellationToken);
}
