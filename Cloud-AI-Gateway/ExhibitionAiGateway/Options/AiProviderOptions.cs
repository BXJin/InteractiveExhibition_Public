namespace ExhibitionAiGateway.Options;

public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public string Provider { get; set; } = "OpenAI";
}
