// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Identity.Configuration;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Utilities;

internal enum OtelExporter
{
    /// <summary>
    /// Represents Console exporter.
    /// </summary>
    Console,

    /// <summary>
    /// Represents a generic exporter.
    /// </summary>
    Generic,
}

public static class OpenTelemetryUtilities
{
    public static MeterProviderBuilder AddExporters(
        this MeterProviderBuilder builder,
        IConfiguration config)
    {
        var diagnosticsConfig = config.GetDiagnosticsConfiguration();
        foreach (var exporter in diagnosticsConfig.Metrics.Exporters)
        {
            switch (exporter.Type)
            {
                case nameof(OtelExporter.Console):
                    builder.AddConsoleExporter();
                    break;

                case nameof(OtelExporter.Generic):
                    builder.AddOtlpExporter(
                        options => options.Endpoint = new Uri(exporter.Endpoint));
                    break;
            }
        }

        return builder;
    }

    public static OpenTelemetryLoggerOptions AddExporters(
        this OpenTelemetryLoggerOptions loggerOptions,
        IConfiguration config)
    {
        var diagnosticsConfig = config.GetDiagnosticsConfiguration();
        foreach (var exporter in diagnosticsConfig.Logs.Exporters)
        {
            switch (exporter.Type)
            {
                case nameof(OtelExporter.Console):
                    loggerOptions.AddConsoleExporter();
                    break;

                case nameof(OtelExporter.Generic):
                    loggerOptions.AddOtlpExporter(
                        configure: (options) =>
                        {
                            options.Endpoint = new Uri(exporter.Endpoint);
                        });
                    break;
            }
        }

        return loggerOptions;
    }

    public static TracerProviderBuilder AddExporters(
        this TracerProviderBuilder builder,
        IConfiguration config)
    {
        var diagnosticsConfig = config.GetDiagnosticsConfiguration();
        foreach (var exporter in diagnosticsConfig.Traces.Exporters)
        {
            switch (exporter.Type)
            {
                case nameof(OtelExporter.Console):
                    builder = builder.AddConsoleExporter();
                    break;

                case nameof(OtelExporter.Generic):
                    builder = builder.AddOtlpExporter(
                        options => options.Endpoint = new Uri(exporter.Endpoint));
                    break;
            }
        }

        return builder;
    }
}