var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy" }))
    .WithName("HealthCheck")
    .WithOpenApi();

// Readiness check endpoint
app.MapGet("/readyz", () => Results.Ok(new { status = "Ready" }))
    .WithName("ReadinessCheck")
    .WithOpenApi();

// Root endpoint
app.MapGet("/", () => Results.Ok(new { 
    application = "Naglfar Analytics",
    description = "The ship made of dead men's nails. A bit darker, but represents collection and analysis of threat data.",
    version = "1.0.0"
}))
.WithName("Root")
.WithOpenApi();

app.Run();
