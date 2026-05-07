namespace ExhibitionServer.Options;

public sealed class KnowledgeSearchOptions
{
    public const string SectionName = "KnowledgeSearch";

    public bool EmbeddingsEnabled { get; set; } = false;

    public string EmbeddingProvider { get; set; } = "OpenAI";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public string EmbeddingEndpoint { get; set; } = "https://api.openai.com/v1/embeddings";

    public string? ApiKey { get; set; }

    public double KeywordWeight { get; set; } = 0.35;

    public double VectorWeight { get; set; } = 0.65;

    public double MinHybridScore { get; set; } = 0.12;
}
