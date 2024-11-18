// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class AddSnpHostDataInput
{
    public string InfraType { get; set; } = default!;

    // Base64 encoded security policy.
    public string? SecurityPolicy { get; set; } = default!;

    public string HostData { get; set; } = default!;

    public JsonObject? ProviderConfig { get; set; }
}