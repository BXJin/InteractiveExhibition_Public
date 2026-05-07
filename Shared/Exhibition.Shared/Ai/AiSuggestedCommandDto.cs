namespace Exhibition.Shared.Ai;

public sealed record AiSuggestedCommandDto
{
    public required string Type { get; init; }

    public string? CharacterId { get; init; }

    public string? Emotion { get; init; }

    public string? Animation { get; init; }

    public string? StageEvent { get; init; }
}
