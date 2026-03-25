using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Snip.Shared.Telemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddSnipTracing(
        this IServiceCollection services,
        string serviceName,
        string otlpEndpoint = "http://localhost:4317",
        bool isWebService = true)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName))
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });

                if (isWebService)
                {
                    tracing.AddAspNetCoreInstrumentation();
                }
            });

        return services;
    }
}