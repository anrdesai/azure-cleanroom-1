// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AttestationClient;

namespace Controllers;

public class SigningKeyInfo
{
    public string SigningCert { get; set; } = default!;

    public AttestationReport AttestationReport { get; set; } = default!;
}