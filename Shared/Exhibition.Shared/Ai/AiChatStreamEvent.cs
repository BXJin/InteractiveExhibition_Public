namespace Exhibition.Shared.Ai;

public sealed record AiChatStreamEvent
{
    public required string EventType { get; init; }

    public string? Text { get; init; }

    public AiChatResponse? CompleteResponse { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }

    public static AiChatStreamEvent Delta(string text) => new()
    {
        EventType = AiChatStreamEventTypes.Delta,
        Text = text
    };

    public static AiChatStreamEvent Complete(AiChatResponse response) => new()
    {
        EventType = AiChatStreamEventTypes.Complete,
        CompleteResponse = response
    };

    public static AiChatStreamEvent Error(string errorCode, string message) => new()
    {
        EventType = AiChatStreamEventTypes.Error,
        ErrorCode = errorCode,
        Message = message
    };
}
