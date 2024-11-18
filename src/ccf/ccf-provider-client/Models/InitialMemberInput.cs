// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class InitialMemberInput
{
    // PEM encoded certificate string.
    public string Certificate { get; set; } = default!;

    // PEM encoded public key string.
    public string? EncryptionPublicKey { get; set; } = default!;

    public JsonObject MemberData { get; set; } = default!;
}