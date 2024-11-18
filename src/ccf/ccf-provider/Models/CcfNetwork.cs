// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public class CcfNetwork
{
    public string Name { get; set; } = default!;

    public string InfraType { get; set; } = default!;

    public int NodeCount { get; set; } = default!;

    public string Endpoint { get; set; } = default!;

    public List<string> Nodes { get; set; } = default!;
}