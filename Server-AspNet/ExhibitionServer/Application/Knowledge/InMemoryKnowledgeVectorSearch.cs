using System.Collections.Concurrent;
using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionServer.Application.Knowledge;

public sealed class InMemoryKnowledgeVectorSearch : IKnowledgeVectorSearch
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly KnowledgeSearchOptions _options;
    private readonly ILogger<InMemoryKnowledgeVectorSearch> _logger;
    private readonly ConcurrentDictionary<string, float[]> _documentEmbeddings = new(StringComparer.Ordinal);

    public InMemoryKnowledgeVectorSearch(
        IEmbeddingClient embeddingClient,
        IOptions<KnowledgeSearchOptions> options,
        ILogger<InMemoryKnowledgeVectorSearch> logger)
    {
        _embeddingClient = embeddingClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.EmbeddingsEnabled && _embeddingClient.IsConfigured;

    public async Task<IReadOnlyList<KnowledgeVectorSearchResult>> SearchAsync(
        string query,
        IReadOnlyList<ExhibitionKnowledgeDocument> documents,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || documents.Count == 0)
        {
            return [];
        }

        var queryEmbedding = await _embeddingClient.CreateEmbeddingAsync(query, cancellationToken);
        if (queryEmbedding is null || queryEmbedding.Length == 0)
        {
            return [];
        }

        var results = new List<KnowledgeVectorSearchResult>(documents.Count);
        foreach (var document in documents)
        {
            var documentEmbedding = await GetDocumentEmbeddingAsync(document, cancellationToken);
            if (documentEmbedding is null || documentEmbedding.Length != queryEmbedding.Length)
            {
                continue;
            }

            var score = CosineSimilarity(queryEmbedding, documentEmbedding);
            results.Add(new KnowledgeVectorSearchResult(document, score));
        }

        return results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Document.Id)
            .Take(Math.Max(1, maxResults))
            .ToArray();
    }

    private async Task<float[]?> GetDocumentEmbeddingAsync(
        ExhibitionKnowledgeDocument document,
        CancellationToken cancellationToken)
    {
        if (_documentEmbeddings.TryGetValue(document.Id, out var cached))
        {
            return cached;
        }

        var embeddingText = BuildEmbeddingText(document);
        var embedding = await _embeddingClient.CreateEmbeddingAsync(embeddingText, cancellationToken);
        if (embedding is null || embedding.Length == 0)
        {
            _logger.LogWarning("Failed to create document embedding. DocumentId={DocumentId}", document.Id);
            return null;
        }

        _documentEmbeddings[document.Id] = embedding;
        return embedding;
    }

    private static string BuildEmbeddingText(ExhibitionKnowledgeDocument document)
    {
        return string.Join('\n', new[]
        {
            document.Title,
            document.Zone,
            document.Summary,
            document.Description,
            string.Join(", ", document.Tags),
            string.Join(", ", document.Aliases),
            document.SourceInstitution ?? string.Empty
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var i = 0; i < left.Count; i++)
        {
            dot += left[i] * right[i];
            leftNorm += left[i] * left[i];
            rightNorm += right[i] * right[i];
        }

        if (leftNorm <= 0 || rightNorm <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }
}
