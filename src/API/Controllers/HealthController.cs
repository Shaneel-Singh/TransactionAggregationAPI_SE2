using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ITransactionCacheService _cacheService;

    public HealthController(HealthCheckService healthCheckService, ITransactionCacheService cacheService)
    {
        _healthCheckService = healthCheckService;
        _cacheService = cacheService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });

    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live() => Ok(new { status = "alive", timestamp = DateTime.UtcNow });

    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        var status = report.Status == HealthStatus.Healthy ? "ready" : "not ready";
        var statusCode = report.Status == HealthStatus.Healthy ? 200 : 503;

        return StatusCode(statusCode, new
        {
            status,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), description = e.Value.Description })
        });
    }

    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Metrics()
    {
        var cacheStats = await _cacheService.GetStatsAsync();
        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            cache = new
            {
                hits = cacheStats.Hits,
                misses = cacheStats.Misses,
                invalidations = cacheStats.Invalidations,
                hitRate = cacheStats.Hits + cacheStats.Misses == 0
                    ? 0.0
                    : Math.Round((double)cacheStats.Hits / (cacheStats.Hits + cacheStats.Misses) * 100, 2)
            }
        });
    }
}
