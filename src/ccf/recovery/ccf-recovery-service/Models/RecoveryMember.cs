// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Controllers;

public class RecoveryMember
{
    // PEM encoded string.
    public string SigningCert { get; set; } = default!;

    // PEM encoded string.
    public string EncryptionPublicKey { get; set; } = default!;

    public RecoveryServiceEnvironmentInfo RecoveryService { get; set; } = default!;

    public class RecoveryServiceEnvironmentInfo
    {
        public string HostData { get; set; } = default!;
    }
}