// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AttestationClient;

namespace Controllers;

public class RecoveryMemberReport
{
    public ReportAndSigningCert SigningKeyReport { get; set; } = default!;

    public ReportAndEncKey EncryptionKeyReport { get; set; } = default!;
}

public class ReportAndSigningCert
{
    public AttestationReport Report { get; set; } = default!;

    public string SigningCert { get; set; } = default!;
}

public class ReportAndEncKey
{
    public AttestationReport Report { get; set; } = default!;

    public string EncryptionPublicKey { get; set; } = default!;
}