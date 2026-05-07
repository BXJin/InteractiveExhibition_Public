using Exhibition.Shared.Ai;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Application;

public sealed class AiChatStreamService : IAiChatStreamService
{
    private readonly IAiChatProviderResolver _providerResolver;
    private readonly IRetrievedContextRanker _contextRanker;
    private readonly GatewayOptions _gatewayOptions;
    private readonly ILogger<AiChatStreamService> _logger;

    public AiChatStreamService(
        IAiChatProviderResolver providerResolver,
        IRetrievedContextRanker contextRanker,
        IOptions<GatewayOptions> gatewayOptions,
        ILogger<AiChatStreamService> logger)
    {
        _providerResolver = providerResolver;
        _contextRanker = contextRanker;
        _gatewayOptions = gatewayOptions.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<AiChatStreamEvent> StreamReplyAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            yield return validationError;
            yield break;
        }

        var provider = _providerResolver.Resolve();
        if (!provider.IsConfigured)
        {
            yield return AiChatStreamEvent.Error(
                provider.MissingConfigurationErrorCode,
                $"{provider.ProviderName} provider가 구성되지 않았습니다.");
            yield break;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var message = request.Message.Trim();
        var rankedContext = await _contextRanker.RankAsync(
            message,
            request.RetrievedContext,
            cancellationToken);
        var rankedRequest = request with
        {
            Message = message,
            RetrievedContext = rankedContext
        };

        await foreach (var streamEvent in provider.StreamReplyAsync(rankedRequest, cancellationToken)
            .ConfigureAwait(false))
        {
            if (streamEvent.EventType == AiChatStreamEventTypes.Complete &&
                streamEvent.CompleteResponse is not null)
            {
                yield return AiChatStreamEvent.Complete(streamEvent.CompleteResponse with
                {
                    Success = true,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    Provider = provider.ProviderName,
                    Model = provider.Model
                });
                yield break;
            }

            yield return streamEvent;
        }
    }

    private AiChatStreamEvent? Validate(AiChatRequest request)
    {
        var message = request.Message.Trim();

        if (message.Length == 0)
        {
            return AiChatStreamEvent.Error("EMPTY_MESSAGE", "메시지를 입력해 주세요.");
        }

        if (message.Length > _gatewayOptions.MaxInputChars)
        {
            return AiChatStreamEvent.Error(
                "MESSAGE_TOO_LONG",
                $"메시지는 {_gatewayOptions.MaxInputChars}자 이하로 입력해 주세요.");
        }

        return null;
    }
}
