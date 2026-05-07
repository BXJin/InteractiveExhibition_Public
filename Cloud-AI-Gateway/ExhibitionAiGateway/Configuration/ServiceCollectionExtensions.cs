using ExhibitionAiGateway.Application;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Application.Rag;
using ExhibitionAiGateway.Options;
using ExhibitionAiGateway.Providers;
using ExhibitionAiGateway.Providers.OpenAI;
using System.Threading.RateLimiting;

namespace ExhibitionAiGateway.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAiGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RegisterGatewayOptions(services, configuration);
        RegisterProviderOptions(services, configuration);
        RegisterRateLimiter(services, configuration);
        RegisterHttpClients(services, configuration);
        RegisterProviders(services);
        RegisterApplicationServices(services);

        return services;
    }

    // ── Options ───────────────────────────────────────────────────────────────

    private static void RegisterGatewayOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GatewayOptions>(configuration.GetSection(GatewayOptions.SectionName));

        services.Configure<GatewaySecurityOptions>(configuration.GetSection(GatewaySecurityOptions.SectionName));
        services.PostConfigure<GatewaySecurityOptions>(options =>
        {
            options.ClientKey ??= configuration["AI_GATEWAY_CLIENT_KEY"];
        });

        services.Configure<AiRateLimitOptions>(configuration.GetSection(AiRateLimitOptions.SectionName));
        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.PostConfigure<RagOptions>(options =>
        {
            options.ApiKey ??= configuration["OPENAI_API_KEY"];
            options.EmbeddingModel = configuration["OPENAI_EMBEDDING_MODEL"] ?? options.EmbeddingModel;
        });
    }

    private static void RegisterProviderOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.PostConfigure<OpenAiOptions>(options =>
        {
            options.ApiKey ??= configuration["OPENAI_API_KEY"];
            options.Model = configuration["OPENAI_MODEL"] ?? options.Model;
        });

        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.PostConfigure<GeminiOptions>(options =>
        {
            options.ApiKey ??= configuration["GEMINI_API_KEY"];
            options.Model = configuration["GEMINI_MODEL"] ?? options.Model;
        });

        services.Configure<GroqOptions>(configuration.GetSection(GroqOptions.SectionName));
        services.PostConfigure<GroqOptions>(options =>
        {
            options.ApiKey ??= configuration["GROQ_API_KEY"];
            options.Model = configuration["GROQ_MODEL"] ?? options.Model;
        });

        services.Configure<LocalLlmOptions>(configuration.GetSection(LocalLlmOptions.SectionName));
        services.PostConfigure<LocalLlmOptions>(options =>
        {
            options.BaseUrl = configuration["LOCAL_LLM_BASE_URL"] ?? options.BaseUrl;
            options.Model = configuration["LOCAL_LLM_MODEL"] ?? options.Model;
            options.ApiStyle = configuration["LOCAL_LLM_API_STYLE"] ?? options.ApiStyle;
        });
    }

    // ── Rate Limiter ──────────────────────────────────────────────────────────

    private static void RegisterRateLimiter(IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitOptions = configuration
            .GetSection(AiRateLimitOptions.SectionName)
            .Get<AiRateLimitOptions>() ?? new AiRateLimitOptions();

        var securityOptions = configuration
            .GetSection(GatewaySecurityOptions.SectionName)
            .Get<GatewaySecurityOptions>() ?? new GatewaySecurityOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("ai-chat", context =>
            {
                var partitionKey = ResolvePartitionKey(context, securityOptions.HeaderName);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, rateLimitOptions.PermitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.WindowSeconds)),
                        QueueLimit = Math.Max(0, rateLimitOptions.QueueLimit),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });
    }

    // ── HttpClients ───────────────────────────────────────────────────────────

    private static void RegisterHttpClients(IServiceCollection services, IConfiguration configuration)
    {
        // 각 Provider가 IHttpClientFactory.CreateClient(name)으로 가져감.
        // Singleton provider가 HttpClient를 직접 보유하지 않아 소켓 재활용이 IHttpClientFactory에 위임됨.
        var openAiOptions = configuration
            .GetSection(OpenAiOptions.SectionName)
            .Get<OpenAiOptions>() ?? new OpenAiOptions();

        services.AddHttpClient("openai", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, openAiOptions.TimeoutSeconds));
        });

        // 추후 Gemini, Groq, LocalLlm 구현 시 동일 패턴으로 named client 추가
    }

    // ── Providers ─────────────────────────────────────────────────────────────

    private static void RegisterProviders(IServiceCollection services)
    {
        // 구현 완료 — IAiChatProvider로 등록
        services.AddSingleton<IAiChatProvider, OpenAiResponsesProvider>();

        // 미구현 — Placeholder로 등록 (IsConfigured=false, 호출 시 에러코드 반환)
        services.AddSingleton<IAiChatProvider>(sp =>
            new PlaceholderAiChatProvider("Gemini",
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeminiOptions>>().Value.Model));

        services.AddSingleton<IAiChatProvider>(sp =>
            new PlaceholderAiChatProvider("Groq",
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GroqOptions>>().Value.Model));

        services.AddSingleton<IAiChatProvider>(sp =>
            new PlaceholderAiChatProvider("LocalLlm",
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LocalLlmOptions>>().Value.Model));

        services.AddSingleton<IAiChatProviderResolver, AiChatProviderResolver>();
    }

    // ── Application Services ──────────────────────────────────────────────────

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<IEmbeddingClient, OpenAiEmbeddingClient>();
        services.AddSingleton<IRetrievedContextRanker, EmbeddingRetrievedContextRanker>();
        services.AddScoped<IAiChatService, AiChatService>();
        services.AddScoped<IAiChatStreamService, AiChatStreamService>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ResolvePartitionKey(HttpContext context, string gatewayKeyHeader)
    {
        if (context.Request.Headers.TryGetValue(gatewayKeyHeader, out var clientKey) &&
            !string.IsNullOrWhiteSpace(clientKey))
        {
            return $"key:{clientKey}";
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
