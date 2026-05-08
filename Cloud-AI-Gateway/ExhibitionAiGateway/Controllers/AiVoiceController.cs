using ExhibitionAiGateway.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExhibitionAiGateway.Controllers;

[ApiController]
[Route("api/ai/voice")]
[EnableRateLimiting("ai-chat")]
public sealed class AiVoiceController : ControllerBase
{
    private const long MaxAudioBytes = 10 * 1024 * 1024; // 10MB

    private readonly ISttProvider _sttProvider;
    private readonly ITtsProvider _ttsProvider;
    private readonly ILogger<AiVoiceController> _logger;

    public AiVoiceController(
        ISttProvider sttProvider,
        ITtsProvider ttsProvider,
        ILogger<AiVoiceController> logger)
    {
        _sttProvider = sttProvider;
        _ttsProvider = ttsProvider;
        _logger = logger;
    }

    /// <summary>
    /// 오디오 파일을 텍스트로 변환한다 (STT).
    /// POST /api/ai/voice/transcribe
    /// Content-Type: multipart/form-data
    /// </summary>
    [HttpPost("transcribe")]
    public async Task<IActionResult> Transcribe(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { success = false, errorCode = "AUDIO_FILE_MISSING" });
        }

        if (file.Length > MaxAudioBytes)
        {
            return BadRequest(new { success = false, errorCode = "AUDIO_FILE_TOO_LARGE" });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var transcript = await _sttProvider.TranscribeAsync(
                stream,
                file.FileName.Length > 0 ? file.FileName : "audio.webm",
                cancellationToken);

            return Ok(new { success = true, transcript });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STT transcription failed.");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { success = false, errorCode = "STT_UPSTREAM_ERROR" });
        }
    }

    /// <summary>
    /// 텍스트를 오디오(wav)로 변환한다 (TTS).
    /// POST /api/ai/voice/tts
    /// Content-Type: application/json
    /// Body: { "text": "...", "voice": "coral" }
    /// </summary>
    [HttpPost("tts")]
    public async Task<IActionResult> Synthesize(
        [FromBody] TtsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { success = false, errorCode = "TTS_TEXT_MISSING" });
        }

        if (request.Text.Length > 4096)
        {
            return BadRequest(new { success = false, errorCode = "TTS_TEXT_TOO_LONG" });
        }

        try
        {
            var audioBytes = await _ttsProvider.SynthesizeAsync(
                request.Text,
                request.Voice,
                cancellationToken);

            return File(audioBytes, "audio/wav");
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS synthesis failed.");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { success = false, errorCode = "TTS_UPSTREAM_ERROR" });
        }
    }
}

public sealed record TtsRequest(string Text, string? Voice);
