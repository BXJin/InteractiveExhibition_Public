namespace ExhibitionServer.Options;

public sealed class AiGatewayOptions
{
    public const string SectionName = "AiGateway";

    public bool Enabled { get; set; } = true;

    public string? BaseUrl { get; set; }

    public string? ClientKey { get; set; }

    public string HeaderName { get; set; } = "X-AI-Gateway-Key";

    public int TimeoutSeconds { get; set; } = 20;
}
