// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AttestationClient;

namespace Controllers;

public class EncryptionKeyInfo
{
    public string EncryptionPublicKey { get; set; } = default!;

    public AttestationReport AttestationReport { get; set; } = default!;
}