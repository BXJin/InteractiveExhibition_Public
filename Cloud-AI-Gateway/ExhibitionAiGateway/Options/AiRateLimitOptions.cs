namespace ExhibitionAiGateway.Options;

public sealed class AiRateLimitOptions
{
    public const string SectionName = "AiRateLimit";

    public int PermitLimit { get; set; } = 30;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; }
}
