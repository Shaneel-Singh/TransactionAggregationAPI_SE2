# =============================================================================
# Stage 1: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY TransactionAggregationAPI.sln .
COPY src/API/TransactionAggregationAPI.API.csproj src/API/
COPY src/Application/TransactionAggregationAPI.Application.csproj src/Application/
COPY src/Infrastructure/TransactionAggregationAPI.Infrastructure.csproj src/Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy remaining source
COPY src/ src/

# Build and publish
RUN dotnet publish src/API/TransactionAggregationAPI.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# =============================================================================
# Stage 2: Runtime
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Security: create and use a non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy published output
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

ENTRYPOINT ["dotnet", "TransactionAggregationAPI.API.dll"]
