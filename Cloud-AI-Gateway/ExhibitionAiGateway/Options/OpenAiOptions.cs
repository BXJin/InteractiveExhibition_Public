namespace ExhibitionAiGateway.Options;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";

    public string ResponsesEndpoint { get; set; } = "https://api.openai.com/v1/responses";

    public int TimeoutSeconds { get; set; } = 30;

    public string SystemPrompt { get; set; } =
        "You are an AI guide for an interactive Unreal Engine exhibition. " +
        "Answer in Korean by default. Keep replies concise, helpful, and safe.";
}