using System.Net.Http.Headers;
using System.Text.Json;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Providers.OpenAI;

public sealed class OpenAiSttProvider : ISttProvider
{
    private const string HttpClientName = "openai";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly VoiceOptions _voiceOptions;
    private readonly ILogger<OpenAiSttProvider> _logger;

    public OpenAiSttProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<VoiceOptions> voiceOptions,
        ILogger<OpenAiSttProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAiOptions = openAiOptions.Value;
        _voiceOptions = voiceOptions.Value;
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);

        using var form = new MultipartFormDataContent();

        var audioContent = new StreamContent(audioStream);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue(ResolveMimeType(fileName));
        form.Add(audioContent, "file", fileName);
        form.Add(new StringContent(_voiceOptions.SttModel), "model");
        form.Add(new StringContent("ko"), "language");

        var response = await client.PostAsync(_voiceOptions.SttEndpoint, form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("STT request failed ({StatusCode}): {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"STT failed: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("text", out var textElement))
        {
            _logger.LogError("STT response missing 'text' field: {Json}", json);
            throw new InvalidOperationException("STT response missing 'text' field.");
        }

        return textElement.GetString() ?? string.Empty;
    }

    private static string ResolveMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".webm" => "audio/webm",
            ".mp4"  => "audio/mp4",
            ".m4a"  => "audio/mp4",
            ".wav"  => "audio/wav",
            ".ogg"  => "audio/ogg",
            _       => "audio/webm"
        };
    }
}
