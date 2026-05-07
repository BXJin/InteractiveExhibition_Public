using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Exhibition.Shared.Ai;
using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Application.Knowledge;
using ExhibitionServer.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionServer.Application.Chat;

public sealed class AiGatewayClient : IAiGatewayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AiGatewayOptions _options;
    private readonly ILogger<AiGatewayClient> _logger;

    public AiGatewayClient(
        HttpClient httpClient,
        IOptions<AiGatewayOptions> options,
        ILogger<AiGatewayClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
    }

    public async Task<AiChatResponse?> CreateReplyAsync(
        ChatRequest request,
        IReadOnlyList<ExhibitionKnowledgeDocument> retrievedContext,
        IReadOnlyList<AiConversationTurnDto> conversationHistory,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return null;
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(_options.BaseUrl), "/api/ai/chat"));

            AddAuthentication(httpRequest);
            httpRequest.Content = JsonContent.Create(ToAiRequest(request, retrievedContext, conversationHistory));

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var gatewayResponse = await response.Content.ReadFromJsonAsync<AiChatResponse>(
                cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode && gatewayResponse is { Success: true })
            {
                return gatewayResponse;
            }

            _logger.LogWarning(
                "AI Gateway returned non-success. Status={StatusCode}, ErrorCode={ErrorCode}",
                (int)response.StatusCode,
                gatewayResponse?.ErrorCode);

            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("AI Gateway request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AI Gateway request failed. Message={Message}", ex.Message);
            return null;
        }
    }

    public async IAsyncEnumerable<AiChatStreamEvent> StreamReplyAsync(
        ChatRequest request,
        IReadOnlyList<ExhibitionKnowledgeDocument> retrievedContext,
        IReadOnlyList<AiConversationTurnDto> conversationHistory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<AiChatStreamEvent>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        var producer = ProduceStreamEventsAsync(
            request,
            retrievedContext,
            conversationHistory,
            channel.Writer,
            cancellationToken);

        await foreach (var streamEvent in channel.Reader.ReadAllAsync(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return streamEvent;
        }

        await producer.ConfigureAwait(false);
    }

    private async Task ProduceStreamEventsAsync(
        ChatRequest request,
        IReadOnlyList<ExhibitionKnowledgeDocument> retrievedContext,
        IReadOnlyList<AiConversationTurnDto> conversationHistory,
        ChannelWriter<AiChatStreamEvent> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                writer.TryWrite(AiChatStreamEvent.Error("AI_GATEWAY_DISABLED", "AI Gateway is disabled."));
                return;
            }

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(_options.BaseUrl), "/api/ai/chat/stream"));

            AddAuthentication(httpRequest);
            httpRequest.Content = JsonContent.Create(ToAiRequest(request, retrievedContext, conversationHistory));

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                writer.TryWrite(AiChatStreamEvent.Error(
                    "AI_GATEWAY_STREAM_FAILED",
                    $"AI Gateway stream failed with status {(int)response.StatusCode}."));
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? eventType = null;
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                {
                    eventType = line["event:".Length..].Trim();
                    continue;
                }

                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var json = line["data:".Length..].Trim();
                if (json.Length == 0)
                {
                    continue;
                }

                AiChatStreamEvent? streamEvent;
                try
                {
                    streamEvent = JsonSerializer.Deserialize<AiChatStreamEvent>(json, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("Failed to parse AI Gateway stream event. Message={Message}", ex.Message);
                    continue;
                }

                if (streamEvent is null)
                {
                    continue;
                }

                writer.TryWrite(string.IsNullOrWhiteSpace(streamEvent.EventType) && !string.IsNullOrWhiteSpace(eventType)
                    ? streamEvent with { EventType = eventType }
                    : streamEvent);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("AI Gateway stream request timed out.");
            writer.TryWrite(AiChatStreamEvent.Error("AI_GATEWAY_STREAM_TIMEOUT", "AI Gateway stream request timed out."));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AI Gateway stream request failed. Message={Message}", ex.Message);
            writer.TryWrite(AiChatStreamEvent.Error("AI_GATEWAY_STREAM_FAILED", "AI Gateway stream request failed."));
        }
        finally
        {
            writer.Complete();
        }
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientKey))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_options.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(_options.HeaderName, _options.ClientKey);
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ClientKey);
    }

    private static AiChatRequest ToAiRequest(
        ChatRequest request,
        IReadOnlyList<ExhibitionKnowledgeDocument> retrievedContext,
        IReadOnlyList<AiConversationTurnDto> conversationHistory)
    {
        return new AiChatRequest
        {
            Message = request.Message,
            ConversationId = request.ConversationId,
            ConversationHistory = conversationHistory,
            SelectedArtifactId = request.SelectedArtifactId,
            RetrievedContext = retrievedContext.Select(document => new AiRetrievedContextDto
            {
                Id = document.Id,
                Title = document.Title,
                Zone = document.Zone,
                Summary = document.Summary,
                Description = document.Description,
                Tags = document.Tags,
                Aliases = document.Aliases,
                AssetPath = document.AssetPath,
                SourceInstitution = document.SourceInstitution,
                SourceUrl = document.SourceUrl,
                License = document.License
            }).ToArray()
        };
    }
}
