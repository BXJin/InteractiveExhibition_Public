using Exhibition.Shared.Commands;

namespace ExhibitionServer.Application.Chat;

public sealed record ChatLogEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? ConversationId { get; init; }
    public required string UserText { get; init; }
    public string? SelectedArtifactId { get; init; }
    public IReadOnlyList<string> RetrievedSources { get; init; } = Array.Empty<string>();
    public required string Reply { get; init; }
    public IReadOnlyList<ExhibitionCommand> GeneratedCommands { get; init; } = Array.Empty<ExhibitionCommand>();
    public IReadOnlyList<Guid> ExecutedCommandIds { get; init; } = Array.Empty<Guid>();
    public bool Success { get; init; }
    public long LatencyMs { get; init; }
    public string PromptVersion { get; init; } = "guide-rule-v1";
}
