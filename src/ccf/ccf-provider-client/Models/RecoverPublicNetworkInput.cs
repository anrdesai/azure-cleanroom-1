// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class RecoverPublicNetworkInput
{
    public string TargetNetworkName { get; set; } = default!;

    public int NodeCount { get; set; } = default!;

    public string InfraType { get; set; } = default!;

    public string? NodeLogLevel { get; set; } = default!;

    public SecurityPolicyConfigInput? SecurityPolicy { get; set; }

    // PEM encoded certificate string.
    public string PreviousServiceCertificate { get; set; } = default!;

    public JsonObject? ProviderConfig { get; set; }
}