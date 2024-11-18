// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace CcfProvider;

public class InitialMember
{
    public string Certificate { get; set; } = default!;

    public string? EncryptionPublicKey { get; set; } = default!;

    public JsonObject MemberData { get; set; } = default!;
}