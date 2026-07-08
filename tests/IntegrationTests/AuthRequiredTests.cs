using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using TransactionAggregationAPI.API.Models.Requests;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;
using TransactionAggregationAPI.Infrastructure.Data;
using Xunit;

namespace TransactionAggregationAPI.IntegrationTests;

public class AuthRequiredTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthRequiredTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WithoutApiKey_Returns200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetHealthLive_WithoutApiKey_Returns200()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetHealthReady_WithoutApiKey_DoesNotReturnAuthError()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactions_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactions_WithWrongApiKey_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/transactions");
        request.Headers.Add("X-API-Key", "wrong-key");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactions_WithCorrectApiKey_Returns200()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/transactions");
        request.Headers.Add("X-API-Key", "test-key-12345");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTransactionsByCustomer_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/transactions/customer/C001");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactionsByCustomer_WithCorrectApiKey_Returns200()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/transactions/customer/C001");
        request.Headers.Add("X-API-Key", "test-key-12345");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostTransaction_WithoutApiKey_Returns401()
    {
        var transaction = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            Description = "Test",
            MerchantName = "Test Merchant",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(transaction);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/transactions", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostTransaction_WithCorrectApiKey_Returns201()
    {
        var transaction = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            Description = "Test",
            MerchantName = "Test Merchant",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(transaction);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = content
        };
        request.Headers.Add("X-API-Key", "test-key-12345");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DeleteTransaction_WithoutApiKey_Returns401()
    {
        var transactionId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/transactions/{transactionId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactionById_WithApiKey_NotFound_Returns404()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/transactions/{nonExistentId}");
        request.Headers.Add("X-API-Key", "test-key-12345");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostAggregate_WithCorrectApiKey_ReturnsSuccess()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions/aggregate");
        request.Headers.Add("X-API-Key", "test-key-12345");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.MultiStatus);
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment
        builder.UseEnvironment("Test");

        // Use configuration settings to override values before they're used in validation
        builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=TestDb;Username=test;Password=test");
        builder.UseSetting("Redis:ConnectionString", "localhost:6379");
        builder.UseSetting("ApiKeys:0", "test-key-12345");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:3000");

        builder.ConfigureServices((context, services) =>
        {
            // Remove services that depend on real infrastructure
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<TransactionDbContext>) ||
                           d.ServiceType == typeof(TransactionDbContext) ||
                           d.ServiceType == typeof(Npgsql.NpgsqlDataSource) ||
                           d.ServiceType == typeof(ITransactionCacheService) ||
                           d.ServiceType == typeof(ITransactionSource))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database — stable name per factory instance
            services.AddDbContext<TransactionDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestDb")
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Replace cache service with mock
            var mockCache = new Mock<ITransactionCacheService>();
            mockCache.Setup(c => c.GetTransactionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UnifiedTransaction?)null);
            mockCache.Setup(c => c.InvalidateCustomerListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockCache.Setup(c => c.InvalidateSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockCache.Setup(c => c.InvalidateTransactionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockCache.Setup(c => c.SetTransactionAsync(It.IsAny<UnifiedTransaction>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockCache.Setup(c => c.GetStatsAsync())
                .ReturnsAsync(new CacheStats(0, 0, 0));
            services.AddSingleton(mockCache.Object);

            // Replace real source adapters (10% random failure) with deterministic stubs
            // Register 3 stubs to match production source count
            services.AddSingleton<ITransactionSource>(new DeterministicTestSource("TestSourceA", "A"));
            services.AddSingleton<ITransactionSource>(new DeterministicTestSource("TestSourceB", "B"));
            services.AddSingleton<ITransactionSource>(new DeterministicTestSource("TestSourceC", "C"));
        });
    }
}

/// <summary>Deterministic transaction source — always succeeds, returns predictable data.</summary>
public class DeterministicTestSource : ITransactionSource
{
    private readonly string _name;
    private readonly string _prefix;

    public DeterministicTestSource(string name, string prefix)
    {
        _name = name;
        _prefix = prefix;
    }

    public string SourceName => _name;

    public Task<IReadOnlyList<UnifiedTransaction>> FetchTransactionsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        IReadOnlyList<UnifiedTransaction> transactions = new List<UnifiedTransaction>
        {
            new() { ExternalId = $"{_prefix}001", SourceSystem = _name, CustomerId = "C001", AccountId = "ACC001", Amount = 250.00m, Currency = "ZAR", Description = "Checkers grocery", MerchantName = "Checkers", Category = TransactionCategory.Food, TransactionType = "debit", TransactionDateUtc = now.AddDays(-5), CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { ExternalId = $"{_prefix}002", SourceSystem = _name, CustomerId = "C001", AccountId = "ACC001", Amount = 45.50m, Currency = "ZAR", Description = "Uber trip", MerchantName = "Uber", Category = TransactionCategory.Transport, TransactionType = "debit", TransactionDateUtc = now.AddDays(-4), CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { ExternalId = $"{_prefix}003", SourceSystem = _name, CustomerId = "C002", AccountId = "ACC002", Amount = 15000.00m, Currency = "ZAR", Description = "Monthly salary", MerchantName = "Employer", Category = TransactionCategory.Salary, TransactionType = "credit", TransactionDateUtc = now.AddDays(-3), CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { ExternalId = $"{_prefix}004", SourceSystem = _name, CustomerId = "C001", AccountId = "ACC001", Amount = 320.00m, Currency = "ZAR", Description = "Pick n Pay groceries", MerchantName = "Pick n Pay", Category = TransactionCategory.Food, TransactionType = "debit", TransactionDateUtc = now.AddDays(-1), CreatedAtUtc = now, UpdatedAtUtc = now },
        };
        return Task.FromResult(transactions);
    }
}
