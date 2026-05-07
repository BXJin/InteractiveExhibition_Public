using Exhibition.Shared.Ai;

namespace ExhibitionAiGateway.Application.Abstractions;

public interface IAiChatStreamService
{
    IAsyncEnumerable<AiChatStreamEvent> StreamReplyAsync(
        AiChatRequest request,
        CancellationToken cancellationToken);
}
