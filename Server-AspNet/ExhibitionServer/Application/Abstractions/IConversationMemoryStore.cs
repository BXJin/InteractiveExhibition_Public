using Exhibition.Shared.Ai;

namespace ExhibitionServer.Application.Abstractions;

public interface IConversationMemoryStore
{
    IReadOnlyList<AiConversationTurnDto> GetRecentTurns(string? conversationId);

    void AppendExchange(string? conversationId, string userMessage, string assistantReply);
}
