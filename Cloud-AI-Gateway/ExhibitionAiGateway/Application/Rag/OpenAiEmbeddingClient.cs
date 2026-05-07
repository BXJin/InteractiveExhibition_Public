using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Application.Rag;

public sealed class OpenAiEmbeddingClient : IEmbeddingClient
{
    private const string HttpClientName = "openai";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RagOptions _options;
    private readonly ILogger<OpenAiEmbeddingClient> _logger;

    public OpenAiEmbeddingClient(
        IHttpClientFactory httpClientFactory,
        IOptions<RagOptions> options,
        ILogger<OpenAiEmbeddingClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        _options.RerankEnabled &&
        string.Equals(_options.EmbeddingProvider, "OpenAI", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<float[]?> CreateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.EmbeddingEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.EmbeddingModel,
            input
        }, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "OpenAI embedding request failed. StatusCode={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                TrimForLog(body));
            return null;
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Array ||
            dataElement.GetArrayLength() == 0)
        {
            return null;
        }

        var first = dataElement[0];
        if (!first.TryGetProperty("embedding", out var embeddingElement) ||
            embeddingElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var vector = new float[embeddingElement.GetArrayLength()];
        var index = 0;
        foreach (var value in embeddingElement.EnumerateArray())
        {
            vector[index++] = value.GetSingle();
        }

        return vector;
    }

    private static string TrimForLog(string value)
    {
        const int maxLength = 500;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
