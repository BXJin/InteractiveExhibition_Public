namespace ExhibitionAiGateway.Options;

/// <summary>
/// Provider에 무관한 Gateway 공통 요청 검증 설정.
/// </summary>
public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    /// <summary>단일 요청에서 허용하는 최대 입력 문자 수.</summary>
    public int MaxInputChars { get; set; } = 1000;
}
