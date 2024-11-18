// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Identity.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Utilities;

namespace IdentitySidecar;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.BuildConfiguration(builder.Environment, args);

        // Migrating away from using Startup class as per link below.
        // https://andrewlock.net/exploring-dotnet-6-part-12-upgrading-a-dotnet-5-startup-based-app-to-dotnet-6/#option-2-re-use-your-startup-class
        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("identity"))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter()
            .AddExporters(builder.Configuration))
        .WithTracing(tracing => tracing
            .AddSource("Microsoft.Azure.CleanRoomSidecar.Identity")
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .SetSampler(new AlwaysOnSampler())
            .AddExporters(builder.Configuration));
        var app = builder.Build();

        startup.Configure(app, app.Environment);

        app.Run();
    }
}
