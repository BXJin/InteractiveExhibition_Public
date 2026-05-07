namespace ExhibitionServer.Application.Chat;

public sealed record ChatRequest
{
    public required string Message { get; init; }
    public string? ConversationId { get; init; }
    public string CharacterId { get; init; } = "Character_01";
    public string? SelectedArtifactId { get; init; }
    public string Mode { get; init; } = "guide";
}
