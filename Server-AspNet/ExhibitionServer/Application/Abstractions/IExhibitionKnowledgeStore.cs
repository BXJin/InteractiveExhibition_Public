using ExhibitionServer.Application.Knowledge;

namespace ExhibitionServer.Application.Abstractions;

public interface IExhibitionKnowledgeStore
{
    IReadOnlyList<ExhibitionKnowledgeDocument> GetCatalogDocuments(int maxResults = 10);

    IReadOnlyList<ExhibitionKnowledgeDocument> Search(string message, string? selectedArtifactId, int maxResults = 3);
}
