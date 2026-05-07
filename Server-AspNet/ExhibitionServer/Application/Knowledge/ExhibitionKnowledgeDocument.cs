namespace ExhibitionServer.Application.Knowledge;

public sealed record ExhibitionKnowledgeDocument
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Zone { get; init; } = "main_hall";
    public required string Summary { get; init; }
    public required string Description { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
    public string? AssetPath { get; init; }
    public string? SourceInstitution { get; init; }
    public string? SourceUrl { get; init; }
    public string? License { get; init; }
    public bool CatalogVisible { get; init; } = true;
    public string RecommendedEmotion { get; init; } = "neutral";
    public string? RecommendedAnimation { get; init; }
    public string? RecommendedStageEvent { get; init; }
    public string GuideTone { get; init; } = "calm";

    public string SearchText =>
        $"{Id} {Title} {Zone} {Summary} {Description} {string.Join(' ', Tags)} {string.Join(' ', Aliases)} {AssetPath} {SourceInstitution} {License} {RecommendedEmotion} {RecommendedAnimation} {RecommendedStageEvent}";
}
