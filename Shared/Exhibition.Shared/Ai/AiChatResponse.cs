namespace Exhibition.Shared.Ai;

public sealed record AiChatResponse
{
    public required string Reply { get; init; }

    public IReadOnlyList<AiSuggestedCommandDto> SuggestedCommands { get; init; } = [];

    public bool Success { get; init; }

    public long LatencyMs { get; init; }

    public string? Provider { get; init; }

    public string? Model { get; init; }

    public string? ErrorCode { get; init; }
}
