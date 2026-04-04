using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Shared.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        string serviceName,
        IConfiguration configuration)
    {
        var seqEndpoint = configuration["Observability:SeqEndpoint"] ?? "http://localhost:5341";

        // ── OpenTelemetry: Traces + Metrics ──────────────────────────────
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(serviceName)
                    .AddSource("MassTransit")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{seqEndpoint}/ingest/otlp/v1/traces");
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(serviceName)
                    .AddMeter("MassTransit")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{seqEndpoint}/ingest/otlp/v1/metrics");
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
            });

        // ── Serilog: Structured Logging ──────────────────────────────────
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} | {Message:lj}{NewLine}{Exception}")
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = $"{seqEndpoint}/ingest/otlp/v1/logs";
                options.Protocol = OtlpProtocol.HttpProtobuf;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName
                };
            })
            .CreateLogger();

        services.AddSerilog();

        return services;
    }
}
