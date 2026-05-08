namespace ExhibitionAiGateway.Options;

public sealed class VoiceOptions
{
    public const string SectionName = "Voice";

    public string SttModel { get; set; } = "whisper-1";

    public string SttEndpoint { get; set; } = "https://api.openai.com/v1/audio/transcriptions";

    public string TtsModel { get; set; } = "gpt-4o-mini-tts";

    public string TtsEndpoint { get; set; } = "https://api.openai.com/v1/audio/speech";

    /// <summary>기본 TTS 음성. ash | echo | verse | onyx | coral 등</summary>
    public string DefaultVoice { get; set; } = "alloy";

    /// <summary>TTS 발화 스타일 지시문</summary>
    public string TtsInstructions { get; set; } =
        "한국어로 또렷하고 명확하게 발음해. 단어를 늘이거나 감정을 과장하지 마. " +
        "짧고 깔끔하게 말하되, 친근하고 밝은 전시 가이드 톤을 유지해.";

    /// <summary>TTS 응답 오디오 포맷</summary>
    public string TtsResponseFormat { get; set; } = "wav";
}
