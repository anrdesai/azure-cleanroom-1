// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AttestationClient;

namespace Controllers;

public class WorkspaceConfiguration
{
    public string CcfEndpoint { get; set; } = default!;

    public string? CcfEndpointCert { get; set; } = default!;

    public bool CcfEndpointSkipTlsVerify { get; set; } = default!;

    public string? CcfRecoverySvcEndpoint { get; set; } = default!;

    public string? CcfRecoverySvcEndpointCert { get; set; } = default!;

    public bool CcfRecoverySvcEndpointSkipTlsVerify { get; set; } = default!;

    public AttestationReportKey Attestation { get; set; } = default!;
}