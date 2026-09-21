using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Ledger.API.Extensions;

public static class ObservabilityExtensions
{
    public static void AddObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: "cypherly.ledger.svc",
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(b => b
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter())
            .WithMetrics(b => b
                .AddRuntimeInstrumentation()
                .AddNpgsqlInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());
    }

    public static void AddLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.AddOtlpExporter();
        });

        // Always log to stdout so logs show up in `kubectl logs` / k9s, not only in the OTLP backend
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.IncludeScopes = true;
            o.TimestampFormat = builder.Environment.IsDevelopment() ? "HH:mm:ss " : "yyyy-MM-ddTHH:mm:ss.fffZ ";
            o.UseUtcTimestamp = !builder.Environment.IsDevelopment();
        });
    }
}
