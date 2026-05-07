namespace ExhibitionAiGateway.Options;

public sealed class GatewaySecurityOptions
{
    public const string SectionName = "GatewaySecurity";

    public string? ClientKey { get; set; }

    public string HeaderName { get; set; } = "X-AI-Gateway-Key";
}
