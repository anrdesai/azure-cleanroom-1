// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Metrics;

public class MetricsEmitterBuilder : IMetricsEmitterBuilder
{
    private MetricsEmitterBuilder()
    {
    }

    public static IMetricsEmitterBuilder CreateBuilder()
    {
        return new MetricsEmitterBuilder();
    }

    /// <inheritdoc/>
    public IMetricsEmitter Build(
        string serviceMeterName,
        Func<Dictionary<string, List<string>>> getMetricsToCreate)
    {
        return new MeterMetricsEmitter(
            serviceMeterName,
            getMetricsToCreate);
    }
}
