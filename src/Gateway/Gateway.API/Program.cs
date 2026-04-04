using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

// ── Observability ────────────────────────────────────────────────────
builder.Services.AddObservability("Gateway.API", builder.Configuration);

// ── YARP Reverse Proxy ───────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── Rate Limiting ────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 10;
    });
});

// ── Health Checks ────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRateLimiter();

app.MapReverseProxy();
app.MapHealthChecks("/health");

app.Run();
