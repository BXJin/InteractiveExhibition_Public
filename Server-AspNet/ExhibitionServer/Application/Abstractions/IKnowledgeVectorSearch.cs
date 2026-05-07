using ExhibitionServer.Application.Knowledge;

namespace ExhibitionServer.Application.Abstractions;

public interface IKnowledgeVectorSearch
{
    bool IsEnabled { get; }

    Task<IReadOnlyList<KnowledgeVectorSearchResult>> SearchAsync(
        string query,
        IReadOnlyList<ExhibitionKnowledgeDocument> documents,
        int maxResults,
        CancellationToken cancellationToken = default);
}

public sealed record KnowledgeVectorSearchResult(
    ExhibitionKnowledgeDocument Document,
    double Score);
