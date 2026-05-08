namespace ExhibitionServer.Application.Voice;

/// <summary>
/// VoiceChatService → Controller SSE 이벤트 타입.
/// </summary>
public sealed record VoiceStreamEvent
{
    public required string Type { get; init; }

    /// <summary>transcript, text_chunk, ack 텍스트</summary>
    public string? Text { get; init; }

    /// <summary>GET /api/voice/audio/{id} 형식의 오디오 URL</summary>
    public string? AudioUrl { get; init; }

    /// <summary>에러 코드 (type="error" 일 때)</summary>
    public string? ErrorCode { get; init; }

    public static VoiceStreamEvent Ack(string audioUrl, string text) =>
        new() { Type = "ack", AudioUrl = audioUrl, Text = text };

    public static VoiceStreamEvent AckTextOnly(string text) =>
        new() { Type = "ack", Text = text };

    public static VoiceStreamEvent Transcript(string text) =>
        new() { Type = "transcript", Text = text };

    public static VoiceStreamEvent TextChunk(string text) =>
        new() { Type = "text_chunk", Text = text };

    public static VoiceStreamEvent TtsChunk(string audioUrl, string text) =>
        new() { Type = "tts_chunk", AudioUrl = audioUrl, Text = text };

    public static VoiceStreamEvent Done() =>
        new() { Type = "done" };

    public static VoiceStreamEvent Error(string errorCode, string message) =>
        new() { Type = "error", ErrorCode = errorCode, Text = message };
}
