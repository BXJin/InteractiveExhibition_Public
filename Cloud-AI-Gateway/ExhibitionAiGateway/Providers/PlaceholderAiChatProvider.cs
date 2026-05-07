using Exhibition.Shared.Ai;
using ExhibitionAiGateway.Application.Abstractions;

namespace ExhibitionAiGateway.Providers;

public sealed class PlaceholderAiChatProvider : IAiChatProvider
{
    private readonly string _providerName;
    private readonly string _model;

    public PlaceholderAiChatProvider(string providerName, string model)
    {
        _providerName = providerName;
        _model = model;
    }

    public string ProviderName => _providerName;

    public string Model => _model;

    public bool IsConfigured => false;

    public string MissingConfigurationErrorCode => $"{_providerName.ToUpperInvariant()}_PROVIDER_NOT_IMPLEMENTED";

    public Task<AiChatResponse> CreateReplyAsync(
        AiChatRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            $"{_providerName} provider is registered as a placeholder and is not implemented yet.");
    }

    public async IAsyncEnumerable<AiChatStreamEvent> StreamReplyAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield return AiChatStreamEvent.Error(
            MissingConfigurationErrorCode,
            $"{_providerName} streaming provider is not implemented yet.");
    }
}
