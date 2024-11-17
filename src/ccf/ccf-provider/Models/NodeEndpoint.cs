// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public class NodeEndpoint
{
    public string NodeName { get; set; } = default!;

    public string ClientRpcAddress { get; set; } = default!;

    public string NodeEndorsedRpcAddress { get; set; } = default!;
}