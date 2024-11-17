// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class ConfigureConfidentialRecoveryInput
{
    public string InfraType { get; set; } = default!;

    public string RecoveryServiceName { get; set; } = default!;

    public string RecoveryMemberName { get; set; } = default!;

    public JsonObject? ProviderConfig { get; set; }
}