using FluentValidation;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using Serilog.Events;
using TransactionAggregationAPI.API.Extensions;
using TransactionAggregationAPI.API.Middleware;
using TransactionAggregationAPI.API.Models.Requests;
using TransactionAggregationAPI.API.Validators;
using TransactionAggregationAPI.Infrastructure.Data;

// Bootstrap logger � replaced after config is loaded

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TransactionAggregationAPI");
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TransactionAggregationAPI")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    //Startup validation � fail fast on missing config
    ValidateRequiredConfig(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new OpenApiInfo { Title = "Transaction Aggregation API", Version = "v1" });
        opts.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-API-Key",
            Description = "API key required for all transaction endpoints"
        });
        opts.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                },
                []
            }
        });
    });
    builder.Services.AddApplicationServices(builder.Configuration);

    var connStr = builder.Configuration["ConnectionStrings:Postgres"]!;
    var redisConn = builder.Configuration["Redis:ConnectionString"]!;
    builder.Services.AddHealthChecks()
        .AddNpgSql(sp => sp.GetRequiredService<Npgsql.NpgsqlDataSource>(), "select 1;", null, "postgres")
        .AddRedis(redisConn, name: "redis");

    builder.Services.AddScoped<IValidator<CreateTransactionRequest>, CreateTransactionRequestValidator>();
    builder.Services.AddScoped<IValidator<GetTransactionsRequest>, GetTransactionsRequestValidator>();
    builder.Services.AddRateLimitingPolicies();
    builder.Services.AddCorsPolicy(builder.Configuration);

    var app = builder.Build();

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseCors("Default");
    app.UseRateLimiter();
    app.UseSwagger();
    app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction Aggregation API v1"));
    app.UseMiddleware<ApiKeyAuthMiddleware>();
    app.MapControllers();

    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "TransactionAggregationAPI terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
static void ValidateRequiredConfig(IConfiguration config)
    {
        var required = new[]
        {
            "ConnectionStrings:Postgres",
            "Redis:ConnectionString",
            "ApiKeys",
            "Cors:AllowedOrigins"
        };

    var missing = required.Where(key =>
    {
        var val = config[key];
        if (!string.IsNullOrWhiteSpace(val)) return false;
        var section = config.GetSection(key);
        return !section.Exists() || !section.GetChildren().Any();
    }).ToList();

    if (missing.Count > 0)
        throw new InvalidOperationException(
            $"Missing required configuration: {string.Join(", ", missing)}. " +
            "Set these via environment variables.");
}

// Make Program class accessible for integration tests
public partial class Program { }

