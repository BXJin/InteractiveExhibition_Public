using System.Runtime.CompilerServices;
using System.Text.Json;
using Exhibition.Shared.Ai;
using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Application.Chat;
using ExhibitionServer.Realtime.Abstractions;

namespace ExhibitionServer.Application.Voice;

/// <summary>
/// 음성 채팅 파이프라인 오케스트레이터.
///
/// 흐름:
///   1. Gateway STT → transcript
///   2. FastAck → 캐시 오디오 즉시 반환 (latency 완화)
///   3. ChatGuideService.StreamAsync (RAG + LLM 스트리밍)
///      - delta chunk → SentenceBuffer → 문장 완성 시 TTS 호출
///   4. UE에 playAnimation(explain, loop:true/false) 디스패치
/// </summary>
public sealed class VoiceChatService : IVoiceChatService
{
    private readonly IAiGatewayVoiceClient _voiceClient;
    private readonly IChatGuideService _chatGuideService;
    private readonly FastAckService _fastAckService;
    private readonly TtsAudioStore _audioStore;
    private readonly IRawUnrealBroadcaster _unrealBroadcaster;
    private readonly ILogger<VoiceChatService> _logger;

    public VoiceChatService(
        IAiGatewayVoiceClient voiceClient,
        IChatGuideService chatGuideService,
        FastAckService fastAckService,
        TtsAudioStore audioStore,
        IRawUnrealBroadcaster unrealBroadcaster,
        ILogger<VoiceChatService> logger)
    {
        _voiceClient = voiceClient;
        _chatGuideService = chatGuideService;
        _fastAckService = fastAckService;
        _audioStore = audioStore;
        _unrealBroadcaster = unrealBroadcaster;
        _logger = logger;
    }

    public async IAsyncEnumerable<VoiceStreamEvent> StreamAsync(
        Stream audioStream,
        string fileName,
        string conversationId,
        string characterId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 1. STT
        var transcript = await _voiceClient.TranscribeAsync(audioStream, fileName, cancellationToken);
        if (string.IsNullOrWhiteSpace(transcript))
        {
            yield return VoiceStreamEvent.Error("STT_FAILED", "음성을 인식하지 못했습니다. 다시 말씀해 주세요.");
            yield break;
        }

        // 2. Fast Ack — STT 결과 나오자마자 즉시 반환
        var ack = _fastAckService.Select(transcript);
        if (ack.HasValue)
        {
            if (ack.Value.Audio != null)
            {
                var ackId = _audioStore.Store(ack.Value.Audio);
                yield return VoiceStreamEvent.Ack($"/api/voice/audio/{ackId}", ack.Value.Text);
            }
            else
            {
                // wav 파일이 아직 없어도 텍스트 ack는 전달
                yield return VoiceStreamEvent.AckTextOnly(ack.Value.Text);
            }
        }

        // 3. transcript 전달 (패널 텍스트 표시용)
        yield return VoiceStreamEvent.Transcript(transcript);

        // 4. ChatGuideService 스트리밍 + 문장 단위 TTS
        var chatRequest = new ChatRequest
        {
            Message = transcript,
            ConversationId = conversationId,
            CharacterId = characterId,
            VoiceMode = true,
        };

        var sentenceBuffer = new SentenceBuffer();
        var speakingStarted = false;

        await foreach (var streamEvent in _chatGuideService.StreamAsync(chatRequest, cancellationToken))
        {
            if (streamEvent.EventType == AiChatStreamEventTypes.Delta &&
                !string.IsNullOrEmpty(streamEvent.Text))
            {
                yield return VoiceStreamEvent.TextChunk(streamEvent.Text);
                sentenceBuffer.Feed(streamEvent.Text);

                // 완성된 문장마다 TTS 호출
                while (sentenceBuffer.TryFlushSentence(out var sentence))
                {
                    if (!speakingStarted)
                    {
                        await SendPlayAnimationAsync(characterId, loop: true, cancellationToken);
                        speakingStarted = true;
                    }

                    var ttsEvent = await SynthesizeSentenceAsync(sentence, cancellationToken);
                    if (ttsEvent != null)
                    {
                        yield return ttsEvent;
                    }
                }
            }
        }

        // 5. 남은 텍스트 TTS
        var remaining = sentenceBuffer.FlushRemaining();
        if (!string.IsNullOrWhiteSpace(remaining))
        {
            if (!speakingStarted)
            {
                await SendPlayAnimationAsync(characterId, loop: true, cancellationToken);
                speakingStarted = true;
            }

            var ttsEvent = await SynthesizeSentenceAsync(remaining, cancellationToken);
            if (ttsEvent != null)
            {
                yield return ttsEvent;
            }
        }

        yield return VoiceStreamEvent.Done();
    }

    public async Task OnSpeakingCompleteAsync(string characterId, CancellationToken cancellationToken)
    {
        await SendPlayAnimationAsync(characterId, loop: false, cancellationToken);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    private async Task<VoiceStreamEvent?> SynthesizeSentenceAsync(
        string sentence,
        CancellationToken cancellationToken)
    {
        var audioBytes = await _voiceClient.TtsAsync(sentence, voice: null, cancellationToken);
        if (audioBytes == null)
        {
            // TTS 실패 시 텍스트만 반환 (오디오 없이 계속 진행)
            _logger.LogWarning("TTS failed for sentence. Skipping audio. Text={Text}", sentence);
            return null;
        }

        var audioId = _audioStore.Store(audioBytes);
        return VoiceStreamEvent.TtsChunk($"/api/voice/audio/{audioId}", sentence);
    }

    private async Task SendPlayAnimationAsync(
        string characterId,
        bool loop,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                type         = "playAnimation",
                characterId,
                animationKey = "explain",
                loop
            });

            await _unrealBroadcaster.BroadcastRawAsync(json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "playAnimation broadcast failed. Loop={Loop}", loop);
        }
    }
}
