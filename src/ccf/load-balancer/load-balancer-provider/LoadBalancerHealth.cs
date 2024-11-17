// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LoadBalancerProvider;

public enum LbStatus
{
    /// <summary>
    /// Nothing from the LB provider side is considered as an issue.
    /// </summary>
    Ok,

    /// <summary>
    /// LB is not functioning as part of the CCF network. Should be replaced.
    /// </summary>
    NeedsReplacement
}

public class LoadBalancerHealth
{
    public string Name { get; set; } = default!;

    public string Endpoint { get; set; } = default!;

    // LbStatus string value.
    public string Status { get; set; } = default!;

    public List<Reason> Reasons { get; set; } = default!;
}

public class Reason
{
    public string Code { get; set; } = default!;

    public string Message { get; set; } = default!;
}
