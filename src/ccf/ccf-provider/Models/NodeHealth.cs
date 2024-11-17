// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public enum NodeStatus
{
    /// <summary>
    /// Nothing from the Node provider side is considered as an issue.
    /// </summary>
    Ok,

    /// <summary>
    /// Node will no longer function as part of the CCF network. Should be replaced.
    /// </summary>
    NeedsReplacement
}

public class NodeHealth
{
    public string Name { get; set; } = default!;

    public string Endpoint { get; set; } = default!;

    // NodeStatus string value.
    public string Status { get; set; } = default!;

    public List<Reason> Reasons { get; set; } = default!;
}

public class Reason
{
    public string Code { get; set; } = default!;

    public string Message { get; set; } = default!;
}
