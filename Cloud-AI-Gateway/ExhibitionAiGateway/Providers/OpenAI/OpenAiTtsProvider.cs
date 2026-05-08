using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Providers.OpenAI;

public sealed class OpenAiTtsProvider : ITtsProvider
{
    private const string HttpClientName = "openai";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly VoiceOptions _voiceOptions;
    private readonly ILogger<OpenAiTtsProvider> _logger;

    public OpenAiTtsProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<VoiceOptions> voiceOptions,
        ILogger<OpenAiTtsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAiOptions = openAiOptions.Value;
        _voiceOptions = voiceOptions.Value;
        _logger = logger;
    }

    public async Task<byte[]> SynthesizeAsync(
        string text,
        string? voice,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);

        var body = new
        {
            model        = _voiceOptions.TtsModel,
            input        = text,
            voice        = voice ?? _voiceOptions.DefaultVoice,
            instructions = _voiceOptions.TtsInstructions,
            response_format = _voiceOptions.TtsResponseFormat
        };

        var json    = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_voiceOptions.TtsEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("TTS request failed ({StatusCode}): {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"TTS failed: {response.StatusCode}");
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
