using ExhibitionServer.Application.Voice;

namespace ExhibitionServer.Application.Abstractions;

public interface IVoiceChatService
{
    /// <summary>
    /// 음성 채팅 파이프라인을 실행하고 SSE 이벤트를 스트리밍한다.
    /// 순서: STT → FastAck → ChatGuideService(RAG+LLM) → 문장 단위 TTS
    /// </summary>
    IAsyncEnumerable<VoiceStreamEvent> StreamAsync(
        Stream audioStream,
        string fileName,
        string conversationId,
        string characterId,
        CancellationToken cancellationToken);

    /// <summary>
    /// 패널이 TTS 큐 재생을 완료했을 때 호출.
    /// UE에 speaking 애니메이션 종료를 알린다.
    /// </summary>
    Task OnSpeakingCompleteAsync(string characterId, CancellationToken cancellationToken);
}
