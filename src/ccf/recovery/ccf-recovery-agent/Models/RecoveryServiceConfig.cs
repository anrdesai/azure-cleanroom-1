// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public class RecoveryServiceConfig
{
    public string Endpoint { get; set; } = default!;

    public string? ServiceCert { get; set; } = default!;
}