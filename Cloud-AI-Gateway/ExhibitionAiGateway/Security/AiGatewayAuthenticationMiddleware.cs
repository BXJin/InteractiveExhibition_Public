using System.Security.Cryptography;
using System.Text;
using ExhibitionAiGateway.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionAiGateway.Security;

public sealed class AiGatewayAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewaySecurityOptions _options;

    public AiGatewayAuthenticationMiddleware(
        RequestDelegate next,
        IOptions<GatewaySecurityOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresGatewayAuthentication(context.Request))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ClientKey))
        {
            await _next(context);
            return;
        }

        var suppliedKey = ReadSuppliedKey(context.Request);
        if (IsMatch(suppliedKey, _options.ClientKey))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            errorCode = "AI_GATEWAY_UNAUTHORIZED",
            reply = "AI Gateway authentication failed."
        });
    }

    private string? ReadSuppliedKey(HttpRequest request)
    {
        if (request.Headers.TryGetValue(_options.HeaderName, out var headerValue))
        {
            return headerValue.ToString();
        }

        var authorization = request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";

        return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..]
            : null;
    }

    private static bool RequiresGatewayAuthentication(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api/ai", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMatch(string? suppliedKey, string expectedKey)
    {
        if (string.IsNullOrWhiteSpace(suppliedKey))
        {
            return false;
        }

        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);

        return suppliedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }
}
