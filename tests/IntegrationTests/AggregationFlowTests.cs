using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;
using TransactionAggregationAPI.API.Models.Requests;
using Xunit;

namespace TransactionAggregationAPI.IntegrationTests;

public class AggregationFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;
    private const string ApiKey = "test-key-12345";

    public AggregationFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostAggregate_ReturnsSuccessStatusCode()
    {
        var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.MultiStatus);
    }

    [Fact]
    public async Task PostAggregate_ResponseHasSourceResults()
    {
        var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AggregationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.SourceResults.Should().NotBeNull();
        result.SourceResults.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task PostAggregate_EachSourceResultHasRequiredFields()
    {
        var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AggregationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        foreach (var sourceResult in result!.SourceResults)
        {
            sourceResult.SourceName.Should().NotBeNullOrEmpty();
            sourceResult.Should().Match<SourceResultDto>(sr =>
                sr.Success == true || sr.Success == false);
            sourceResult.DurationMs.Should().BeGreaterOrEqualTo(0);
        }
    }

    [Fact]
    public async Task AfterAggregate_GetTransactions_ReturnsTransactions()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get transactions
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions");
        var response = await _client.SendAsync(getRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task AfterAggregate_GetTransactionsByCustomer_ReturnsCustomerTransactions()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get customer transactions
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions/customer/C001");
        var response = await _client.SendAsync(getRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task AfterAggregate_GetCustomerSummary_ReturnsSummary()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get summary
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions/customer/C001/summary");
        var response = await _client.SendAsync(getRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SummaryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AfterAggregate_Summary_HasTotalCountGreaterThanZero()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get summary
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions/customer/C001/summary");
        var response = await _client.SendAsync(getRequest);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SummaryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AfterAggregate_Summary_HasTotalAmount()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get summary
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions/customer/C001/summary");
        var response = await _client.SendAsync(getRequest);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SummaryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.TotalAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AfterAggregate_Summary_HasCountByCategory()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get summary
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions/customer/C001/summary");
        var response = await _client.SendAsync(getRequest);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SummaryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.CountByCategory.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTransactionsWithPageSize5_ReturnsMax5Items()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get with page size 5
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions?pageSize=5");
        var response = await _client.SendAsync(getRequest);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Items.Count.Should().BeLessOrEqualTo(5);
    }

    [Fact]
    public async Task GetTransactionsByCategory_ReturnsOnlyMatchingCategory()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get Food transactions
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions?category=Food");
        var response = await _client.SendAsync(getRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        if (result!.Items.Count > 0)
        {
            result.Items.Should().OnlyContain(t => t.Category == "Food");
        }
    }

    [Fact]
    public async Task Pagination_TotalPagesCalculatedCorrectly()
    {
        // First aggregate
        var aggregateRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/transactions/aggregate");
        await _client.SendAsync(aggregateRequest);

        // Then get with page size 5
        var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/transactions?pageSize=5");
        var response = await _client.SendAsync(getRequest);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        var expectedTotalPages = (int)Math.Ceiling((double)result!.TotalCount / 5);
        result.TotalPages.Should().Be(expectedTotalPages);
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-API-Key", ApiKey);
        return request;
    }

    // DTOs for deserialization
    private class AggregationResponse
    {
        public int TotalFetched { get; set; }
        public int TotalUpserted { get; set; }
        public List<SourceResultDto> SourceResults { get; set; } = new();
    }

    private class SourceResultDto
    {
        public string SourceName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int Count { get; set; }
        public string? ErrorMessage { get; set; }
        public double DurationMs { get; set; }
    }

    private class PaginatedResponse
    {
        public List<TransactionDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    private class TransactionDto
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    private class SummaryResponse
    {
        public string CustomerId { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public Dictionary<string, int> CountByCategory { get; set; } = new();
        public Dictionary<string, decimal> AmountByCategory { get; set; } = new();
    }
}
