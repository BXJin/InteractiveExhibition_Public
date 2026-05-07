using System.Text.Json;
using Exhibition.Shared.Ai;

namespace ExhibitionAiGateway.Streaming;

public static class SseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task WriteEventAsync(
        HttpResponse response,
        AiChatStreamEvent streamEvent,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: {streamEvent.EventType}\n", cancellationToken);
        await response.WriteAsync("data: ", cancellationToken);
        await JsonSerializer.SerializeAsync(response.Body, streamEvent, JsonOptions, cancellationToken);
        await response.WriteAsync("\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
