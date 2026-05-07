using Exhibition.Shared.Ai;
using ExhibitionServer.Application.Chat;
using ExhibitionServer.Application.Knowledge;

namespace ExhibitionServer.Application.Abstractions;

public interface IAiGatewayClient
{
    Task<AiChatResponse?> CreateReplyAsync(
        ChatRequest request,
        IReadOnlyList<ExhibitionKnowledgeDocument> retrievedContext,
        IReadOnlyList<AiConversationTurnDto> conversationHistory,
        CancellationToken cancellationToken);

    IAsyncEnumerable<AiChatStreamEvent> StreamReplyAsync(
        ChatRequest request,
        IReadOnlyList<ExhibitionKnowledgeDocument> retrievedContext,
        IReadOnlyList<AiConversationTurnDto> conversationHistory,
        CancellationToken cancellationToken);
}
