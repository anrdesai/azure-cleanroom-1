// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class UpdateNetworkInput
{
    public int NodeCount { get; set; } = default!;

    public string InfraType { get; set; } = default!;

    public string? NodeLogLevel { get; set; } = default!;

    public SecurityPolicyConfigInput? SecurityPolicy { get; set; }

    public JsonObject? ProviderConfig { get; set; }
}