using Exhibition.Shared.Ai;

namespace ExhibitionAiGateway.Application.Abstractions;

public interface IAiChatProvider
{
    string ProviderName { get; }

    string Model { get; }

    bool IsConfigured { get; }

    string MissingConfigurationErrorCode { get; }

    Task<AiChatResponse> CreateReplyAsync(AiChatRequest request, CancellationToken cancellationToken);

    IAsyncEnumerable<AiChatStreamEvent> StreamReplyAsync(
        AiChatRequest request,
        CancellationToken cancellationToken);
}
