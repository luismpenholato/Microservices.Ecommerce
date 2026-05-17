using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace BuildingBlocks.Observability;

public static class WorkerOperationalHostExtensions
{
    public static WebApplicationBuilder AddWorkerOperationalHost(
        this WebApplicationBuilder builder,
        string serviceName,
        Action<IHealthChecksBuilder>? configureReadyChecks = null)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {CorrelationId} {Message:lj}{NewLine}{Exception}")
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("MassTransit", LogEventLevel.Warning);
        });

        builder.Services.AddEcommerceOperationalMetrics(serviceName);
        ObservabilityExtensions.ConfigureOpenTelemetry(builder, serviceName);

        var healthBuilder = builder.Services.AddHealthChecks().AddLiveCheck();
        configureReadyChecks?.Invoke(healthBuilder);

        return builder;
    }

    public static WebApplication MapWorkerOperationalEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new() { Predicate = check => check.Tags.Contains("live") });
        app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
        app.MapPrometheusScrapingEndpoint("/metrics");
        return app;
    }
}
