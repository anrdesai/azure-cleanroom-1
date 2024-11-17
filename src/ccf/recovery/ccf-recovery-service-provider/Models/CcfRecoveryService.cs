// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CcfRecoveryProvider;

public class CcfRecoveryService
{
    public string Name { get; set; } = default!;

    public string InfraType { get; set; } = default!;

    public string Endpoint { get; set; } = default!;

    public string ServiceCert { get; set; } = default!;
}