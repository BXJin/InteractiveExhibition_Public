using System.Net.Http.Headers;
using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionServer.Application.Chat;

/// <summary>
/// AI Gateway의 STT/TTS 엔드포인트를 호출하는 클라이언트.
/// API 키는 Gateway에만 보관되므로 로컬 서버는 Gateway를 경유한다.
/// </summary>
public sealed class AiGatewayVoiceClient : IAiGatewayVoiceClient
{
    private readonly HttpClient _httpClient;
    private readonly AiGatewayOptions _options;
    private readonly ILogger<AiGatewayVoiceClient> _logger;

    public AiGatewayVoiceClient(
        HttpClient httpClient,
        IOptions<AiGatewayOptions> options,
        ILogger<AiGatewayVoiceClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
        _logger = logger;
    }

    public async Task<string?> TranscribeAsync(
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return null;
        }

        try
        {
            using var form = new MultipartFormDataContent();
            var audioContent = new StreamContent(audioStream);
            form.Add(audioContent, "file", fileName);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(_options.BaseUrl), "/api/ai/voice/transcribe"));

            AddAuthentication(request);
            request.Content = form;

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("STT request failed. Status={StatusCode}", (int)response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TranscribeResponse>(
                cancellationToken: cancellationToken);

            return result?.Transcript;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("STT request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("STT request failed. Message={Message}", ex.Message);
            return null;
        }
    }

    public async Task<byte[]?> TtsAsync(
        string text,
        string? voice,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(_options.BaseUrl), "/api/ai/voice/tts"));

            AddAuthentication(request);
            request.Content = System.Net.Http.Json.JsonContent.Create(new { text, voice });

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TTS request failed. Status={StatusCode}", (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("TTS request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("TTS request failed. Message={Message}", ex.Message);
            return null;
        }
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientKey))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_options.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(_options.HeaderName, _options.ClientKey);
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ClientKey);
    }

    private sealed record TranscribeResponse(bool Success, string? Transcript);
}
