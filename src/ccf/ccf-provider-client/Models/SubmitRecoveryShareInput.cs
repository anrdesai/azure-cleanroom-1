// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class SubmitRecoveryShareInput
{
    public string InfraType { get; set; } = default!;

    // PEM encoded certificate string.
    public string SigningCert { get; set; } = default!;

    // PEM encoded private key string.
    public string SigningKey { get; set; } = default!;

    // PEM encoded private key string.
    public string EncryptionPrivateKey { get; set; } = default!;

    public JsonObject? ProviderConfig { get; set; }
}