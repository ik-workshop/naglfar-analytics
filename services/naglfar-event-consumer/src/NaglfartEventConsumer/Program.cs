using NaglfartEventConsumer.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 8080 for metrics
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // Metrics endpoint
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("redis_event_consumer", () => HealthCheckResult.Healthy("Redis Event Consumer is running"));

// Register Neo4j service as singleton
builder.Services.AddSingleton<Neo4jService>();

// Register the Redis Event Consumer background service
builder.Services.AddHostedService<RedisEventConsumer>();

var app = builder.Build();

// Expose Prometheus metrics endpoint at /metrics
app.MapMetrics();

// Health check endpoints
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

app.Run();
