using NaglfartEventConsumer.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("redis_event_consumer", () => HealthCheckResult.Healthy("Redis Event Consumer is running"));

// Register the Redis Event Consumer background service
builder.Services.AddHostedService<RedisEventConsumer>();

var host = builder.Build();
host.Run();
