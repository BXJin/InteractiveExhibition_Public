using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Exhibition.Shared.Ai;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Providers.OpenAI;

public sealed class OpenAiResponsesProvider : IAiChatProvider
{
    private const string HttpClientName = "openai";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiResponsesProvider> _logger;

    public OpenAiResponsesProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiResponsesProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "OpenAI";

    public string Model => _options.Model;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public string MissingConfigurationErrorCode => "OPENAI_API_KEY_MISSING";

    public async Task<AiChatResponse> CreateReplyAsync(
        AiChatRequest request,
        CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.ResponsesEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(BuildPayload(request), options: JsonOptions);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI API returned {(int)response.StatusCode}: {TrimForLog(body)}");
        }

        using var document = JsonDocument.Parse(body);
        if (TryReadOutputText(document.RootElement, out var outputText))
        {
            return ParseModelOutput(outputText);
        }

        throw new InvalidOperationException("OpenAI API response did not include output text.");
    }

    public async IAsyncEnumerable<AiChatStreamEvent> StreamReplyAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<AiChatStreamEvent>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        var producer = ProduceStreamEventsAsync(request, channel.Writer, cancellationToken);

        await foreach (var streamEvent in channel.Reader.ReadAllAsync(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return streamEvent;
        }

        await producer.ConfigureAwait(false);
    }

    private async Task ProduceStreamEventsAsync(
        AiChatRequest request,
        ChannelWriter<AiChatStreamEvent> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.ResponsesEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            httpRequest.Content = JsonContent.Create(BuildStreamPayload(request), options: JsonOptions);

            using var response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"OpenAI API returned {(int)response.StatusCode}: {TrimForLog(body)}");
            }

            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            var extractor = new ReplyStreamExtractor();
            string? fullOutputText = null;

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null) break;
                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;

                var json = line["data:".Length..].Trim();
                if (json is "[DONE]" or "") continue;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeElement)) continue;

                switch (typeElement.GetString())
                {
                    case "response.output_text.delta":
                        if (root.TryGetProperty("delta", out var deltaElement) &&
                            deltaElement.ValueKind == JsonValueKind.String)
                        {
                            var deltaText = deltaElement.GetString();
                            if (!string.IsNullOrEmpty(deltaText))
                            {
                                var replyChunk = extractor.AddChunk(deltaText);
                                if (!string.IsNullOrEmpty(replyChunk))
                                {
                                    writer.TryWrite(AiChatStreamEvent.Delta(replyChunk));
                                }
                            }
                        }
                        break;

                    case "response.output_text.done":
                        if (root.TryGetProperty("text", out var textElement) &&
                            textElement.ValueKind == JsonValueKind.String)
                        {
                            fullOutputText = textElement.GetString();
                        }
                        break;

                    case "response.failed":
                        var errorMsg = "OpenAI response failed.";
                        if (root.TryGetProperty("response", out var failedResp) &&
                            failedResp.TryGetProperty("error", out var errEl) &&
                            errEl.TryGetProperty("message", out var errMsgEl))
                        {
                            errorMsg = errMsgEl.GetString() ?? errorMsg;
                        }
                        writer.TryWrite(AiChatStreamEvent.Error("OPENAI_RESPONSE_FAILED", errorMsg));
                        return;
                }
            }

            if (!string.IsNullOrWhiteSpace(fullOutputText))
            {
                writer.TryWrite(AiChatStreamEvent.Complete(ParseModelOutput(fullOutputText)));
            }
            else
            {
                writer.TryWrite(AiChatStreamEvent.Error(
                    "OPENAI_STREAM_NO_OUTPUT",
                    "OpenAI stream completed without output text."));
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("OpenAI stream request timed out.");
            writer.TryWrite(AiChatStreamEvent.Error("OPENAI_TIMEOUT", "OpenAI 요청이 시간 초과되었습니다."));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI stream request failed.");
            writer.TryWrite(AiChatStreamEvent.Error("OPENAI_STREAM_FAILED", "OpenAI 스트림 요청에 실패했습니다."));
        }
        finally
        {
            writer.Complete();
        }
    }

    private object BuildStreamPayload(AiChatRequest request)
    {
        return new
        {
            model = _options.Model,
            instructions = BuildInstructions(),
            input = BuildInput(request),
            stream = true
        };
    }

    private object BuildPayload(AiChatRequest request)
    {
        return new
        {
            model = _options.Model,
            instructions = BuildInstructions(),
            input = BuildInput(request)
        };
    }

    private string BuildInstructions()
    {
        return _options.SystemPrompt + """


You are a friendly AI guide for an interactive exhibition. You can hold general conversation as well as answer exhibition-specific questions. Always reply in Korean unless the user writes in another language.

Return only a JSON object. Do not wrap it in markdown.
Schema:
{
  "reply": "Korean guide response for the user",
  "suggestedCommands": [
    {
      "type": "setEmotion | playAnimation | triggerStageEvent",
      "characterId": "Character_01",
      "emotion": "happy | sad | angry | surprise | neutral",
      "animation": "wave | bow | clap | explain | idle",
      "stageEvent": "style_classic | style_aged | style_modern | light.spotOn | light.spotOff | scene.reset"
    }
  ]
}

Emotion selection — choose based on the TONE and CONTENT of YOUR reply, not the user's wording:
- When greeting or welcoming the user → setEmotion greeting
- When explaining, describing, or introducing an exhibit (most common) → setEmotion explaining
- When exploring an interesting topic, responding to a curious question → setEmotion curious
- When giving an analytical, historical, or reflective answer → setEmotion thinking
- When your reply is enthusiastic, exciting, or celebratory → setEmotion happy
- When your reply is about something awe-inspiring or unexpected → setEmotion surprise
- When your reply is solemn, melancholic, or deeply reverent → setEmotion sad
- When your reply is dramatic, tense, or powerful → setEmotion angry
- For calm neutral responses with no particular tone → setEmotion neutral

Animation selection — choose based on context:
- Greeting or welcoming → playAnimation wave
- Explaining, introducing, or describing an artifact → playAnimation explain
- Showing respect or reverence (ancient/historical objects) → playAnimation bow
- Celebrating, congratulating, or expressing great enthusiasm → playAnimation clap
- Neutral or idle state → playAnimation idle
- Do NOT pair wave with explaining — use one or the other per message.

Scene rules:
- Only suggest triggerStageEvent when the user explicitly asks to change atmosphere or lighting.
- If only asking whether it is possible, explain it in the reply but do not trigger a scene change.
- Atmosphere options: style_classic (고전적), style_aged (빈티지), style_modern (현대적)
- Lighting options: light.spotOn, light.spotOff

Conversation rules:
- For greetings → setEmotion greeting + playAnimation wave.
- If asked what you can do → setEmotion explaining + reply with capability list. No animation needed.
- If asked what exhibits are here → setEmotion explaining + playAnimation explain.
- For exhibit explanations → setEmotion explaining + playAnimation explain.
- For curious/exploratory questions → setEmotion curious. No animation needed unless explaining.
- For analytical/historical questions → setEmotion thinking + playAnimation explain if describing.
- For unrelated questions, answer briefly and helpfully, then gently redirect to the exhibition. Use setEmotion curious.
- If the user requests a specific mood (e.g. "밝게 소개해줘"), honor the mood for emotion (happy) but still add playAnimation explain if describing.

General rules:
- ALWAYS include exactly one setEmotion command per reply. Never omit it.
- Use at most 3 suggestedCommands total (1 setEmotion + up to 2 others).
- Omit fields that do not apply to the command type.
- Do not suggest movement, rotation, file, shell, network, or arbitrary execution commands.
""";
    }

    private static string BuildInput(AiChatRequest request)
    {
        if (request.RetrievedContext.Count == 0 && request.ConversationHistory.Count == 0)
        {
            return request.Message;
        }

        var builder = new StringBuilder();
        if (request.ConversationHistory.Count > 0)
        {
            builder.AppendLine("Recent conversation history:");
            foreach (var turn in request.ConversationHistory)
            {
                builder.Append("- ");
                builder.Append(NormalizeRole(turn.Role));
                builder.Append(": ");
                builder.AppendLine(turn.Content);
            }

            builder.AppendLine();
        }

        builder.AppendLine("User message:");
        builder.AppendLine(request.Message);
        builder.AppendLine();

        if (request.RetrievedContext.Count == 0)
        {
            return builder.ToString();
        }

        builder.AppendLine("Retrieved exhibition context:");

        foreach (var context in request.RetrievedContext)
        {
            builder.Append("- ");
            builder.Append(context.Title);
            builder.Append(" (");
            builder.Append(context.Id);
            builder.Append(')');

            if (!string.IsNullOrWhiteSpace(context.Summary))
            {
                builder.Append(": ");
                builder.Append(context.Summary);
            }

            if (!string.IsNullOrWhiteSpace(context.Description))
            {
                builder.Append(" Description: ");
                builder.Append(context.Description);
            }

            if (!string.IsNullOrWhiteSpace(context.Zone))
            {
                builder.Append(" Zone: ");
                builder.Append(context.Zone);
            }

            if (context.Tags.Count > 0)
            {
                builder.Append(" Tags: ");
                builder.Append(string.Join(", ", context.Tags));
            }

            if (context.Aliases.Count > 0)
            {
                builder.Append(" Aliases: ");
                builder.Append(string.Join(", ", context.Aliases));
            }

            if (!string.IsNullOrWhiteSpace(context.SourceInstitution))
            {
                builder.Append(" Source: ");
                builder.Append(context.SourceInstitution);
            }

            if (!string.IsNullOrWhiteSpace(context.License))
            {
                builder.Append(" License: ");
                builder.Append(context.License);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string NormalizeRole(string role)
    {
        return string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)
            ? "assistant"
            : "user";
    }

    private static bool TryReadOutputText(JsonElement root, out string outputText)
    {
        if (root.TryGetProperty("output_text", out var outputTextElement) &&
            outputTextElement.ValueKind == JsonValueKind.String)
        {
            outputText = outputTextElement.GetString() ?? string.Empty;
            return outputText.Length > 0;
        }

        if (!root.TryGetProperty("output", out var outputElement) ||
            outputElement.ValueKind != JsonValueKind.Array)
        {
            outputText = string.Empty;
            return false;
        }

        var builder = new StringBuilder();

        foreach (var outputItem in outputElement.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in contentElement.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    builder.Append(textElement.GetString());
                }
            }
        }

        outputText = builder.ToString();
        return outputText.Length > 0;
    }

    private AiChatResponse ParseModelOutput(string outputText)
    {
        var json = StripMarkdownFence(outputText);

        try
        {
            var modelOutput = JsonSerializer.Deserialize<AiChatModelOutput>(json, JsonOptions);
            if (!string.IsNullOrWhiteSpace(modelOutput?.Reply))
            {
                return new AiChatResponse
                {
                    Reply = modelOutput.Reply,
                    SuggestedCommands = modelOutput.SuggestedCommands ?? [],
                    Success = true
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Failed to parse structured JSON from model output. Falling back to raw text. Output={Output}",
                TrimForLog(outputText));
        }

        return new AiChatResponse
        {
            Reply = outputText,
            Success = true
        };
    }

    private static string StripMarkdownFence(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLineEnd = trimmed.IndexOf('\n');
        if (firstLineEnd < 0)
        {
            return trimmed;
        }

        var withoutOpeningFence = trimmed[(firstLineEnd + 1)..].Trim();
        return withoutOpeningFence.EndsWith("```", StringComparison.Ordinal)
            ? withoutOpeningFence[..^3].Trim()
            : withoutOpeningFence;
    }

    private static string TrimForLog(string value)
    {
        const int maxLength = 500;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed record AiChatModelOutput
    {
        public string? Reply { get; init; }

        public IReadOnlyList<AiSuggestedCommandDto>? SuggestedCommands { get; init; }
    }
}
