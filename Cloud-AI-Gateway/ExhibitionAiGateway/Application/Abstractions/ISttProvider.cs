namespace ExhibitionAiGateway.Application.Abstractions;

public interface ISttProvider
{
    /// <summary>
    /// 오디오 스트림을 텍스트로 변환한다.
    /// </summary>
    /// <param name="audioStream">오디오 데이터 스트림</param>
    /// <param name="fileName">파일명 (MIME type 추론용)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>변환된 텍스트 (transcript)</returns>
    Task<string> TranscribeAsync(
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken);
}
