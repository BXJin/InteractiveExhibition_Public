namespace ExhibitionAiGateway.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public bool RerankEnabled { get; set; } = true;

    public string EmbeddingProvider { get; set; } = "OpenAI";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public string EmbeddingEndpoint { get; set; } = "https://api.openai.com/v1/embeddings";

    public int MaxContextAfterRerank { get; set; } = 3;

    public double MinRerankScore { get; set; } = 0;

    public string? ApiKey { get; set; }
}
