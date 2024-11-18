// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.Azure.IdentitySidecar.Telemetry.Metrics;

namespace Metrics;

public static class MetricsExtensions
{
    public static void Log(this IMetricsEmitter metricsEmitter, IdentityMetric metric)
    {
        if (metric.Value.HasValue)
        {
            metricsEmitter.Log(
                metric.Name,
                metric.Value.Value,
                metric.Dimensions,
                metric.Annotations);
        }
        else
        {
            metricsEmitter.Log(
                metric.Name,
                metric.Dimensions,
                metric.Annotations);
        }
    }
}
