namespace ExhibitionAiGateway.Application.Abstractions;

public interface ITtsProvider
{
    /// <summary>
    /// 텍스트를 오디오 데이터로 변환한다.
    /// </summary>
    /// <param name="text">변환할 텍스트</param>
    /// <param name="voice">음성 이름 (null이면 기본값 사용)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>오디오 바이너리 (wav)</returns>
    Task<byte[]> SynthesizeAsync(
        string text,
        string? voice,
        CancellationToken cancellationToken);
}
