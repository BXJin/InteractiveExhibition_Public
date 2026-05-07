using System.Diagnostics;
using Exhibition.Shared.Ai;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Application;

public sealed class AiChatService : IAiChatService
{
    private readonly IAiChatProviderResolver _providerResolver;
    private readonly IRetrievedContextRanker _contextRanker;
    private readonly GatewayOptions _gatewayOptions;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        IAiChatProviderResolver providerResolver,
        IRetrievedContextRanker contextRanker,
        IOptions<GatewayOptions> gatewayOptions,
        ILogger<AiChatService> logger)
    {
        _providerResolver = providerResolver;
        _contextRanker = contextRanker;
        _gatewayOptions = gatewayOptions.Value;
        _logger = logger;
    }

    public async Task<AiChatResponse> CreateReplyAsync(
        AiChatRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var message = request.Message.Trim();

        if (message.Length == 0)
        {
            return CreateFailure("EMPTY_MESSAGE", "메시지를 입력해 주세요.", "gateway", null, stopwatch.ElapsedMilliseconds);
        }

        if (message.Length > _gatewayOptions.MaxInputChars)
        {
            return CreateFailure(
                "MESSAGE_TOO_LONG",
                $"메시지는 {_gatewayOptions.MaxInputChars}자 이하로 입력해 주세요.",
                "gateway",
                null,
                stopwatch.ElapsedMilliseconds);
        }

        var provider = _providerResolver.Resolve();

        if (!provider.IsConfigured)
        {
            return CreateFailure(
                provider.MissingConfigurationErrorCode,
                $"{provider.ProviderName} provider가 설정되어 있지 않습니다.",
                provider.ProviderName,
                provider.Model,
                stopwatch.ElapsedMilliseconds);
        }

        try
        {
            var rankedContext = await _contextRanker.RankAsync(
                message,
                request.RetrievedContext,
                cancellationToken);

            var aiResponse = await provider.CreateReplyAsync(
                request with
                {
                    Message = message,
                    RetrievedContext = rankedContext
                },
                cancellationToken);

            return aiResponse with
            {
                Success = true,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                Provider = provider.ProviderName,
                Model = provider.Model
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateFailure(
                "AI_PROVIDER_TIMEOUT",
                "AI 응답 시간이 초과되었습니다.",
                provider.ProviderName,
                provider.Model,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI provider request failed. Provider={Provider}", provider.ProviderName);
            return CreateFailure(
                "AI_PROVIDER_REQUEST_FAILED",
                "AI 응답을 가져오지 못했습니다.",
                provider.ProviderName,
                provider.Model,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static AiChatResponse CreateFailure(
        string errorCode,
        string reply,
        string provider,
        string? model,
        long latencyMs)
    {
        return new AiChatResponse
        {
            Reply = reply,
            Success = false,
            LatencyMs = latencyMs,
            Provider = provider,
            Model = model,
            ErrorCode = errorCode
        };
    }
}
