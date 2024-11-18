// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Identity.Configuration;
using Identity.CredentialManager;
using IdentitySidecar.Filters;
using Metrics;
using Microsoft.Azure.IdentitySidecar.Telemetry.Metrics;
using OpenTelemetry.Resources;
using Utilities;

namespace IdentitySidecar;

/// <summary>
/// The startup class.
/// </summary>
internal class Startup
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;
    private readonly IMetricsEmitter metricsEmitter;
    private IConfiguration configuration;

    public Startup(IConfiguration config)
    {
        this.metricsEmitter = MetricsEmitterBuilder.CreateBuilder().Build(
            Constants.Metrics.ServiceMeterName,
            () => IdentityMetric.Enumerate());

        this.loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-ddThh:mm:ssZ ";
                options.UseUtcTimestamp = true;
            });
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("identity"));
                options.AddExporters(config);
            });
        });
        this.logger = this.loggerFactory.CreateLogger("identity");
        this.configuration = config;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add(typeof(ExceptionFilter));
            options.Filters.Add(typeof(GlobalActionFilter));
            options.Filters.Add(typeof(RequestResponseLoggingFilter));
        });

        services.AddSingleton(this.logger);
        services.AddSingleton(this.metricsEmitter);

        var identityConfig = this.configuration.GetIdentityConfiguration()!;
        var diagnosticsConfig = this.configuration.GetDiagnosticsConfiguration()!;
        this.logger.LogInformation(
            $"Starting Identity Sidecar with Configuration:" +
            $"{identityConfig.SafeToString()}");

        this.logger.LogInformation(
            $"Starting Identity Sidecar with Diagnostics Configuration:" +
            $"{diagnosticsConfig.SafeToString()}");

        var credManager = new CredentialManager(identityConfig, this.logger);
        services.AddSingleton(credManager);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        this.metricsEmitter.Log(IdentityMetric.RoleStart());
    }
}