namespace Exhibition.Shared.Ai;

public sealed record AiRetrievedContextDto
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public string? Zone { get; init; }

    public string? Summary { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<string> Aliases { get; init; } = [];

    public string? AssetPath { get; init; }

    public string? SourceInstitution { get; init; }

    public string? SourceUrl { get; init; }

    public string? License { get; init; }
}
