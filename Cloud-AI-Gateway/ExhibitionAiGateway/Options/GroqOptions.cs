namespace ExhibitionAiGateway.Options;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "llama-3.1-8b-instant";

    public int TimeoutSeconds { get; set; } = 30;
}
