using System.Collections.Concurrent;
using Exhibition.Shared.Ai;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Application.Rag;

public sealed class EmbeddingRetrievedContextRanker : IRetrievedContextRanker
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly RagOptions _options;
    private readonly ILogger<EmbeddingRetrievedContextRanker> _logger;
    private readonly ConcurrentDictionary<string, float[]> _contextEmbeddingCache = new(StringComparer.Ordinal);

    public EmbeddingRetrievedContextRanker(
        IEmbeddingClient embeddingClient,
        IOptions<RagOptions> options,
        ILogger<EmbeddingRetrievedContextRanker> logger)
    {
        _embeddingClient = embeddingClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AiRetrievedContextDto>> RankAsync(
        string query,
        IReadOnlyList<AiRetrievedContextDto> contexts,
        CancellationToken cancellationToken = default)
    {
        if (!_options.RerankEnabled ||
            !_embeddingClient.IsConfigured ||
            contexts.Count <= 1 ||
            string.IsNullOrWhiteSpace(query))
        {
            return Limit(contexts);
        }

        try
        {
            var queryEmbedding = await _embeddingClient.CreateEmbeddingAsync(query, cancellationToken);
            if (queryEmbedding is null || queryEmbedding.Length == 0)
            {
                return Limit(contexts);
            }

            var results = new List<RankedContext>(contexts.Count);
            foreach (var context in contexts)
            {
                var contextEmbedding = await GetContextEmbeddingAsync(context, cancellationToken);
                if (contextEmbedding is null || contextEmbedding.Length != queryEmbedding.Length)
                {
                    continue;
                }

                var score = CosineSimilarity(queryEmbedding, contextEmbedding);
                if (score >= _options.MinRerankScore)
                {
                    results.Add(new RankedContext(context, score));
                }
            }

            return results.Count > 0
                ? results
                    .OrderByDescending(result => result.Score)
                    .ThenBy(result => result.Context.Id, StringComparer.Ordinal)
                    .Take(Math.Max(1, _options.MaxContextAfterRerank))
                    .Select(result => result.Context)
                    .ToArray()
                : Limit(contexts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding context rerank failed. Falling back to incoming context order.");
            return Limit(contexts);
        }
    }

    private async Task<float[]?> GetContextEmbeddingAsync(
        AiRetrievedContextDto context,
        CancellationToken cancellationToken)
    {
        var text = BuildEmbeddingText(context);
        var cacheKey = $"{context.Id}:{StringComparer.Ordinal.GetHashCode(text)}";
        if (_contextEmbeddingCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var embedding = await _embeddingClient.CreateEmbeddingAsync(text, cancellationToken);
        if (embedding is null || embedding.Length == 0)
        {
            _logger.LogWarning("Failed to create context embedding. ContextId={ContextId}", context.Id);
            return null;
        }

        _contextEmbeddingCache[cacheKey] = embedding;
        return embedding;
    }

    private IReadOnlyList<AiRetrievedContextDto> Limit(IReadOnlyList<AiRetrievedContextDto> contexts)
    {
        return contexts
            .Take(Math.Max(1, _options.MaxContextAfterRerank))
            .ToArray();
    }

    private static string BuildEmbeddingText(AiRetrievedContextDto context)
    {
        return string.Join('\n', new[]
        {
            context.Title,
            context.Zone,
            context.Summary,
            context.Description,
            string.Join(", ", context.Tags),
            string.Join(", ", context.Aliases),
            context.SourceInstitution
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

    private sealed record RankedContext(AiRetrievedContextDto Context, double Score);
}
