using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TransactionAggregationAPI.Application.Categorization;
using TransactionAggregationAPI.Application.Interfaces;
using TransactionAggregationAPI.Application.Services;
using TransactionAggregationAPI.Infrastructure.Cache;
using TransactionAggregationAPI.Infrastructure.Data;
using TransactionAggregationAPI.Infrastructure.Data.Repositories;
using TransactionAggregationAPI.Infrastructure.Sources.SourceA;
using TransactionAggregationAPI.Infrastructure.Sources.SourceB;
using TransactionAggregationAPI.Infrastructure.Sources.SourceC;

namespace TransactionAggregationAPI.API.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
	{
        var connectionString = configuration["ConnectionStrings:Postgres"]
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.ConnectionStringBuilder.MinPoolSize = 10;
        dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = 100;
        dataSourceBuilder.ConnectionStringBuilder.ConnectionLifetime = 300;
        dataSourceBuilder.ConnectionStringBuilder.ConnectionIdleLifetime = 60;
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        services.AddDbContext<TransactionDbContext>(opts =>
            opts.UseNpgsql(dataSource, npgsql =>
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

        var redisConnection = configuration["Redis:ConnectionString"]
           ?? throw new InvalidOperationException("Redis:ConnectionString is required.");
        services.AddStackExchangeRedisCache(opts => opts.Configuration = redisConnection);

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<ITransactionCacheService, RedisCacheService>();
        services.AddSingleton<ICategorizationStrategy, KeywordCategorizationStrategy>();
        services.AddSingleton<ICategorizationService, CategorizationService>();
        services.AddSingleton<ITransactionSource, SourceAAdapter>();
        services.AddSingleton<ITransactionSource, SourceBAdapter>();
        services.AddSingleton<ITransactionSource, SourceCAdapter>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAggregationService, AggregationService>();

        return services;
    }

    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(opts =>
        {
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Per-IP: 100 requests per minute
            opts.AddFixedWindowLimiter("PerIp", o =>
            {
                o.PermitLimit = 100;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });

            // Global: 1000 requests per minute (generous for testing)
            opts.AddFixedWindowLimiter("Global", o =>
            {
                o.PermitLimit = 1000;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");

        services.AddCors(opts => opts.AddPolicy("Default", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()));

        return services;
    }
}
