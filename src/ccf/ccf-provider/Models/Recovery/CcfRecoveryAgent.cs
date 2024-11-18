// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfProvider;

public class CcfRecoveryAgent
{
    public string Name { get; set; } = default!;

    public string Endpoint { get; set; } = default!;

    public string ServiceCert { get; set; } = default!;
}