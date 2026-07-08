using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace TransactionAggregationAPI.Infrastructure.Resilience;

public static class PollyPolicies
{
    public static ResiliencePipeline CreateSourcePipeline(string sourceName, ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(10),
                OnTimeout = args =>
                {
                    logger.LogWarning("Source {Source} timed out after {Timeout}s", sourceName, args.Timeout.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    logger.LogWarning("Source {Source} retry {Attempt} after {Delay}ms",
                        sourceName, args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnOpened = args =>
                {
                    logger.LogError("Circuit breaker OPENED for source {Source}", sourceName);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("Circuit breaker CLOSED for source {Source}", sourceName);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
