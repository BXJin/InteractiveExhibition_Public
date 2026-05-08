namespace ExhibitionServer.Application.Abstractions;

public interface IAiGatewayVoiceClient
{
    /// <summary>오디오 스트림을 텍스트로 변환 (STT)</summary>
    Task<string?> TranscribeAsync(
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken);

    /// <summary>텍스트를 오디오 바이트로 변환 (TTS). 실패 시 null 반환.</summary>
    Task<byte[]?> TtsAsync(
        string text,
        string? voice,
        CancellationToken cancellationToken);
}
