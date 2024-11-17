// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public class ContainerGroupSecurityPolicy
{
    public Dictionary<string, string> Images { get; set; } = default!;

    public string ConfidentialComputeCcePolicy { get; set; } = default!;
}