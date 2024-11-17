// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Identity.Configuration;

/// <summary>
/// Class defining the Diagnostics configuration.
/// </summary>
public class DiagnosticsConfiguration
{
    /// <summary>
    /// Gets or sets the Logs configuration.
    /// </summary>
    public LogsConfiguration Logs { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Traces configuration.
    /// </summary>
    public TracesConfiguration Traces { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Metrics configuration.
    /// </summary>
    public MetricsConfiguration Metrics { get; set; } = default!;
}

/// <summary>
/// Class defining the Logs configuration.
/// </summary>
public class LogsConfiguration
{
    /// <summary>
    /// Gets or sets the exporters for logging.
    /// </summary>
    public List<Exporter> Exporters { get; set; } = new List<Exporter>();
}

/// <summary>
/// Class defining the Metrics configuration.
/// </summary>
public class TracesConfiguration
{
    /// <summary>
    /// Gets or sets the exporters for traces.
    /// </summary>
    public List<Exporter> Exporters { get; set; } = new List<Exporter>();
}

/// <summary>
/// Class defining the Metrics configuration.
/// </summary>
public class MetricsConfiguration
{
    /// <summary>
    /// Gets or sets the exporters for metrics.
    /// </summary>
    public List<Exporter> Exporters { get; set; } = new List<Exporter>();
}

/// <summary>
/// Class defining the exporter.
/// </summary>
public class Exporter
{
    /// <summary>
    /// Gets or sets the type of exporter.
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Gets or sets the endpoint for the exporter.
    /// </summary>
    public string Endpoint { get; set; } = default!;
}