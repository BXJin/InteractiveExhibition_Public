namespace ExhibitionServer.Application.Chat;

public sealed record ChatRequest
{
    public required string Message { get; init; }
    public string? ConversationId { get; init; }
    public string CharacterId { get; init; } = "Character_01";
    public string? SelectedArtifactId { get; init; }
    public string Mode { get; init; } = "guide";

    /// <summary>
    /// true이면 ChatGuideService가 PlayAnimationCommand를 dispatch하지 않는다.
    /// VoiceChatService가 애니메이션 타이밍을 직접 제어하는 경우 사용.
    /// </summary>
    public bool VoiceMode { get; init; } = false;
}
