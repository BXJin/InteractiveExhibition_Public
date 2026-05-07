namespace Exhibition.Shared.Ai;

public sealed record AiChatRequest
{
    public required string Message { get; init; }

    public string? ConversationId { get; init; }

    public IReadOnlyList<AiConversationTurnDto> ConversationHistory { get; init; } = [];

    public string? SelectedArtifactId { get; init; }

    public IReadOnlyList<AiRetrievedContextDto> RetrievedContext { get; init; } = [];
}
