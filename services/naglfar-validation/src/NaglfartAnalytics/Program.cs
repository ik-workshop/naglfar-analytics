using Prometheus;
using StackExchange.Redis;
using NaglfartAnalytics.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.QueryStringApiVersionReader("api-version"),
        new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"),
        new Asp.Versioning.UrlSegmentApiVersionReader());
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure Swagger with API versioning
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Naglfar Analytics API",
        Version = "v1",
        Description = "The ship made of dead men's nails. A bit darker, but represents collection and analysis of threat data."
    });
});

// Add health checks
builder.Services.AddHealthChecks();

// Add Redis connection
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false; // Don't fail if Redis is unavailable
    return ConnectionMultiplexer.Connect(configuration);
});

// Register Redis publisher service
builder.Services.AddSingleton<IRedisPublisher, RedisPublisher>();

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Naglfar Analytics API v1");
    });
}

// Prometheus metrics middleware
app.UseHttpMetrics();

// Authentication middleware - check for auth cookie and create E-TOKEN if needed
// This runs BEFORE YARP proxy to ensure all proxied requests are authenticated
app.UseMiddleware<NaglfartAnalytics.AuthenticationMiddleware>();

// Health check endpoints (unversioned - infrastructure)
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy" }))
    .WithName("HealthCheck")
    .ExcludeFromDescription();

app.MapGet("/readyz", () => Results.Ok(new { status = "Ready" }))
    .WithName("ReadinessCheck")
    .ExcludeFromDescription();

// Prometheus metrics endpoint (infrastructure)
app.MapMetrics();

// API v1 endpoints
var v1 = app.NewVersionedApi("Naglfar Analytics API");
var v1Group = v1.MapGroup("/api/v{version:apiVersion}").HasApiVersion(1, 0);

v1Group.MapGet("/info", () => Results.Ok(new {
    application = "Naglfar Analytics",
    description = "The ship made of dead men's nails. A bit darker, but represents collection and analysis of threat data.",
    version = "1.0.0",
    apiVersion = "1.0"
}))
.WithName("GetApplicationInfo")
.WithSummary("Get application information")
.WithDescription("Returns application metadata including name, description, and version");

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();

// Make the implicit Program class public for WebApplicationFactory<Program> in tests
public partial class Program { }
