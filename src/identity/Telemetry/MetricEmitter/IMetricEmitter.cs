// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Specialized;

namespace Metrics;

public interface IMetricsEmitter
{
    void Log(string metricName, OrderedDictionary dimensions, OrderedDictionary annotations);

    void Log(
        string metricName,
        long logValue,
        OrderedDictionary dimensions,
        OrderedDictionary annotations);
}
