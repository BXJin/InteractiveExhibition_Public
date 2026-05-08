using System.Text.Json;
using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Application.Voice;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionServer.Controllers;

[ApiController]
[Route("api/voice")]
public sealed class VoiceChatController : ControllerBase
{
    private const long MaxAudioBytes = 10 * 1024 * 1024; // 10MB

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IVoiceChatService _voiceChatService;
    private readonly TtsAudioStore _audioStore;
    private readonly ILogger<VoiceChatController> _logger;

    public VoiceChatController(
        IVoiceChatService voiceChatService,
        TtsAudioStore audioStore,
        ILogger<VoiceChatController> logger)
    {
        _voiceChatService = voiceChatService;
        _audioStore = audioStore;
        _logger = logger;
    }

    /// <summary>
    /// 음성 채팅 SSE 스트리밍 엔드포인트.
    /// POST /api/voice/chat
    /// Content-Type: multipart/form-data
    /// Fields: file (audio), sessionId, conversationId?, characterId?
    /// </summary>
    [HttpPost("chat")]
    public async Task StreamVoiceChat(
        IFormFile file,
        [FromForm] string? conversationId,
        [FromForm] string? characterId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { errorCode = "AUDIO_FILE_MISSING" }, cancellationToken);
            return;
        }

        if (file.Length > MaxAudioBytes)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { errorCode = "AUDIO_FILE_TOO_LARGE" }, cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "audio.webm" : file.FileName;
        var resolvedConversationId = conversationId ?? Guid.NewGuid().ToString("n");
        var resolvedCharacterId    = characterId ?? "Character_01";

        try
        {
            await using var audioStream = file.OpenReadStream();

            await foreach (var voiceEvent in _voiceChatService.StreamAsync(
                audioStream,
                fileName,
                resolvedConversationId,
                resolvedCharacterId,
                cancellationToken))
            {
                await WriteSseEventAsync(voiceEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 클라이언트 연결 끊김 — 정상 종료
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VoiceChat stream error.");

            var errorEvent = VoiceStreamEvent.Error("VOICE_CHAT_ERROR", "음성 채팅 처리 중 오류가 발생했습니다.");
            await WriteSseEventAsync(errorEvent, cancellationToken);
        }
    }

    /// <summary>
    /// 패널이 TTS 큐 재생을 완료했을 때 호출.
    /// POST /api/voice/speaking-complete
    /// Body: { "characterId": "Character_01" }
    /// </summary>
    [HttpPost("speaking-complete")]
    public async Task<IActionResult> SpeakingComplete(
        [FromBody] SpeakingCompleteRequest request,
        CancellationToken cancellationToken)
    {
        await _voiceChatService.OnSpeakingCompleteAsync(
            request.CharacterId ?? "Character_01",
            cancellationToken);

        return Ok();
    }

    /// <summary>
    /// TTS 오디오 파일 반환.
    /// GET /api/voice/audio/{id}
    /// </summary>
    [HttpGet("audio/{id}")]
    public IActionResult GetAudio(string id)
    {
        var audioBytes = _audioStore.Get(id);
        if (audioBytes == null)
        {
            return NotFound();
        }

        return File(audioBytes, "audio/wav");
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    private async Task WriteSseEventAsync(VoiceStreamEvent voiceEvent, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {voiceEvent.Type}\n", cancellationToken);
        await Response.WriteAsync("data: ", cancellationToken);
        await JsonSerializer.SerializeAsync(Response.Body, voiceEvent, JsonOptions, cancellationToken);
        await Response.WriteAsync("\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}

public sealed record SpeakingCompleteRequest(string? CharacterId);
