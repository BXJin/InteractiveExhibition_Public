namespace ExhibitionAiGateway.Options;

public sealed class LocalLlmOptions
{
    public const string SectionName = "LocalLlm";

    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";

    public string Model { get; set; } = "gemma3:4b";

    public string ApiStyle { get; set; } = "Ollama";

    public int TimeoutSeconds { get; set; } = 60;
}
