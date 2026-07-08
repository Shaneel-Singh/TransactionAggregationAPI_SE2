using Microsoft.AspNetCore.Mvc;
using TransactionAggregationAPI.API.Models.Responses;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.API.Controllers;

[ApiController]
[Route("api/transactions")]
public class AggregationController : ControllerBase
{
    private readonly IAggregationService _aggregationService;
    private readonly ILogger<AggregationController> _logger;

    public AggregationController(IAggregationService aggregationService, ILogger<AggregationController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    [HttpPost("aggregate")]
    public async Task<IActionResult> Aggregate(CancellationToken ct)
    {
        _logger.LogInformation("Aggregation triggered via API");
        var result = await _aggregationService.AggregateAsync(ct);

        var response = new AggregationResponse
        {
            TotalFetched = result.TotalFetched,
            TotalUpserted = result.TotalUpserted,
            SourceResults = result.SourceResults.Select(r => new SourceResultResponse
            {
                SourceName = r.SourceName,
                Success = r.Success,
                Count = r.Count,
                ErrorMessage = r.ErrorMessage,
                DurationMs = r.Duration.TotalMilliseconds
            }).ToList()
        };

        var statusCode = result.SourceResults.All(r => r.Success) ? 200 : 207;
        return StatusCode(statusCode, response);
    }
}
