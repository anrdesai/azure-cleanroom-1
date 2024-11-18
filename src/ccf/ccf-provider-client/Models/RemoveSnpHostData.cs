// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Controllers;

public class RemoveSnpHostDataInput
{
    public string InfraType { get; set; } = default!;

    public string HostData { get; set; } = default!;

    public JsonObject? ProviderConfig { get; set; }
}