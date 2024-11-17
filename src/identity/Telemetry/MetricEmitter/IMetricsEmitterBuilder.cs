// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Metrics;

public interface IMetricsEmitterBuilder
{
    IMetricsEmitter Build(
        string serviceMeterName,
        Func<Dictionary<string, List<string>>> getMetricsToCreate);
}