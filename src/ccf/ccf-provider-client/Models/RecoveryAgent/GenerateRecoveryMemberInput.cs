// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using CcfProvider;

namespace Controllers;

public class GenerateRecoveryMemberInput
{
    public string InfraType { get; set; } = default!;

    public JsonObject? ProviderConfig { get; set; }

    public AgentConfig? AgentConfig { get; set; }

    public string MemberName { get; set; } = default!;
}