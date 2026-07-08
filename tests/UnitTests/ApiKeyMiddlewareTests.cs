using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TransactionAggregationAPI.API.Middleware;
using Xunit;

namespace TransactionAggregationAPI.UnitTests;

public class ApiKeyMiddlewareTests
{
    private readonly Mock<ILogger<ApiKeyAuthMiddleware>> _mockLogger;

    public ApiKeyMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ApiKeyAuthMiddleware>>();
    }

    [Fact]
    public async Task MissingApiKeyHeader_Returns401()
    {
        var middleware = CreateMiddleware(new[] { "test-key-123" });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transactions";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task EmptyApiKeyHeader_Returns401()
    {
        var middleware = CreateMiddleware(new[] { "test-key-123" });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transactions";
        context.Request.Headers["X-API-Key"] = "";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task WhitespaceApiKeyHeader_Returns401()
    {
        var middleware = CreateMiddleware(new[] { "test-key-123" });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transactions";
        context.Request.Headers["X-API-Key"] = "   ";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvalidApiKey_Returns401()
    {
        var middleware = CreateMiddleware(new[] { "valid-key-123" });
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transactions";
        context.Request.Headers["X-API-Key"] = "invalid-key-456";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task ValidApiKey_PassesThrough()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(new[] { "valid-key-123" });
        var middleware = new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transactions";
        context.Request.Headers["X-API-Key"] = "valid-key-123";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task HealthEndpoint_NoApiKeyNeeded_PassesThrough()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(new[] { "valid-key-123" });
        var middleware = new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task HealthLiveEndpoint_NoApiKeyNeeded_PassesThrough()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(new[] { "valid-key-123" });
        var middleware = new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task HealthReadyEndpoint_NoApiKeyNeeded_PassesThrough()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(new[] { "valid-key-123" });
        var middleware = new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/ready";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task HealthMetricsEndpoint_NoApiKeyNeeded_PassesThrough()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(new[] { "valid-key-123" });
        var middleware = new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/metrics";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task MultipleValidKeys_FirstKeyWorks()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(new[] { "key-1", "key-2", "key-3" });
        var middleware = new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transactions";
        context.Request.Headers["X-API-Key"] = "key-1";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task MultipleValidKeys_SecondKeyWorks()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(new[] { "key-1", "key-2", "key-3" });
        var middleware = new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transactions";
        context.Request.Headers["X-API-Key"] = "key-2";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task MultipleValidKeys_ThirdKeyWorks()
    {
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(new[] { "key-1", "key-2", "key-3" });
        var middleware = new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/transactions";
        context.Request.Headers["X-API-Key"] = "key-3";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public void MissingApiKeysConfig_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder().Build();
        RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;

        var act = () => new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one key*");
    }

    [Fact]
    public void EmptyApiKeysArray_ThrowsInvalidOperationException()
    {
        var config = CreateConfiguration(Array.Empty<string>());
        RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;

        var act = () => new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one key*");
    }

    [Fact]
    public async Task InvalidApiKey_ResponseContainsProblemDetails()
    {
        var middleware = CreateMiddleware(new[] { "valid-key-123" });
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/transactions";
        context.Request.Headers["X-API-Key"] = "invalid-key";

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        body.Should().Contain("Unauthorized");
        body.Should().Contain("Invalid API key");
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task MissingApiKey_ResponseContainsProblemDetails()
    {
        var middleware = CreateMiddleware(new[] { "valid-key-123" });
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/transactions";

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        body.Should().Contain("Unauthorized");
        body.Should().Contain("X-API-Key header is required");
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    private ApiKeyAuthMiddleware CreateMiddleware(string[] keys)
    {
        RequestDelegate next = (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var config = CreateConfiguration(keys);
        return new ApiKeyAuthMiddleware(next, config, _mockLogger.Object);
    }

    private IConfiguration CreateConfiguration(string[] keys)
    {
        var configData = new Dictionary<string, string?>();
        for (int i = 0; i < keys.Length; i++)
        {
            configData[$"ApiKeys:{i}"] = keys[i];
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}
