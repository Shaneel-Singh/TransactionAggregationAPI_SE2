using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TransactionAggregationAPI.API.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private readonly HashSet<string> _validKeys;

    private static readonly string[] PublicPaths =
    [
        "/health", "/health/live", "/health/ready", "/health/metrics",
        "/swagger"
    ];

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration config, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var keys = config.GetSection("ApiKeys").Get<string[]>() ?? [];
        if (keys.Length == 0)
            throw new InvalidOperationException("ApiKeys configuration must contain at least one key.");
        _validKeys = [.. keys];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var keyHeader) || string.IsNullOrWhiteSpace(keyHeader))
        {
            _logger.LogWarning("Request to {Path} rejected: missing X-API-Key header", path);
            await WriteUnauthorized(context, "X-API-Key header is required.");
            return;
        }

        var key = keyHeader.ToString();
        if (!_validKeys.Contains(key))
        {
            var masked = MaskKey(key);
            _logger.LogWarning("Request to {Path} rejected: invalid API key {MaskedKey}", path, masked);
            await WriteUnauthorized(context, "Invalid API key.");
            return;
        }

        var maskedForLog = MaskKey(key);
        _logger.LogDebug("Authenticated request to {Path} with key {MaskedKey}", path, maskedForLog);
        await _next(context);
    }

    private static string MaskKey(string key)
    {
        if (key.Length <= 8) return "****";
        return $"{key[..4]}...{key[^4..]}";
    }

    private static async Task WriteUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Unauthorized",
            status = 401,
            detail = message
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
