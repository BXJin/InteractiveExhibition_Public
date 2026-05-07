using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Application;

public sealed class AiChatProviderResolver : IAiChatProviderResolver
{
    private readonly IReadOnlyDictionary<string, IAiChatProvider> _providers;
    private readonly AiProviderOptions _options;

    public AiChatProviderResolver(
        IEnumerable<IAiChatProvider> providers,
        IOptions<AiProviderOptions> options)
    {
        _providers = providers.ToDictionary(
            provider => provider.ProviderName,
            StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
    }

    public IAiChatProvider Resolve()
    {
        if (_providers.TryGetValue(_options.Provider, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException(
            $"Unsupported AI provider '{_options.Provider}'. Supported providers: {string.Join(", ", _providers.Keys)}");
    }
}
