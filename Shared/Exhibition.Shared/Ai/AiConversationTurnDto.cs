namespace Exhibition.Shared.Ai;

public sealed record AiConversationTurnDto
{
    public required string Role { get; init; }

    public required string Content { get; init; }
}
