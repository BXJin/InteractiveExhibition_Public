using ExhibitionServer.Application.Chat;
using Exhibition.Shared.Ai;

namespace ExhibitionServer.Application.Abstractions;

public interface IChatGuideService
{
    Task<ChatResponse> ProcessAsync(ChatRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AiChatStreamEvent> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
