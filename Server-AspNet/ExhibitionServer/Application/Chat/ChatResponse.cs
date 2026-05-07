using Exhibition.Shared.Commands;

namespace ExhibitionServer.Application.Chat;

public sealed record ChatResponse
{
    public required string Reply { get; init; }
    public IReadOnlyList<ExhibitionCommand> Commands { get; init; } = Array.Empty<ExhibitionCommand>();
    public IReadOnlyList<string> RetrievedSources { get; init; } = Array.Empty<string>();
    public bool Success { get; init; } = true;
    public string? Error { get; init; }
    public long LatencyMs { get; init; }
}
